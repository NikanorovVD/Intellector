using UnityEngine;

public static class BoardToEngine
{
    public static Engine CreateEngine(Board board)
    {
        var squares = new EngineFigure[59];
        for (int i = 0; i < 59; i++) squares[i] = EngineFigure.Empty;

        for (int x = 0; x < board.pieces.Length; x++)
        {
            for (int y = 0; y < board.pieces[x].Length; y++)
            {
                var piece = board.pieces[x][y];
                if (piece == null) continue;

                int idx = EngineUtils.GetEngineIndex(x, y);
                squares[idx] = ToEngineFigure(piece);
            }
        }

        var engine = new Engine();
        engine.Load(squares, board.Turn ? EngineColor.Black : EngineColor.White);
        return engine;
    }

    public static EngineFigure ToEngineFigure(IPiece piece) =>
        piece == null ? EngineFigure.Empty : ToEngineFigure(piece.Type, piece.Team ? EngineColor.Black : EngineColor.White);

    private static EngineFigure ToEngineFigure(PieceType type, EngineColor color)
    {
        return type switch
        {
            PieceType.progressor => EngineUtils.WithColor(EngineFigure.WhiteProgressor, color),
            PieceType.liberator => EngineUtils.WithColor(EngineFigure.WhiteLiberator, color),
            PieceType.intellector => EngineUtils.WithColor(EngineFigure.WhiteIntellector, color),
            PieceType.dominator => EngineUtils.WithColor(EngineFigure.WhiteDominator, color),
            PieceType.defensor => EngineUtils.WithColor(EngineFigure.WhiteDefensor, color),
            PieceType.agressor => EngineUtils.WithColor(EngineFigure.WhiteAgressor, color),
            _ => EngineFigure.Empty
        };
    }
}
