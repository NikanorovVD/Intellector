using System;
using System.IO;
using System.Text;

using UnityEngine;

public class GameRecorder : MonoBehaviour
{
    public const int NoTransformInfo = 200;
    public const string AnalysisEvent = "Analysis";

    [SerializeField] private Board board;

    private GameRecord record;
    private string filePath;

    private int firstPly;
    private int firstFullmove;

    private void Start()
    {
        if (!Settings.Playing) return;
        board.MoveStartEvent += MoveStartHandler;
        board.EndGameEvent += EndGameHandler;
        board.RestartEvent += BeginNewGame;
        BeginNewGame();
    }

    private void OnDestroy()
    {
        if (board == null) return;
        board.MoveStartEvent -= MoveStartHandler;
        board.EndGameEvent -= EndGameHandler;
        board.RestartEvent -= BeginNewGame;
    }

    private void BeginNewGame()
    {
        DateTime utcNow = DateTime.UtcNow;
        GameMode mode = Settings.GameMode;
        (string white, string black) = ResolvePlayerNames(mode, board.PlayerTeam);

        record = new GameRecord
        {
            Event = ResolveEvent(mode),
            Site = "Intellector",
            Date = utcNow.ToString("yyyy.MM.dd"),
            UTCTime = utcNow.ToString("HH:mm:ss"),
            White = white,
            Black = black,
            Result = GameRecord.UnfinishedResult,
            TimeControl = ResolveTimeControl(),
            GameMode = mode.ToString(),
            AppVersion = Settings.AppVersion.ToString()
        };
        if (!string.IsNullOrEmpty(Settings.StartIfen))
        {
            record.SetUp = "1";
            record.Ifen = Settings.StartIfen;
        }
        IpgnFormatter.GetMovetextOrigin(record, out firstPly, out firstFullmove);

        filePath = CreateGameFilePath(mode, white, black);
        WriteRecord(record);
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        IPiece moving = board.Pieces[start.x][start.y];
        if (moving == null) return;

        IPiece target = board.Pieces[end.x][end.y];
        bool castling = target != null && target.Team == moving.Team;
        bool capture = target != null && target.Team != moving.Team;
        PieceType? transformation = null;
        if (transformInfo != NoTransformInfo && transformInfo != (int)moving.Type)
            transformation = (PieceType)transformInfo;

        RecordedMove recordedMove = new RecordedMove
        {
            Piece = moving.Type,
            From = start,
            To = end,
            Capture = capture,
            Castling = castling,
            Transformation = transformation
        };
        record.Moves.Add(recordedMove);
        if (record.IsFinished)
            WriteRecord(record);
        else
            AppendMove(recordedMove);
    }

    private void EndGameHandler(Side? winner, EndGameReason reason)
    {
        record.Result = IpgnFormatter.FormatResult(winner);
        record.Termination = IpgnFormatter.FormatTermination(reason);
        WriteRecord(record);
    }

    private void AppendMove(RecordedMove move)
    {
        File.AppendAllText(filePath, IpgnFormatter.FormatMovetextEntry(move, record.Moves.Count - 1, firstPly, firstFullmove));
    }

    private void WriteRecord(GameRecord gameRecord)
    {
        File.WriteAllText(filePath, IpgnFormatter.Format(gameRecord));
    }

    private static (string white, string black) ResolvePlayerNames(GameMode mode, Side playerTeam)
    {
        string userName = string.IsNullOrEmpty(Settings.UserName) ? "Player" : Settings.UserName;

        if (mode == GameMode.AI)
        {
            if (playerTeam == Side.White)
                return (userName, Settings.AI.DisplayName);
            return (Settings.AI.DisplayName, userName);
        }

        if (mode == GameMode.Network)
        {
            GameInfo gameInfo = GameInfo.Load();
            string opponent = "Opponent";

            /* FIXME: В нормальной реализации в gameInfo.Name будет имя соперника, но пока что тут имя лобби,
               поэтому для его создателя оно совпадет с собственным именем.
            */
            if (!string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != userName)
                opponent = gameInfo.Name;
            if (playerTeam == Side.Black)
                return (opponent, userName);
            return (userName, opponent);
        }

        return ("White", "Black");
    }

    private static string ResolveEvent(GameMode mode)
    {
        if (mode != GameMode.Network)
            return mode.ToString();

        string roomName = GameInfo.Load().Name;
        return string.IsNullOrEmpty(roomName) ? mode.ToString() : roomName;
    }

    private static string ResolveTimeControl()
    {
        return IpgnFormatter.FormatTimeControl(GameInfo.Load().TimeControl);
    }

    private static string CreateGameFilePath(GameMode mode, string white, string black)
    {
        DateTime now = DateTime.Now;
        string name = mode == GameMode.Local
            ? $"Локальная_{now:yy-MM-dd_HH-mm}"
            : $"{SanitizeFileName(white)}-{SanitizeFileName(black)}_{now:yy-MM-dd_HH-mm-ss}";
        return AllocatePath(name);
    }

    public static string CreateAnalysisFilePath(string sourcePath)
    {
        string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        if (string.IsNullOrEmpty(sourceName))
            sourceName = "Реплей";
        return AllocatePath(sourceName + "_анализ");
    }

    private static string AllocatePath(string name)
    {
        Directory.CreateDirectory(GamesDirectory);
        string path = Path.Combine(GamesDirectory, name + ReplayFile.Extension);
        if (!File.Exists(path))
            return path;

        for (int number = 2; ; number++)
        {
            path = Path.Combine(GamesDirectory, $"{name}({number}){ReplayFile.Extension}");
            if (!File.Exists(path))
                return path;
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Player";

        string name = value.Trim().Replace('<', '(').Replace('>', ')');
        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (char symbol in name)
        {
            if (Array.IndexOf(invalid, symbol) < 0)
                builder.Append(symbol);
        }

        string sanitized = builder.ToString().Trim().TrimEnd('.');
        return sanitized.Length == 0 ? "Player" : sanitized;
    }

    public static string GamesDirectory => Path.Combine(Application.persistentDataPath, "Games");
}
