using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private Board board;
    public void Exit()
    {
        if (board.NetworkGame)
            networkManager.SendExit();
        SceneManager.LoadScene(0);
    }
}
