using UnityEngine;

public class NotationBoard
{
    private readonly TileState?[][] tiles;

    public NotationBoard()
    {
        tiles = new TileState?[9][];
        for (int i = 0; i < 9; i++)
            tiles[i] = new TileState?[7 - (i % 2)];
        PlaceInitial();
    }

    public NotationBoard(RecordedPosition position)
    {
        tiles = new TileState?[9][];
        for (int i = 0; i < 9; i++)
        {
            tiles[i] = new TileState?[7 - (i % 2)];
            for (int j = 0; j < tiles[i].Length; j++)
                tiles[i][j] = Copy(position.Pieces[i][j]);
        }
    }

    public TileState? Get(Vector2Int pos) => Copy(tiles[pos.x][pos.y]);

    public void Apply(RecordedMove move)
    {
        TileState? from = tiles[move.From.x][move.From.y];
        if (from == null)
            throw new System.InvalidOperationException($"Нет фигуры на {IpgnFormatter.FormatTile(move.From)}");

        if (move.Castling)
        {
            (tiles[move.From.x][move.From.y], tiles[move.To.x][move.To.y]) =
                (tiles[move.To.x][move.To.y], tiles[move.From.x][move.From.y]);
            return;
        }

        tiles[move.From.x][move.From.y] = null;
        tiles[move.To.x][move.To.y] = new TileState
        {
            Type = move.Transformation ?? from.Type,
            Team = from.Team
        };
    }

    public void Apply(ReplayMove move)
    {
        tiles[move.From.x][move.From.y] = Copy(move.FromAfter);
        tiles[move.To.x][move.To.y] = Copy(move.ToAfter);
    }

    private void PlaceInitial()
    {
        Place(0, 0, PieceType.Dominator, Side.White);
        Place(1, 0, PieceType.Liberator, Side.White);
        Place(2, 0, PieceType.Agressor, Side.White);
        Place(3, 0, PieceType.Defensor, Side.White);
        Place(4, 0, PieceType.Intellector, Side.White);
        Place(5, 0, PieceType.Defensor, Side.White);
        Place(6, 0, PieceType.Agressor, Side.White);
        Place(7, 0, PieceType.Liberator, Side.White);
        Place(8, 0, PieceType.Dominator, Side.White);
        for (int i = 0; i < 9; i += 2)
            Place(i, 1, PieceType.Progressor, Side.White);

        Place(0, 6, PieceType.Dominator, Side.Black);
        Place(1, 5, PieceType.Liberator, Side.Black);
        Place(2, 6, PieceType.Agressor, Side.Black);
        Place(3, 5, PieceType.Defensor, Side.Black);
        Place(4, 6, PieceType.Intellector, Side.Black);
        Place(5, 5, PieceType.Defensor, Side.Black);
        Place(6, 6, PieceType.Agressor, Side.Black);
        Place(7, 5, PieceType.Liberator, Side.Black);
        Place(8, 6, PieceType.Dominator, Side.Black);
        for (int i = 0; i < 9; i += 2)
            Place(i, 5, PieceType.Progressor, Side.Black);
    }

    private void Place(int x, int y, PieceType type, Side team)
    {
        tiles[x][y] = new TileState { Type = type, Team = team };
    }

    private static TileState? Copy(TileState? state)
    {
        if (state == null) return null;
        return new TileState { Type = state.Type, Team = state.Team };
    }
}
