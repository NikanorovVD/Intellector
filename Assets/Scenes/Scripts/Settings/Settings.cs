using UnityEngine;

public class Settings
{
    // FIXME: сомнительно держать версию в коде
    public const int APP_VERSION = 17;
    private static Connection _serverConnection;
    private static UserConfig _userConfig;
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
        if (_serverConnection == null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("server_connection");
            _serverConnection = JsonUtility.FromJson<Connection>(textAsset.text);
        }
        return _serverConnection;
    }

    public static string UserName
    {
        get
        {
            _userConfig ??= UserConfig.Load();
            return _userConfig.UserName;
        }
        set
        {
            _userConfig.UserName = value;
            _userConfig.Save();
        }
    }

    public static PieceMaterials PieceMaterials
    {
        get
        {
            _userConfig ??= UserConfig.Load();
            return _userConfig.Material;
        }
        set
        {
            _userConfig.Material = value;
            _userConfig.Save();
        }
    }

    public static bool AutoRotateCameraInLocalGame
    {
        get
        {
            _userConfig ??= UserConfig.Load();
            return _userConfig.AutoRotateCameraInLocalGame;
        }
        set
        {
            _userConfig.AutoRotateCameraInLocalGame = value;
            _userConfig.Save();
        }
    }

    public static AISettings AI
    {
        get
        {
            _userConfig ??= UserConfig.Load();
            return _userConfig.AI.Clamped();
        }
        set
        {
            _userConfig ??= UserConfig.Load();
            _userConfig.AI = value.Clamped();
            _userConfig.Save();
        }
    }
}
