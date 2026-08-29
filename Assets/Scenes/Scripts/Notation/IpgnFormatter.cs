using System;
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
        if (!string.IsNullOrEmpty(record.Termination))
            AppendTag(builder, "Termination", record.Termination);

        builder.AppendLine();
        return builder.ToString();
    }

    public static string FormatMovetextEntry(RecordedMove move, int index)
    {
        var builder = new StringBuilder();
        if (index % 2 == 0)
        {
            if (index > 0)
                builder.AppendLine();
            builder.Append(index / 2 + 1);
            builder.Append(". ");
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

        for (int i = 0; i < record.Moves.Count; i++)
            builder.Append(FormatMovetextEntry(record.Moves[i], i));
        builder.AppendLine();
    }
}
