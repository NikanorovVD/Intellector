using System;

public static class IfenParser
{
    public static RecordedPosition Parse(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        string[] fields = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 4)
            throw new FormatException("Ожидаются четыре поля IFEN: расстановка, очередь, полуходы, номер хода.");

        var position = new RecordedPosition();
        ParsePlacement(position.Pieces, fields[0]);
        position.BlackToMove = ParseSide(fields[1]);
        position.HalfmoveClock = ParseNonNegativeInt(fields[2], "полуходов без прогресса");
        position.FullmoveNumber = ParseFullmove(fields[3]);
        return position;
    }

    private static void ParsePlacement(TileState?[][] pieces, string placement)
    {
        string[] ranks = placement.Split('/');
        if (ranks.Length != 7)
            throw new FormatException("Расстановка должна содержать 7 рядов, разделённых '/'.");

        for (int i = 0; i < ranks.Length; i++)
        {
            int rank = 7 - i;
            int expected = IfenFormatter.SquaresOnRank(rank);
            int square = 0;

            foreach (char symbol in ranks[i])
            {
                if (symbol >= '1' && symbol <= '9')
                {
                    square += symbol - '0';
                    if (square > expected)
                        throw new FormatException($"В ряде {rank} слишком много клеток.");
                    continue;
                }

                if (symbol == '0')
                    throw new FormatException("Цифра 0 в расстановке запрещена.");

                if (square >= expected)
                    throw new FormatException($"В ряде {rank} слишком много клеток.");

                int file = IfenFormatter.FileOnRank(rank, square);
                pieces[file][rank - 1] = ParsePiece(symbol);
                square++;
            }

            if (square != expected)
                throw new FormatException($"В ряде {rank} ожидается {expected} клеток, получено {square}.");
        }
    }

    private static TileState ParsePiece(char letter)
    {
        try
        {
            return new TileState
            {
                Type = IpgnFormatter.PieceFromLetter(letter),
                Team = char.IsLower(letter)
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new FormatException($"Неизвестная буква фигуры: {letter}");
        }
    }

    private static bool ParseSide(string text)
    {
        if (text == "w") return false;
        if (text == "b") return true;
        throw new FormatException($"Некорректная очередь хода: {text}");
    }

    private static int ParseNonNegativeInt(string text, string name)
    {
        if (!int.TryParse(text, out int value) || value < 0)
            throw new FormatException($"Некорректное число {name}: {text}");
        return value;
    }

    private static int ParseFullmove(string text)
    {
        if (!int.TryParse(text, out int value) || value < 1)
            throw new FormatException($"Некорректный номер хода: {text}");
        return value;
    }
}
