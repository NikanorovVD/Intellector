using System;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class IpgnFormatter
{
    public static string Format(GameRecord record)
    {
        var builder = new StringBuilder(FormatHeader(record));
        AppendMoves(builder, record);
        if (record.IsFinished)
            builder.AppendLine(record.Result);
        return builder.ToString();
    }

    private static string FormatHeader(GameRecord record)
    {
        var builder = new StringBuilder();

        AppendTag(builder, "Event", record.Event);
        AppendTag(builder, "Site", record.Site);
        AppendTag(builder, "Date", record.Date);
        AppendTag(builder, "UTCTime", record.UTCTime);
        AppendTag(builder, "White", record.White);
        AppendTag(builder, "Black", record.Black);
        AppendTag(builder, "Result", record.Result);
        AppendTag(builder, "TimeControl", record.TimeControl);
        AppendTag(builder, "GameMode", record.GameMode);
        AppendTag(builder, "AppVersion", record.AppVersion);
        if (record.SetUp == "1")
        {
            AppendTag(builder, "SetUp", "1");
            AppendTag(builder, "IFEN", record.Ifen);
        }
        if (!string.IsNullOrEmpty(record.Termination))
            AppendTag(builder, "Termination", record.Termination);

        builder.AppendLine();
        return builder.ToString();
    }

    public static string FormatMovetextEntry(RecordedMove move, int index)
    {
        return FormatMovetextEntry(move, index, 0, 1);
    }

    public static string FormatMovetextEntry(RecordedMove move, int index, int firstPly, int firstFullmove)
    {
        int ply = firstPly + index;
        int fullmove = firstFullmove + ply / 2;
        var builder = new StringBuilder();
        if (ply % 2 == 0)
        {
            if (index > 0)
                builder.AppendLine();
            builder.Append(fullmove);
            builder.Append(". ");
        }
        else if (index == 0)
        {
            builder.Append(fullmove);
            builder.Append(". ... ");
        }
        else
        {
            builder.Append(' ');
        }
        builder.Append(FormatMove(move));
        return builder.ToString();
    }

    public static string FormatTile(Vector2Int coordinates)
    {
        char file = (char)('a' + coordinates.x);
        int rank = coordinates.y + 1;
        return $"{file}{rank}";
    }

    public static PieceType PieceFromLetter(char letter)
    {
        return char.ToUpperInvariant(letter) switch
        {
            'P' => PieceType.progressor,
            'L' => PieceType.liberator,
            'I' => PieceType.intellector,
            'D' => PieceType.dominator,
            'F' => PieceType.defensor,
            'A' => PieceType.agressor,
            _ => throw new ArgumentOutOfRangeException(nameof(letter), letter, "Неизвестная буква фигуры")
        };
    }

    public static Vector2Int ParseTile(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2)
            throw new FormatException($"Некорректная клетка: {text}");
        char file = char.ToLowerInvariant(text[0]);
        int x = file - 'a';
        if (x < 0 || x > 8)
            throw new FormatException($"Некорректная клетка: {text}");
        if (!int.TryParse(text.Substring(1), out int rank) || rank < 1 || rank > 7)
            throw new FormatException($"Некорректная клетка: {text}");
        return new Vector2Int(x, rank - 1);
    }

    public static char PieceLetter(PieceType type)
    {
        return type switch
        {
            PieceType.progressor => 'P',
            PieceType.liberator => 'L',
            PieceType.intellector => 'I',
            PieceType.dominator => 'D',
            PieceType.defensor => 'F',
            PieceType.agressor => 'A',
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип фигуры")
        };
    }

    public static string FormatMove(RecordedMove move)
    {
        string separator = move.Castling ? "<->" : move.Capture ? "x" : "-";
        string text = $"{PieceLetter(move.Piece)}{FormatTile(move.From)}{separator}{FormatTile(move.To)}";
        if (move.Transformation.HasValue)
            text += $"={PieceLetter(move.Transformation.Value)}";
        return text;
    }

    public static string FormatResult(bool? winner)
    {
        return winner switch
        {
            false => "1-0",
            true => "0-1",
            null => "1/2-1/2"
        };
    }

    public static string FormatTimeControl(TimeContol timeControl)
    {
        if (timeControl == null || !timeControl.Active)
            return "-";
        string baseSeconds = (timeControl.MaxMilliseconds / 1000).ToString(CultureInfo.InvariantCulture);
        if (timeControl.AddedSeconds <= 0)
            return baseSeconds;
        return baseSeconds + "+" + timeControl.AddedSeconds.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatTermination(EndGameReason reason)
    {
        return reason switch
        {
            EndGameReason.IntellectorCapture => "Интеллектор был взят",
            EndGameReason.IntellectorReachLustRank => "Интеллектор достиг базовой линии",
            EndGameReason.AllPiecesBlocked => "Блокировка",
            EndGameReason.TimesUp => "Время истекло",
            EndGameReason.Exit => "Выход из партии",
            EndGameReason.Resignation => "Сдача",
            EndGameReason.DrawByAgreement => "Ничья по договоренности",
            EndGameReason.DrawByRepeatingPosition => "Ничья повторением позиции",
            EndGameReason.DrawBy30MovesRule => "Ничья по правилу 30 ходов",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Неизвестная причина окончания")
        };
    }

    private static void AppendTag(StringBuilder builder, string name, string value)
    {
        builder.Append('[');
        builder.Append(name);
        builder.Append(" \"");
        builder.Append(EscapeTagValue(value));
        builder.AppendLine("\"]");
    }

    private static string EscapeTagValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void AppendMoves(StringBuilder builder, GameRecord record)
    {
        if (record.Moves == null || record.Moves.Count == 0)
            return;

        GetMovetextOrigin(record, out int firstPly, out int firstFullmove);
        for (int i = 0; i < record.Moves.Count; i++)
            builder.Append(FormatMovetextEntry(record.Moves[i], i, firstPly, firstFullmove));
        builder.AppendLine();
    }

    public static void GetMovetextOrigin(GameRecord record, out int firstPly, out int firstFullmove)
    {
        firstPly = 0;
        firstFullmove = 1;
        if (record == null || record.SetUp != "1" || string.IsNullOrEmpty(record.Ifen))
            return;
        RecordedPosition position = IfenParser.Parse(record.Ifen);
        firstPly = position.BlackToMove ? 1 : 0;
        firstFullmove = position.FullmoveNumber;
    }
}
