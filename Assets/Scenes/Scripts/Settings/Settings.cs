using UnityEngine;

public class Settings
{
    // FIXME: сомнительно держать версию в коде
    public const int AppVersion = 17;
    private static Connection serverConnection;
    private static UserConfig userConfig;
    public static IServerFactory ServerFactory { get; private set; }

    static Settings()
    {
        ServerFactory = new TCPServerFactory();
    }

    public static GameMode GameMode { get; set; }
    public static bool PlayerTeam { get; set; }
    public static string ReplayFilePath { get; set; }
    public static string StartIfen { get; private set; }
    public static RecordedPosition StartPosition { get; private set; }
    public static PositionArrange Arrange { get; } = new PositionArrange();

    public sealed class PositionArrange
    {
        public bool Editing { get; set; }
        public bool Resume { get; set; }
        public GameMode Mode { get; set; }
        public string Backup { get; set; }
    }

    public static bool TrySetStartIfen(string raw, out string error)
    {
        raw = (raw ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            ClearStartPosition();
            error = null;
            return true;
        }

        if (!IfenParser.TryParse(raw, out RecordedPosition position, out error))
            return false;

        StartPosition = position;
        StartIfen = IfenFormatter.Format(position);
        return true;
    }

    public static void ClearStartPosition()
    {
        StartIfen = null;
        StartPosition = null;
    }

    public static Connection GetConnection()
    {
        if (serverConnection == null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("server_connection");
            serverConnection = JsonUtility.FromJson<Connection>(textAsset.text);
        }
        return serverConnection;
    }

    public static string UserName
    {
        get
        {
            userConfig ??= UserConfig.Load();
            return userConfig.UserName;
        }
        set
        {
            userConfig.UserName = value;
            userConfig.Save();
        }
    }

    public static PieceMaterials PieceMaterials
    {
        get
        {
            userConfig ??= UserConfig.Load();
            return userConfig.Material;
        }
        set
        {
            userConfig.Material = value;
            userConfig.Save();
        }
    }

    public static bool AutoRotateCameraInLocalGame
    {
        get
        {
            userConfig ??= UserConfig.Load();
            return userConfig.AutoRotateCameraInLocalGame;
        }
        set
        {
            userConfig.AutoRotateCameraInLocalGame = value;
            userConfig.Save();
        }
    }

    public static AISettings AI
    {
        get
        {
            userConfig ??= UserConfig.Load();
            return userConfig.AI.Clamped();
        }
        set
        {
            userConfig ??= UserConfig.Load();
            userConfig.AI = value.Clamped();
            userConfig.Save();
        }
    }
}
