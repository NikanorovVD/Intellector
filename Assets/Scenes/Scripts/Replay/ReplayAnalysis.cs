using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using UnityEngine;

public class ReplayAnalysis
{
    public event Action<MoveResult> Updated;
    public event Action<int, MoveQuality> MoveJudged;
    public event Action<int, MoveQuality> VariationJudged;
    public event Action<int, int> PreProgress;
    public event Action PreFinished;
    public event Action SaveableChanged;

    private const double SearchMs = 3_600_000;

    private const int LiveJudgementDepth = 6;
    private readonly object preLock = new();
    private int generation;
    private int preGeneration;
    private Engine running;
    private Engine anchor;
    private Engine preRunning;
    private bool enabled;
    private bool preAnalyzing;
    private bool mainLineComplete;
    private int preFirstPly;
    private MoveResult[] mainCache;
    private bool[] mainHas;
    private MoveQuality[] moveQualities;
    private double? whiteAccuracy;
    private double? blackAccuracy;
    private double[] liveMarks;
    private int[] liveDepth;
    private bool[] liveHas;
    private MoveResult[] liveCache;
    private int varFrom = -1;
    private double[] varMarks;
    private int[] varDepth;
    private bool[] varHas;

    public bool Enabled => enabled;
    public bool IsPreAnalyzing
    {
        get { lock (preLock) return preAnalyzing; }
    }

    public bool HasCompleteMainLine
    {
        get { lock (preLock) return mainLineComplete && !preAnalyzing; }
    }

    public bool CanSave
    {
        get
        {
            lock (preLock)
                return !preAnalyzing && (HasAnyLocked(mainHas) || HasAnyLocked(liveHas));
        }
    }

    public void SetEnabled(bool on)
    {
        if (on == enabled) return;
        enabled = on;
        if (!on)
            StopLive();
    }

    public bool TryGetMain(int ply, out MoveResult result)
    {
        lock (preLock)
        {
            if (mainHas != null && ply >= 0 && ply < mainHas.Length && mainHas[ply])
            {
                result = mainCache[ply];
                return true;
            }
        }
        result = default;
        return false;
    }

    public bool TryGetMoveQuality(out GameMoveQuality quality)
    {
        lock (preLock)
        {
            if (moveQualities != null)
            {
                quality = new GameMoveQuality
                {
                    Moves = moveQualities,
                    WhiteAccuracy = whiteAccuracy,
                    BlackAccuracy = blackAccuracy
                };
                return true;
            }
        }
        quality = default;
        return false;
    }

    public bool TryExport(NotationBoard start, IReadOnlyList<ReplayMove> moves, out List<PositionAnalysis> positions)
    {
        positions = null;
        MoveResult[] copy;
        bool[] has;
        lock (preLock)
        {
            if (preAnalyzing || start == null || moves == null)
                return false;
            int count = moves.Count + 1;
            copy = new MoveResult[count];
            has = new bool[count];
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                if (TryStoredResultLocked(i, out MoveResult stored))
                {
                    copy[i] = stored;
                    has[i] = true;
                    any = true;
                }
            }
            if (!any)
                return false;
        }

