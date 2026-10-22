using UnityEngine;

public static class BoardToEngine
{
    public static Engine CreateEngine(Board board)
    {
        var engine = new Engine();
        Load(engine, board);
        return engine;
    }

    public static void Load(Engine engine, Board board)
    {
        engine.Load(CopySquares(board), board.Turn);
    }

    public static void Load(Engine engine, NotationBoard board, Side sideToMove)
    {
        engine.Load(CopySquares(board), sideToMove);
    }

    public static EngineFigure[] CopySquares(Board board)
    {
        var squares = new EngineFigure[59];
        for (int i = 0; i < 59; i++)
            squares[i] = EngineFigure.Empty;

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

        return squares;
    }

    public static Engine CreateEngine(NotationBoard board, Side sideToMove)
    {
        var engine = new Engine();
        Load(engine, board, sideToMove);
        return engine;
    }

    public static EngineFigure[] CopySquares(NotationBoard board)
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

        return squares;
    }

    public static EngineFigure ToEngineFigure(IPiece piece) =>
        piece == null ? EngineFigure.Empty : ToEngineFigure(piece.Type, piece.Team);

    public static EngineFigure ToEngineFigure(TileState tile) =>
        tile == null ? EngineFigure.Empty : ToEngineFigure(tile.Type, tile.Team);

    private static EngineFigure ToEngineFigure(PieceType type, Side side)
    {
        return type switch
        {
            PieceType.Progressor => EngineUtils.WithColor(EngineFigure.WhiteProgressor, side),
            PieceType.Liberator => EngineUtils.WithColor(EngineFigure.WhiteLiberator, side),
            PieceType.Intellector => EngineUtils.WithColor(EngineFigure.WhiteIntellector, side),
            PieceType.Dominator => EngineUtils.WithColor(EngineFigure.WhiteDominator, side),
            PieceType.Defensor => EngineUtils.WithColor(EngineFigure.WhiteDefensor, side),
            PieceType.Agressor => EngineUtils.WithColor(EngineFigure.WhiteAgressor, side),
            _ => EngineFigure.Empty
        };
    }
}
