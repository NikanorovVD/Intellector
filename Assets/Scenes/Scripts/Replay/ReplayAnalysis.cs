using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public class ReplayAnalysis
{
    public event Action<MoveResult> Updated;

    private const double SearchMs = 3_600_000;
    private int generation;
    private Engine running;
    private bool enabled;

    public bool Enabled => enabled;

    public void SetEnabled(bool on, Board board)
    {
        if (on == enabled) return;
        enabled = on;
        if (on)
            Analyze(board);
        else
            Stop();
    }

    public void Analyze(Board board)
    {
        generation++;
        running?.RequestStop();
        if (!enabled || board == null)
        {
            running = null;
            Updated?.Invoke(default);
            return;
        }

        int gen = generation;
        Engine engine = BoardToEngine.CreateEngine(board);
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

    public void Stop()
    {
        generation++;
        running?.RequestStop();
        running = null;
        Updated?.Invoke(default);
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
        IPiece moving = board.pieces[fromX][fromY];
        if (moving == null) return null;
        IPiece target = board.pieces[toX][toY];
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
            0 => PieceType.progressor,
            1 => PieceType.dominator,
            2 => PieceType.liberator,
            3 => PieceType.agressor,
            4 => PieceType.defensor,
            5 => PieceType.intellector,
            _ => PieceType.progressor
        };
    }
}
