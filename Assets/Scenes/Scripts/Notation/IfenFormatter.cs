using System;
using System.Text;

public static class IfenFormatter
{
    public static RecordedPosition Initial()
    {
        var position = new RecordedPosition();
        Place(position, 0, 0, PieceType.dominator, false);
        Place(position, 1, 0, PieceType.liberator, false);
        Place(position, 2, 0, PieceType.agressor, false);
        Place(position, 3, 0, PieceType.defensor, false);
        Place(position, 4, 0, PieceType.intellector, false);
        Place(position, 5, 0, PieceType.defensor, false);
        Place(position, 6, 0, PieceType.agressor, false);
        Place(position, 7, 0, PieceType.liberator, false);
        Place(position, 8, 0, PieceType.dominator, false);
        for (int x = 0; x < 9; x += 2)
            Place(position, x, 1, PieceType.progressor, false);

        Place(position, 0, 6, PieceType.dominator, true);
        Place(position, 1, 5, PieceType.liberator, true);
        Place(position, 2, 6, PieceType.agressor, true);
        Place(position, 3, 5, PieceType.defensor, true);
        Place(position, 4, 6, PieceType.intellector, true);
        Place(position, 5, 5, PieceType.defensor, true);
        Place(position, 6, 6, PieceType.agressor, true);
        Place(position, 7, 5, PieceType.liberator, true);
        Place(position, 8, 6, PieceType.dominator, true);
        for (int x = 0; x < 9; x += 2)
            Place(position, x, 5, PieceType.progressor, true);

        return position;
    }

    public static string Format(RecordedPosition position)
    {
        if (position == null)
            throw new ArgumentNullException(nameof(position));
        if (position.Pieces == null)
            throw new ArgumentException("Расстановка не задана.", nameof(position));

        var builder = new StringBuilder();
        AppendPlacement(builder, position.Pieces);
        builder.Append(' ');
        builder.Append(position.BlackToMove ? 'b' : 'w');
        builder.Append(' ');
        builder.Append(position.HalfmoveClock);
        builder.Append(' ');
        builder.Append(position.FullmoveNumber);
        return builder.ToString();
    }

    public static int SquaresOnRank(int rank) => rank == 7 ? 5 : 9;

    public static int FileOnRank(int rank, int square) => rank == 7 ? square * 2 : square;

    private static void AppendPlacement(StringBuilder builder, TileState?[][] pieces)
    {
        for (int rank = 7; rank >= 1; rank--)
        {
            if (rank < 7)
                builder.Append('/');

            int empty = 0;
            int squares = SquaresOnRank(rank);
            for (int square = 0; square < squares; square++)
            {
                int file = FileOnRank(rank, square);
                TileState? piece = pieces[file][rank - 1];
                if (piece == null)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    builder.Append(empty);
                    empty = 0;
                }
                builder.Append(FormatPiece(piece));
            }

            if (empty > 0)
                builder.Append(empty);
        }
    }

    private static char FormatPiece(TileState piece)
    {
        char letter = IpgnFormatter.PieceLetter(piece.Type);
        return piece.Team ? char.ToLowerInvariant(letter) : letter;
    }

    private static void Place(RecordedPosition position, int x, int y, PieceType type, bool team)
    {
        position.Pieces[x][y] = new TileState { Type = type, Team = team };
    }
}