        positions = new List<PositionAnalysis>(copy.Length);
        for (int ply = 0; ply < copy.Length; ply++)
        {
            RecordedMove best = null;
            if (has[ply] && copy[ply].Move.HasValue)
                best = ToRecordedMove(copy[ply].Move.Value, start);
            positions.Add(new PositionAnalysis
            {
                Mark = has[ply] ? copy[ply].Mark : 0,
                Depth = has[ply] ? copy[ply].Depth : 0,
                BestMove = best,
                Known = has[ply]
            });
            if (ply < moves.Count)
                start.Apply(moves[ply]);
        }
        return true;
    }

    private bool TryStoredResultLocked(int ply, out MoveResult result)
    {
        if (mainHas != null && ply >= 0 && ply < mainHas.Length && mainHas[ply] && IsFiniteMark(mainCache[ply].Mark))
        {
            result = mainCache[ply];
            return true;
        }
        if (liveHas != null && ply >= 0 && ply < liveHas.Length && liveHas[ply] && IsFiniteMark(liveCache[ply].Mark))
        {
            result = liveCache[ply];
            return true;
        }
        result = default;
        return false;
    }

    private static bool HasAnyLocked(bool[] has)
    {
        if (has == null)
            return false;
        for (int i = 0; i < has.Length; i++)
        {
            if (has[i])
                return true;
        }
        return false;
    }

    private static bool IsFiniteMark(double mark)
    {
        return !double.IsNaN(mark) && !double.IsInfinity(mark);
    }

    public bool Restore(IReadOnlyList<PositionAnalysis> positions, int firstPly)
    {
        if (positions == null || positions.Count == 0)
            return false;

        var cache = new MoveResult[positions.Count];
        var has = new bool[positions.Count];
        bool any = false;
        bool all = true;
        for (int ply = 0; ply < positions.Count; ply++)
        {
            PositionAnalysis position = positions[ply];
            if (position == null || !position.Known)
            {
                all = false;
                continue;
            }
            if (double.IsNaN(position.Mark) || double.IsInfinity(position.Mark) || position.Depth < 0)
                return false;
            EngineMove? engineMove = null;
            if (position.BestMove != null)
                engineMove = ToEngineMove(position.BestMove, Sides.ToMove(firstPly + ply));
            cache[ply] = new MoveResult
            {
                Mark = position.Mark,
                Depth = position.Depth,
                Move = engineMove
            };
            has[ply] = true;
            any = true;
        }
        if (!any)
            return false;

        lock (preLock)
        {
            if (preAnalyzing)
                return false;
            preFirstPly = firstPly;
            mainCache = cache;
            mainHas = has;
            mainLineComplete = all;
            ComputeMoveQualityLocked();
        }
        return true;
    }

    public void Analyze(Board board, int ply, int positionCount, int firstPly, NotationBoard previous)
    {
        generation++;
        running?.RequestStop();
        anchor?.RequestStop();
        anchor = null;
        if (!enabled || board == null || preAnalyzing)
        {
            running = null;
            if (!preAnalyzing)
                Updated?.Invoke(default);
            return;
        }

        int gen = generation;
        Engine engine = BoardToEngine.CreateEngine(board);
        if (engine.TryGetTerminalResult(out MoveResult terminal))
        {
            running = null;
            Updated?.Invoke(terminal);
            NoteLive(ply, terminal, positionCount, firstPly);
            StartAnchor(previous, ply, positionCount, firstPly, gen);
            return;
        }

        running = engine;
        StartAnchor(previous, ply, positionCount, firstPly, gen);
        engine.OnProgress += result =>
        {
            if (gen != generation) return;
            if (double.IsInfinity(result.Mark)) return;
            MainTasks.AddTask(() =>
            {
                if (gen != generation) return;
                Updated?.Invoke(result);
                NoteLive(ply, result, positionCount, firstPly);
            });
        };
        Task.Run(() =>
        {
            try
            {
                engine.BestMoveByTime(SearchMs);
            }
            catch (Exception e)
            {
                LogWriter.Error(e);
            }
        });
    }

    public void AnalyzeVariation(Board board, int varPos, int variationFrom, int firstPly, NotationBoard previous)
    {
        generation++;
        running?.RequestStop();
        anchor?.RequestStop();
        anchor = null;
        if (!enabled || board == null || preAnalyzing)
        {
            running = null;
            if (!preAnalyzing)
                Updated?.Invoke(default);
            return;
        }

        int gen = generation;
        Engine engine = BoardToEngine.CreateEngine(board);
        if (engine.TryGetTerminalResult(out MoveResult terminal))
        {
            running = null;
            Updated?.Invoke(terminal);
            NoteVariation(varPos, terminal, variationFrom, firstPly);
            StartVariationAnchor(previous, varPos, variationFrom, firstPly, gen);
            return;
        }

        running = engine;
        StartVariationAnchor(previous, varPos, variationFrom, firstPly, gen);
        engine.OnProgress += result =>
        {
            if (gen != generation) return;
            if (double.IsInfinity(result.Mark)) return;
            MainTasks.AddTask(() =>
            {
                if (gen != generation) return;
                Updated?.Invoke(result);
                NoteVariation(varPos, result, variationFrom, firstPly);
            });
        };
        Task.Run(() =>
        {
            try
            {
                engine.BestMoveByTime(SearchMs);
            }
            catch (Exception e)
            {
                LogWriter.Error(e);
            }
        });
    }

    public void ResetVariation(int variationFrom)
    {
        lock (preLock)
        {
            varFrom = variationFrom;
            varMarks = null;
            varDepth = null;
            varHas = null;
        }
    }

    public void TrimVariation(int keepThrough)
    {
        lock (preLock)
        {
            if (varHas == null)
                return;
            for (int i = keepThrough + 1; i < varHas.Length; i++)
                varHas[i] = false;
        }
    }

    public void StartPreAnalyze(IReadOnlyList<ReplayMove> moves, NotationBoard start, int firstPly, int depth)
    {
        StopLive();
        if (moves == null || start == null) return;

        int total = moves.Count + 1;
        int gen;
        lock (preLock)
        {
            preGeneration++;
            preRunning?.RequestStop();
            preRunning = null;
            gen = preGeneration;
            preAnalyzing = true;
            mainLineComplete = false;
            preFirstPly = firstPly;
            mainCache = new MoveResult[total];
            mainHas = new bool[total];
            ClearMoveQualityLocked();
        }

        PreProgress?.Invoke(0, total);
        int searchDepth = Math.Max(1, depth);
        Task.Run(() => RunPreAnalyze(moves, start, firstPly, searchDepth, gen, total));
    }

    public void CancelPreAnalyze()
    {
        bool notify;
        lock (preLock)
        {
            notify = preAnalyzing || mainLineComplete || mainHas != null;
            preGeneration++;
            preRunning?.RequestStop();
            preRunning = null;
            preAnalyzing = false;
            mainLineComplete = false;
            mainCache = null;
            mainHas = null;
            ClearMoveQualityLocked();
        }
        if (notify)
            PreFinished?.Invoke();
    }

    public void Discard()
    {
        StopLive();
        lock (preLock)
        {
            preGeneration++;
            preRunning?.RequestStop();
            preRunning = null;
            preAnalyzing = false;
            mainLineComplete = false;
            mainCache = null;
            mainHas = null;
            ClearMoveQualityLocked();
        }
    }

    public void StopLive()
    {
        generation++;
        running?.RequestStop();
        running = null;
        anchor?.RequestStop();
        anchor = null;
    }

    public void Stop()
    {
        CancelPreAnalyze();
        StopLive();
        Updated?.Invoke(default);
    }

    private void RunPreAnalyze(
        IReadOnlyList<ReplayMove> moves,
        NotationBoard board,
        int firstPly,
        int depth,
        int gen,
        int total)
    {
        try
        {
            for (int ply = 0; ply < total; ply++)
            {
                if (!IsCurrentPre(gen)) return;

                Side sideToMove = Sides.ToMove(firstPly + ply);
                Engine engine = BoardToEngine.CreateEngine(board, sideToMove);
                MoveResult result;
                if (!engine.TryGetTerminalResult(out result))
                {
                    lock (preLock)
                    {
                        if (gen != preGeneration) return;
                        preRunning = engine;
                    }
                    result = engine.BestMoveByDepth(depth);
                    lock (preLock)
                    {
                        if (preRunning == engine)
                            preRunning = null;
                    }
                }

                if (!StoreMain(gen, ply, result)) return;
                MainTasks.AddTask(() =>
                {
                    if (!IsCurrentPre(gen)) return;
                    PreProgress?.Invoke(ply + 1, total);
                });

                if (ply < moves.Count)
                    board.Apply(moves[ply]);
            }

            lock (preLock)
            {
                if (gen != preGeneration) return;
                preAnalyzing = false;
                mainLineComplete = true;
                ComputeMoveQualityLocked();
            }
            MainTasks.AddTask(() =>
            {
                if (!IsCurrentPre(gen)) return;
                PreFinished?.Invoke();
            });
        }
        catch (Exception e)
        {
            LogWriter.Error(e);
            lock (preLock)
            {
                if (gen != preGeneration) return;
                preAnalyzing = false;
                mainLineComplete = false;
                ClearMoveQualityLocked();
            }
            MainTasks.AddTask(() =>
            {
                if (!IsCurrentPre(gen)) return;
                PreFinished?.Invoke();
            });
        }
    }

    private bool IsCurrentPre(int gen)
    {
        lock (preLock)
            return gen == preGeneration;
    }

    public bool TryDeepenMain(int ply, MoveResult result)
    {
        if (double.IsNaN(result.Mark) || double.IsInfinity(result.Mark))
            return false;

        List<(int index, MoveQuality quality)> changed = null;
        lock (preLock)
        {
            if (preAnalyzing || mainCache == null || mainHas == null)
                return false;
            if (ply < 0 || ply >= mainCache.Length || !mainHas[ply])
                return false;
            if (result.Depth <= mainCache[ply].Depth)
                return false;

            MoveQuality[] previous = moveQualities;
            mainCache[ply] = result;
            if (!mainLineComplete)
                return true;

            ComputeMoveQualityLocked();
            if (previous == null || moveQualities == null)
                return true;
            int count = Math.Min(previous.Length, moveQualities.Length);
            for (int i = 0; i < count; i++)
            {
                if (previous[i].Known && previous[i].Judgement == moveQualities[i].Judgement)
                    continue;
                if (changed == null)
                    changed = new List<(int index, MoveQuality quality)>();
                changed.Add((i, moveQualities[i]));
            }
        }

        if (changed != null)
        {
            for (int i = 0; i < changed.Count; i++)
                MoveJudged?.Invoke(changed[i].index, changed[i].quality);
        }
        return true;
    }

    private bool StoreMain(int gen, int ply, MoveResult result)
    {
        lock (preLock)
        {
            if (gen != preGeneration || mainCache == null || ply >= mainCache.Length)
                return false;
            mainCache[ply] = result;
            mainHas[ply] = true;
            return true;
        }
    }

    private void ClearMoveQualityLocked()
    {
        moveQualities = null;
        whiteAccuracy = null;
        blackAccuracy = null;
        ClearLiveLocked();
    }

    private void StartAnchor(NotationBoard previous, int ply, int positionCount, int firstPly, int gen)
    {
        if (ply <= 0 || previous == null || HasLive(ply - 1) || HasMain(ply - 1))
            return;

        int anchorPly = ply - 1;
        Side sideToMove = Sides.ToMove(firstPly + anchorPly);
        Engine engine = BoardToEngine.CreateEngine(previous, sideToMove);
        if (engine.TryGetTerminalResult(out MoveResult terminal))
        {
            NoteLive(anchorPly, terminal, positionCount, firstPly);
            return;
        }

        anchor = engine;
        Task.Run(() =>
        {
            try
            {
                MoveResult result = engine.BestMoveByDepth(LiveJudgementDepth);
                MainTasks.AddTask(() =>
                {
                    if (gen != generation) return;
                    NoteLive(anchorPly, result, positionCount, firstPly);
                });
            }
            catch (Exception e)
            {
                LogWriter.Error(e);
            }
        });
    }

    private bool HasLive(int ply)
    {
        lock (preLock)
            return liveHas != null && ply >= 0 && ply < liveHas.Length && liveHas[ply];
    }

    private bool HasMain(int ply)
    {
        lock (preLock)
            return mainHas != null && ply >= 0 && ply < mainHas.Length && mainHas[ply];
    }

    private void NoteLive(int ply, MoveResult result, int positionCount, int firstPly)
    {
        if (ply < 0 || positionCount < 2 || !IsStableEval(result))
            return;

        int depth = result.Depth >= LiveJudgementDepth ? result.Depth : int.MaxValue;
        bool publishPrevious = false;
        bool publishNext = false;
        MoveQuality previous = default;
        MoveQuality next = default;
        int previousIndex = ply - 1;
        int nextIndex = ply;
        lock (preLock)
        {
            if (preAnalyzing || mainLineComplete)
                return;
            EnsureLiveLocked(positionCount);
            if (ply >= liveMarks.Length)
                return;
            if (liveHas[ply] && depth < liveDepth[ply])
                return;

            bool changed = !liveHas[ply] || liveMarks[ply] != result.Mark;
            liveMarks[ply] = result.Mark;
            liveDepth[ply] = depth;
            liveHas[ply] = true;
            liveCache[ply] = result;
            if (!changed)
                return;

            publishPrevious = TryJudgeLocked(previousIndex, firstPly, out previous);
            publishNext = TryJudgeLocked(nextIndex, firstPly, out next);
            if (publishPrevious || publishNext)
                ComputeKnownAccuracyLocked(firstPly);
        }

        if (publishPrevious)
            MoveJudged?.Invoke(previousIndex, previous);
        if (publishNext)
            MoveJudged?.Invoke(nextIndex, next);
        SaveableChanged?.Invoke();
    }

    private bool TryJudgeLocked(int moveIndex, int firstPly, out MoveQuality quality)
    {
        quality = default;
        if (moveIndex < 0 || liveHas == null || moveIndex + 1 >= liveHas.Length)
            return false;
        if (!liveHas[moveIndex] || !liveHas[moveIndex + 1])
            return false;
        Side moved = Sides.ToMove(firstPly + moveIndex);
        quality = MoveAccuracy.FromMarks(liveMarks[moveIndex], liveMarks[moveIndex + 1], moved);
        return true;
    }

    private void StartVariationAnchor(NotationBoard previous, int varPos, int variationFrom, int firstPly, int gen)
    {
        if (varPos <= 0 || previous == null || HasVarMark(varPos - 1, variationFrom))
            return;

        int anchorPos = varPos - 1;
        Side sideToMove = Sides.ToMove(firstPly + variationFrom + anchorPos);
        Engine engine = BoardToEngine.CreateEngine(previous, sideToMove);
        if (engine.TryGetTerminalResult(out MoveResult terminal))
        {
            NoteVariation(anchorPos, terminal, variationFrom, firstPly);
            return;
        }

        anchor = engine;
        Task.Run(() =>
        {
            try
            {
                MoveResult result = engine.BestMoveByDepth(LiveJudgementDepth);
                MainTasks.AddTask(() =>
                {
                    if (gen != generation) return;
                    NoteVariation(anchorPos, result, variationFrom, firstPly);
                });
            }
            catch (Exception e)
            {
                LogWriter.Error(e);
            }
        });
    }

    private bool HasVarMark(int varPos, int variationFrom)
    {
        lock (preLock)
            return TryVarMarkLocked(varPos, variationFrom, out _);
    }

    private void NoteVariation(int varPos, MoveResult result, int variationFrom, int firstPly)
    {
        if (varPos < 0 || !IsStableEval(result))
            return;

        int depth = result.Depth >= LiveJudgementDepth ? result.Depth : int.MaxValue;
        bool publishPrevious = false;
        bool publishNext = false;
        MoveQuality previous = default;
        MoveQuality next = default;
        int previousIndex = varPos - 1;
        int nextIndex = varPos;
        lock (preLock)
        {
            if (preAnalyzing)
                return;
            if (varFrom != variationFrom)
            {
                varFrom = variationFrom;
                varMarks = null;
                varDepth = null;
                varHas = null;
            }
            EnsureVarLocked(varPos + 1);
            if (varHas[varPos] && depth < varDepth[varPos])
                return;

            bool changed = !varHas[varPos] || varMarks[varPos] != result.Mark;
            varMarks[varPos] = result.Mark;
            varDepth[varPos] = depth;
            varHas[varPos] = true;
            if (!changed)
                return;

            publishPrevious = TryJudgeVariationLocked(previousIndex, variationFrom, firstPly, out previous);
            publishNext = TryJudgeVariationLocked(nextIndex, variationFrom, firstPly, out next);
        }

        if (publishPrevious)
            VariationJudged?.Invoke(previousIndex, previous);
        if (publishNext)
            VariationJudged?.Invoke(nextIndex, next);
    }

    private bool TryJudgeVariationLocked(int moveIndex, int variationFrom, int firstPly, out MoveQuality quality)
    {
        quality = default;
        if (moveIndex < 0 || !TryVarMarkLocked(moveIndex, variationFrom, out double before))
            return false;
        if (!TryVarMarkLocked(moveIndex + 1, variationFrom, out double after))
            return false;
        Side moved = Sides.ToMove(firstPly + variationFrom + moveIndex);
        quality = MoveAccuracy.FromMarks(before, after, moved);
        return true;
    }

    private bool TryVarMarkLocked(int varPos, int variationFrom, out double mark)
    {
        mark = 0;
        if (varPos < 0)
            return false;
        if (varFrom == variationFrom && varHas != null && varPos < varHas.Length && varHas[varPos])
        {
            mark = varMarks[varPos];
            return true;
        }
        if (varPos == 0)
            return TryStoredMarkLocked(variationFrom, out mark);
        return false;
    }

    private bool TryStoredMarkLocked(int ply, out double mark)
    {
        if (mainHas != null && ply >= 0 && ply < mainHas.Length && mainHas[ply])
        {
            mark = mainCache[ply].Mark;
            return true;
        }
        if (liveHas != null && ply >= 0 && ply < liveHas.Length && liveHas[ply])
        {
            mark = liveMarks[ply];
            return true;
        }
        mark = 0;
        return false;
    }

    private void EnsureVarLocked(int positionCount)
    {
        if (varHas != null && varHas.Length >= positionCount)
            return;
        int old = varHas == null ? 0 : varHas.Length;
        var grownMarks = new double[positionCount];
        var grownDepth = new int[positionCount];
        var grownHas = new bool[positionCount];
        if (old > 0)
        {
            Array.Copy(varMarks, grownMarks, old);
            Array.Copy(varDepth, grownDepth, old);
            Array.Copy(varHas, grownHas, old);
        }
        varMarks = grownMarks;
        varDepth = grownDepth;
        varHas = grownHas;
    }

    private static bool IsStableEval(MoveResult result)
    {
        if (double.IsNaN(result.Mark) || double.IsInfinity(result.Mark))
            return false;
        if (result.Depth >= LiveJudgementDepth)
            return true;
        // При гарантированном мате поиск останавливается на текущей глубине.
        return Engine.IsMateScore(result.Mark);
    }

    private void EnsureLiveLocked(int positionCount)
    {
        if (liveHas != null && liveHas.Length == positionCount)
            return;
        liveMarks = new double[positionCount];
        liveDepth = new int[positionCount];
        liveHas = new bool[positionCount];
        liveCache = new MoveResult[positionCount];
    }

    private void ClearLiveLocked()
    {
        liveMarks = null;
        liveDepth = null;
        liveHas = null;
        liveCache = null;
    }

    private void ComputeMoveQualityLocked()
    {
        ComputeKnownAccuracyLocked(preFirstPly);
    }

    private void ComputeKnownAccuracyLocked(int firstPly)
    {
        moveQualities = null;
        whiteAccuracy = null;
        blackAccuracy = null;

        int count = 0;
        if (mainHas != null)
            count = mainHas.Length;
        if (liveHas != null && liveHas.Length > count)
            count = liveHas.Length;
        if (count < 2)
            return;

        int moveCount = count - 1;
        var moves = new MoveQuality[moveCount];
        var white = new List<double>();
        var black = new List<double>();
        for (int i = 0; i < moveCount; i++)
        {
            if (!TryAnyMarkLocked(i, out double before) || !TryAnyMarkLocked(i + 1, out double after))
                continue;
            Side moved = Sides.ToMove(firstPly + i);
            MoveQuality quality = MoveAccuracy.FromMarks(before, after, moved);
            moves[i] = quality;
            if (moved == Side.Black)
                black.Add(quality.Accuracy);
            else
                white.Add(quality.Accuracy);
        }

        if (white.Count == 0 && black.Count == 0)
            return;

        moveQualities = moves;
        whiteAccuracy = MoveAccuracy.GameAccuracy(white);
        blackAccuracy = MoveAccuracy.GameAccuracy(black);
    }

    private bool TryAnyMarkLocked(int ply, out double mark)
    {
        if (mainHas != null && ply >= 0 && ply < mainHas.Length && mainHas[ply])
        {
            mark = mainCache[ply].Mark;
            return true;
        }
        if (liveHas != null && ply >= 0 && ply < liveHas.Length && liveHas[ply])
        {
            mark = liveMarks[ply];
            return true;
        }
        mark = 0;
        return false;
    }

    public static string FormatMark(double mark)
    {
        double win = EngineTables.MarkOf(EngineFigure.WhiteIntellector);
        if (mark >= win * 0.9) return "#";
        if (mark <= -win * 0.9) return "-#";
        return ToPawns(mark).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
    }

    public static float BarRatio(double mark)
    {
        double win = EngineTables.MarkOf(EngineFigure.WhiteIntellector);
        if (mark >= win * 0.9) return 1f;
        if (mark <= -win * 0.9) return 0f;
        return (float)(0.5 + 0.5 * Math.Tanh(ToPawns(mark) / 4.0));
    }

    private static double ToPawns(double mark) =>
        mark / EngineTables.MarkOf(EngineFigure.WhiteProgressor);

    public static string FormatBestMove(EngineMove move, Board board)
    {
        RecordedMove recorded = ToRecordedMove(move, board);
        return recorded == null ? string.Empty : IpgnFormatter.FormatMove(recorded);
    }

    public static void HintMove(EngineMove move, Board board, PromotionHint promotionHint)
    {
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        IPiece moving = board.Pieces[fromX][fromY];
        board.HighlightHint(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY));
        if (promotionHint == null)
            return;
        if (moving == null)
        {
            promotionHint.Clear();
            return;
        }
        PieceType resulting = ToPieceType(move.Figure);
        if (resulting == moving.Type)
            promotionHint.Clear();
        else
            promotionHint.Show(new Vector2Int(toX, toY), resulting, moving.Team);
    }

    private static RecordedMove ToRecordedMove(EngineMove move, Board board)
    {
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        IPiece moving = board.Pieces[fromX][fromY];
        if (moving == null) return null;
        IPiece target = board.Pieces[toX][toY];
        PieceType resulting = ToPieceType(move.Figure);
        return new RecordedMove
        {
            Piece = moving.Type,
            From = new Vector2Int(fromX, fromY),
            To = new Vector2Int(toX, toY),
            Capture = target != null && target.Team != moving.Team,
            Castling = target != null && target.Team == moving.Team,
            Transformation = resulting != moving.Type ? resulting : null
        };
    }

    private static RecordedMove ToRecordedMove(EngineMove move, NotationBoard board)
    {
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        var from = new Vector2Int(fromX, fromY);
        var to = new Vector2Int(toX, toY);
        TileState? moving = board.Get(from);
        if (moving == null)
            return null;
        TileState? target = board.Get(to);
        PieceType resulting = ToPieceType(move.Figure);
        return new RecordedMove
        {
            Piece = moving.Type,
            From = from,
            To = to,
            Capture = target != null && target.Team != moving.Team,
            Castling = target != null && target.Team == moving.Team,
            Transformation = resulting != moving.Type ? resulting : null
        };
    }

    private static EngineMove ToEngineMove(RecordedMove move, Side sideToMove)
    {
        PieceType resulting = move.Transformation ?? move.Piece;
        EngineFigure figure = BoardToEngine.ToEngineFigure(new TileState { Type = resulting, Team = sideToMove });
        return new EngineMove(
            EngineUtils.GetEngineIndex(move.From.x, move.From.y),
            EngineUtils.GetEngineIndex(move.To.x, move.To.y),
            figure);
    }

    private static PieceType ToPieceType(EngineFigure figure)
    {
        return ((int)figure / 2) switch
        {
            0 => PieceType.Progressor,
            1 => PieceType.Dominator,
            2 => PieceType.Liberator,
            3 => PieceType.Agressor,
            4 => PieceType.Defensor,
            5 => PieceType.Intellector,
            _ => PieceType.Progressor
        };
    }
}
