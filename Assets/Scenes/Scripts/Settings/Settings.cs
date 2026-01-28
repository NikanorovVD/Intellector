using UnityEngine;

public class Settings
{
    public const int Version = 16;
    private static Connection _serverConnection;
    private static UserConfig _userConfig;
    public static IServerFactory ServerFactory { get; private set; }

    static Settings()
    {
        ServerFactory = new TCPServerFactory();
    }

    public static GameMode GameMode { get; set; }
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
}
