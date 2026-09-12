using UnityEngine;

public class UserConfig
{
    public string UserName { get; set; }
    public PieceMaterials Material { get; set; }
    public bool AutoRotateCameraInLocalGame { get; set; }
    public AISettings AI { get; set; }

    public void Save()
    {
        PlayerPrefs.SetString(nameof(UserName), UserName);
        PlayerPrefs.SetInt(nameof(Material), (int)Material);
        PlayerPrefs.SetInt(nameof(AutoRotateCameraInLocalGame), AutoRotateCameraInLocalGame ? 1 : 0);
        PlayerPrefs.SetInt(nameof(AISearchMode), (int)AI.Mode);
        PlayerPrefs.SetInt("AIDepth", AI.Depth);
        PlayerPrefs.SetInt("AISearchTimeMs", AI.SearchTimeMs);
        PlayerPrefs.SetInt("AILevel", AI.Level);
        PlayerPrefs.SetInt("AIColor", (int)AI.Color);
    }

    public static UserConfig Load()
    {
        return new UserConfig
        {
            UserName = PlayerPrefs.GetString(nameof(UserName), defaultValue: string.Empty),
            Material = (PieceMaterials)PlayerPrefs.GetInt(nameof(Material), defaultValue: 0),
            AutoRotateCameraInLocalGame = PlayerPrefs.GetInt(nameof(AutoRotateCameraInLocalGame), defaultValue: 1) == 1 ? true : false,
            AI = new AISettings
            {
                Mode = (AISearchMode)PlayerPrefs.GetInt(nameof(AISearchMode), defaultValue: (int)AISearchMode.Depth),
                Depth = PlayerPrefs.GetInt("AIDepth", defaultValue: AISettings.DefaultDepth),
                SearchTimeMs = PlayerPrefs.GetInt("AISearchTimeMs", defaultValue: AISettings.DefaultSearchTimeMs),
                Level = PlayerPrefs.GetInt("AILevel", defaultValue: AISettings.DefaultLevel),
                Color = (ColorChoice)PlayerPrefs.GetInt("AIColor", defaultValue: (int)ColorChoice.random)
            }
        };
    }
}
