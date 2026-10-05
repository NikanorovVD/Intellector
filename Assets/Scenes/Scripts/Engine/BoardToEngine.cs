using UnityEngine;

public static class BoardToEngine
{
    public static Engine CreateEngine(Board board)
    {
        var squares = new EngineFigure[59];
        for (int i = 0; i < 59; i++) squares[i] = EngineFigure.Empty;

        for (int x = 0; x < board.Pieces.Length; x++)
        {
            for (int y = 0; y < board.Pieces[x].Length; y++)
            {
                var piece = board.Pieces[x][y];
                if (piece == null) continue;

                int idx = EngineUtils.GetEngineIndex(x, y);
                squares[idx] = ToEngineFigure(piece);
            }
        }

        var engine = new Engine();
        engine.Load(squares, board.Turn ? EngineColor.Black : EngineColor.White);
        return engine;
    }

    public static Engine CreateEngine(NotationBoard board, bool blackToMove)
    {
        var squares = new EngineFigure[59];
        for (int i = 0; i < 59; i++) squares[i] = EngineFigure.Empty;

        for (int x = 0; x < 9; x++)
        {
            int height = 7 - (x % 2);
            for (int y = 0; y < height; y++)
            {
                TileState tile = board.Get(new Vector2Int(x, y));
                if (tile == null) continue;
                squares[EngineUtils.GetEngineIndex(x, y)] = ToEngineFigure(tile);
            }
        }

        var engine = new Engine();
        engine.Load(squares, blackToMove ? EngineColor.Black : EngineColor.White);
        return engine;
    }

    public static EngineFigure ToEngineFigure(IPiece piece) =>
        piece == null ? EngineFigure.Empty : ToEngineFigure(piece.Type, piece.Team ? EngineColor.Black : EngineColor.White);

    public static EngineFigure ToEngineFigure(TileState tile) =>
        tile == null ? EngineFigure.Empty : ToEngineFigure(tile.Type, tile.Team ? EngineColor.Black : EngineColor.White);

    private static EngineFigure ToEngineFigure(PieceType type, EngineColor color)
    {
        return type switch
        {
            PieceType.Progressor => EngineUtils.WithColor(EngineFigure.WhiteProgressor, color),
            PieceType.Liberator => EngineUtils.WithColor(EngineFigure.WhiteLiberator, color),
            PieceType.Intellector => EngineUtils.WithColor(EngineFigure.WhiteIntellector, color),
            PieceType.Dominator => EngineUtils.WithColor(EngineFigure.WhiteDominator, color),
            PieceType.Defensor => EngineUtils.WithColor(EngineFigure.WhiteDefensor, color),
            PieceType.Agressor => EngineUtils.WithColor(EngineFigure.WhiteAgressor, color),
            _ => EngineFigure.Empty
        };
    }
}
