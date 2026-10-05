using System;
using System.IO;

using UnityEngine;

public class GameRecorder : MonoBehaviour
{
    public const int NoTransformInfo = 200;

    [SerializeField] private Board board;

    private GameRecord record;
    private string filePath;

    private int firstPly;
    private int firstFullmove;

    private void Start()
    {
        if (Settings.GameMode == GameMode.Replay) return;
        board.MoveStartEvent += MoveStartHandler;
        board.EndGameEvent += EndGameHandler;
        board.RestartEvent += BeginNewGame;
        BeginNewGame();
    }

    private void OnDestroy()
    {
        if (Board == null) return;
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

        filePath = CreateFilePath(mode);
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

    private void EndGameHandler(bool? winner, EndGameReason reason)
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

    private static (string white, string black) ResolvePlayerNames(GameMode mode, bool playerTeam)
    {
        string userName = string.IsNullOrEmpty(Settings.UserName) ? "Player" : Settings.UserName;

        if (mode == GameMode.AI)
        {
            if (!playerTeam)
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
            if (playerTeam)
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

    private static string CreateFilePath(GameMode mode)
    {
        Directory.CreateDirectory(GamesDirectory);
        return Path.Combine(GamesDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{mode}.ipgn");
    }

    public static string GamesDirectory => Path.Combine(Application.persistentDataPath, "Games");
}
