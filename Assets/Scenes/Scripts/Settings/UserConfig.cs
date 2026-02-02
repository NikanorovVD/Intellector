using UnityEngine;

public class UserConfig
{
    public string UserName { get; set; }
    public PieceMaterials Material { get; set; }
    public bool AutoRotateCameraInLocalGame { get; set; }

    public void Save()
    {
        PlayerPrefs.SetString(nameof(UserName), UserName);
        PlayerPrefs.SetInt(nameof(Material), (int)Material);
        PlayerPrefs.SetInt(nameof(AutoRotateCameraInLocalGame), AutoRotateCameraInLocalGame ? 1 : 0);
    }

    public static UserConfig Load()
    {
        return new UserConfig
        {
            UserName = PlayerPrefs.GetString(nameof(UserName), defaultValue: string.Empty),
            Material = (PieceMaterials)PlayerPrefs.GetInt(nameof(Material), defaultValue: 0),
            AutoRotateCameraInLocalGame = PlayerPrefs.GetInt(nameof(AutoRotateCameraInLocalGame), defaultValue: 1) == 1 ? true : false
        };
    }
}

