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
        GetComponent<AISetupMenu>().Open(GameMode.Local);
    }

    public void AIGame()
    {
        GetComponent<AISetupMenu>().Open(GameMode.AI);
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
