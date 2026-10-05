using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using UnityEngine;

public class ReplayAnalysis
{
    public event Action<MoveResult> Updated;
    public event Action<int, int> PreProgress;
    public event Action PreFinished;

    private const double SearchMs = 3_600_000;
    private readonly object preLock = new();
    private int generation;
    private int preGeneration;
    private Engine running;
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

    public void Analyze(Board board)
    {
        generation++;
        running?.RequestStop();
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
            return;
        }

        running = engine;
        engine.OnProgress += result =>
        {
            if (gen != generation) return;
            if (double.IsInfinity(result.Mark)) return;
            MainTasks.AddTask(() =>
            {
                if (gen != generation) return;
                Updated?.Invoke(result);
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

    public static void HintMove(EngineMove move, Board board)
    {
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        board.HighlightHint(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY));
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
