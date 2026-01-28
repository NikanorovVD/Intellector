using UnityEngine;

public class UserConfig
{
    public string UserName { get; set; }
    public PieceMaterials Material { get; set; }
    public bool AutoRotateCameraInLocalGame { get; set; } = true;

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
            UserName = PlayerPrefs.GetString(nameof(UserName)),
            Material = (PieceMaterials)PlayerPrefs.GetInt(nameof(Material)),
            AutoRotateCameraInLocalGame = PlayerPrefs.GetInt(nameof(AutoRotateCameraInLocalGame)) == 1 ? true : false
        };
    }
}

