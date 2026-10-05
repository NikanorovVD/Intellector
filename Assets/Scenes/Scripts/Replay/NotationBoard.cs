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
        Place(0, 0, PieceType.Dominator, false);
        Place(1, 0, PieceType.Liberator, false);
        Place(2, 0, PieceType.Agressor, false);
        Place(3, 0, PieceType.Defensor, false);
        Place(4, 0, PieceType.Intellector, false);
        Place(5, 0, PieceType.Defensor, false);
        Place(6, 0, PieceType.Agressor, false);
        Place(7, 0, PieceType.Liberator, false);
        Place(8, 0, PieceType.Dominator, false);
        for (int i = 0; i < 9; i += 2)
            Place(i, 1, PieceType.Progressor, false);

        Place(0, 6, PieceType.Dominator, true);
        Place(1, 5, PieceType.Liberator, true);
        Place(2, 6, PieceType.Agressor, true);
        Place(3, 5, PieceType.Defensor, true);
        Place(4, 6, PieceType.Intellector, true);
        Place(5, 5, PieceType.Defensor, true);
        Place(6, 6, PieceType.Agressor, true);
        Place(7, 5, PieceType.Liberator, true);
        Place(8, 6, PieceType.Dominator, true);
        for (int i = 0; i < 9; i += 2)
            Place(i, 5, PieceType.Progressor, true);
    }

    private void Place(int x, int y, PieceType type, bool team)
    {
        tiles[x][y] = new TileState { Type = type, Team = team };
    }

    private static TileState? Copy(TileState? state)
    {
        if (state == null) return null;
        return new TileState { Type = state.Type, Team = state.Team };
    }
}
