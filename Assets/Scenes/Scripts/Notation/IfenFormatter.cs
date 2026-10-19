using System;
using System.Text;

public static class IfenFormatter
{
    public static RecordedPosition Initial()
    {
        var position = new RecordedPosition();
        Place(position, 0, 0, PieceType.Dominator, Side.White);
        Place(position, 1, 0, PieceType.Liberator, Side.White);
        Place(position, 2, 0, PieceType.Agressor, Side.White);
        Place(position, 3, 0, PieceType.Defensor, Side.White);
        Place(position, 4, 0, PieceType.Intellector, Side.White);
        Place(position, 5, 0, PieceType.Defensor, Side.White);
        Place(position, 6, 0, PieceType.Agressor, Side.White);
        Place(position, 7, 0, PieceType.Liberator, Side.White);
        Place(position, 8, 0, PieceType.Dominator, Side.White);
        for (int x = 0; x < 9; x += 2)
            Place(position, x, 1, PieceType.Progressor, Side.White);

        Place(position, 0, 6, PieceType.Dominator, Side.Black);
        Place(position, 1, 5, PieceType.Liberator, Side.Black);
        Place(position, 2, 6, PieceType.Agressor, Side.Black);
        Place(position, 3, 5, PieceType.Defensor, Side.Black);
        Place(position, 4, 6, PieceType.Intellector, Side.Black);
        Place(position, 5, 5, PieceType.Defensor, Side.Black);
        Place(position, 6, 6, PieceType.Agressor, Side.Black);
        Place(position, 7, 5, PieceType.Liberator, Side.Black);
        Place(position, 8, 6, PieceType.Dominator, Side.Black);
        for (int x = 0; x < 9; x += 2)
            Place(position, x, 5, PieceType.Progressor, Side.Black);

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
        builder.Append(position.SideToMove == Side.Black ? 'b' : 'w');
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
        return piece.Team == Side.Black ? char.ToLowerInvariant(letter) : letter;
    }

    private static void Place(RecordedPosition position, int x, int y, PieceType type, Side team)
    {
        position.Pieces[x][y] = new TileState { Type = type, Team = team };
    }
}
