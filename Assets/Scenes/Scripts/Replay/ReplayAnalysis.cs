using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using UnityEngine;

public class ReplayAnalysis
{
    public event Action<MoveResult> Updated;
    public event Action<int, MoveQuality> MoveJudged;
    public event Action<int, int> PreProgress;
    public event Action PreFinished;

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

    public bool Enabled => enabled;
    public bool IsPreAnalyzing
    {
        get { lock (preLock) return preAnalyzing; }
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
                Debug.LogException(e);
            }
        });
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

                bool blackToMove = (firstPly + ply) % 2 == 1;
                Engine engine = BoardToEngine.CreateEngine(board, blackToMove);
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
            Debug.LogException(e);
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
        if (ply <= 0 || previous == null || HasLive(ply - 1))
            return;

        int anchorPly = ply - 1;
        bool blackToMove = (firstPly + anchorPly) % 2 == 1;
        Engine engine = BoardToEngine.CreateEngine(previous, blackToMove);
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
                Debug.LogException(e);
            }
        });
    }

    private bool HasLive(int ply)
    {
        lock (preLock)
            return liveHas != null && ply >= 0 && ply < liveHas.Length && liveHas[ply];
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
            if (!changed)
                return;

            publishPrevious = TryJudgeLocked(previousIndex, firstPly, out previous);
            publishNext = TryJudgeLocked(nextIndex, firstPly, out next);
        }

        if (publishPrevious)
            MoveJudged?.Invoke(previousIndex, previous);
        if (publishNext)
            MoveJudged?.Invoke(nextIndex, next);
    }

    private bool TryJudgeLocked(int moveIndex, int firstPly, out MoveQuality quality)
    {
        quality = default;
        if (moveIndex < 0 || liveHas == null || moveIndex + 1 >= liveHas.Length)
            return false;
        if (!liveHas[moveIndex] || !liveHas[moveIndex + 1])
            return false;
        bool blackMoved = (firstPly + moveIndex) % 2 == 1;
        quality = MoveAccuracy.FromMarks(liveMarks[moveIndex], liveMarks[moveIndex + 1], blackMoved);
        return true;
    }

    private static bool IsStableEval(MoveResult result)
    {
        if (double.IsNaN(result.Mark) || double.IsInfinity(result.Mark))
            return false;
        if (result.Depth >= LiveJudgementDepth)
            return true;
        // На гарантированном мате поиск останавливается на глубине 0 или 1.
        return Engine.IsImmediateWin(result.Mark);
    }

    private void EnsureLiveLocked(int positionCount)
    {
        if (liveHas != null && liveHas.Length == positionCount)
            return;
        liveMarks = new double[positionCount];
        liveDepth = new int[positionCount];
        liveHas = new bool[positionCount];
    }

    private void ClearLiveLocked()
    {
        liveMarks = null;
        liveDepth = null;
        liveHas = null;
    }

    private void ComputeMoveQualityLocked()
    {
        if (mainCache == null || mainHas == null || mainCache.Length < 2)
        {
            ClearMoveQualityLocked();
            return;
        }

        int moveCount = mainCache.Length - 1;
        var marks = new double[moveCount + 1];
        for (int ply = 0; ply <= moveCount; ply++)
        {
            if (!mainHas[ply])
            {
                ClearMoveQualityLocked();
                return;
            }
            marks[ply] = mainCache[ply].Mark;
        }

        GameMoveQuality quality = MoveAccuracy.FromPositionMarks(marks, preFirstPly);
        moveQualities = quality.Moves;
        whiteAccuracy = quality.WhiteAccuracy;
        blackAccuracy = quality.BlackAccuracy;
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
