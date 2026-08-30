using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class IpgnParser
{
    private static readonly Regex TagRegex = new(@"^\[(\w+)\s+""(.*)""\]\s*$", RegexOptions.Compiled);
    private static readonly Regex MoveRegex = new(
        @"^([PLIDFA])([a-i][1-7])(<->|x|-)([a-i][1-7])(?:=([PLIDFA]))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static GameRecord Parse(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var record = new GameRecord();
        bool inMovetext = false;

        foreach (string rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                inMovetext = true;
                continue;
            }

            if (!inMovetext && line[0] == '[')
            {
                ApplyTag(record, line);
                continue;
            }

            inMovetext = true;
            ParseMovetextLine(record, line);
        }

        return record;
    }

    private static void ApplyTag(GameRecord record, string line)
    {
        Match match = TagRegex.Match(line);
        if (!match.Success)
            throw new FormatException($"Некорректный тег: {line}");

        string name = match.Groups[1].Value;
        string value = UnescapeTagValue(match.Groups[2].Value);
        switch (name)
        {
            case "Event": record.Event = value; break;
            case "Site": record.Site = value; break;
            case "Date": record.Date = value; break;
            case "UTCTime": record.UTCTime = value; break;
            case "White": record.White = value; break;
            case "Black": record.Black = value; break;
            case "Result": record.Result = value; break;
            case "TimeControl": record.TimeControl = value; break;
            case "GameMode": record.GameMode = value; break;
            case "AppVersion": record.AppVersion = value; break;
            case "Termination": record.Termination = value; break;
        }
    }

    private static void ParseMovetextLine(GameRecord record, string line)
    {
        foreach (string token in line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsResultToken(token))
            {
                record.Result = token;
                continue;
            }
            if (IsMoveNumber(token))
                continue;
            record.Moves.Add(ParseMove(token));
        }
    }

    private static RecordedMove ParseMove(string token)
    {
        Match match = MoveRegex.Match(token);
        if (!match.Success)
            throw new FormatException($"Некорректный ход: {token}");

        string separator = match.Groups[3].Value;
        PieceType? transformation = null;
        if (match.Groups[5].Success && match.Groups[5].Value.Length > 0)
            transformation = IpgnFormatter.PieceFromLetter(match.Groups[5].Value[0]);

        return new RecordedMove
        {
            Piece = IpgnFormatter.PieceFromLetter(match.Groups[1].Value[0]),
            From = IpgnFormatter.ParseTile(match.Groups[2].Value),
            To = IpgnFormatter.ParseTile(match.Groups[4].Value),
            Capture = separator == "x",
            Castling = separator == "<->",
            Transformation = transformation
        };
    }

    private static bool IsResultToken(string token)
    {
        return token == GameRecord.UnfinishedResult || token == "1-0" || token == "0-1" || token == "1/2-1/2";
    }

    private static bool IsMoveNumber(string token)
    {
        if (token.Length < 2 || token[token.Length - 1] != '.')
            return false;
        for (int i = 0; i < token.Length - 1; i++)
        {
            if (!char.IsDigit(token[i]))
                return false;
        }
        return true;
    }

    private static string UnescapeTagValue(string value)
    {
        return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
