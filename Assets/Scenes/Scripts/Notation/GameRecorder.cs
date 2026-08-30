using System;
using System.IO;
using UnityEngine;

public class GameRecorder : MonoBehaviour
{
    public const int NoTransformInfo = 200;

    [SerializeField] public Board Board;

    private GameRecord record;
    private string filePath;

    void Start()
    {
        if (Settings.GameMode == GameMode.Replay) return;
        Board.MoveStartEvent += MoveStartHandler;
        Board.EndGameEvent += EndGameHandler;
        Board.RestartEvent += BeginNewGame;
        BeginNewGame();
    }

    void OnDestroy()
    {
        if (Board == null) return;
        Board.MoveStartEvent -= MoveStartHandler;
        Board.EndGameEvent -= EndGameHandler;
        Board.RestartEvent -= BeginNewGame;
    }

    private void BeginNewGame()
    {
        DateTime utcNow = DateTime.UtcNow;
        GameMode mode = Settings.GameMode;
        (string white, string black) = ResolvePlayerNames(mode, Board.PlayerTeam);

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
            AppVersion = Settings.APP_VERSION.ToString()
        };

        filePath = CreateFilePath(mode);
        WriteRecord(record);
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        IPiece moving = Board.pieces[start.x][start.y];
        if (moving == null) return;

        IPiece target = Board.pieces[end.x][end.y];
        bool castling = target != null && target.Team == moving.Team;
        bool capture = target != null && target.Team != moving.Team;
        PieceType? transformation = null;
        if (transform_info != NoTransformInfo && transform_info != (int)moving.Type)
            transformation = (PieceType)transform_info;

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
        record.Termination = reason.ToString();
        WriteRecord(record);
    }

    private void AppendMove(RecordedMove move)
    {
        File.AppendAllText(filePath, IpgnFormatter.FormatMovetextEntry(move, record.Moves.Count - 1));
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
            if (AI.AI_team)
                return (userName, AI.DisplayName);
            return (AI.DisplayName, userName);
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
        TimeContol timeControl = GameInfo.Load().TimeContol;
        if (timeControl == null || !timeControl.Active)
            return "-";
        return timeControl.ToString();
    }

    private static string CreateFilePath(GameMode mode)
    {
        Directory.CreateDirectory(GamesDirectory);
        return Path.Combine(GamesDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{mode}.ipgn");
    }

    public static string GamesDirectory => Path.Combine(Application.persistentDataPath, "Games");
}
