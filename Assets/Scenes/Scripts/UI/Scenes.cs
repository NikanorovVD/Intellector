using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes: MonoBehaviour
{
    public void ChangeScenes(int numberScenes)
    {
        SceneManager.LoadScene(numberScenes);
    }

    public void LocalGame()
    {
        GetComponent<LocalSetupMenu>().Open();
    }

    public void AIGame()
    {
        GetComponent<AISetupMenu>().Open();
    }

    public void OpenReplay()
    {
        Settings.GameMode = GameMode.Replay;
        Settings.ReplayFilePath = HistoryMenu.NewestPath();
        Settings.ClearStartPosition();
        SceneManager.LoadScene(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
