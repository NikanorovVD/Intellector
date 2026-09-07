using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes: MonoBehaviour
{
    public void ChangeScenes(int numberScrenes)
    {
        SceneManager.LoadScene(numberScrenes);
    }

    public void LocalGame()
    {
        Settings.GameMode = GameMode.Local;
        ChangeScenes(1);
    }

    public void AIGame()
    {
        GetComponent<AISetupMenu>().Open();
    }

    public void ShowHistory()
    {
        GetComponent<HistoryMenu>().Open();
    }

    public void Exit()
    {
        Application.Quit();
    }
}
