using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkGamesScene : MonoBehaviour
{
    [SerializeField] private GameObject networkGamePrefab;
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject gameInfoWindow;
    [SerializeField] private GameObject errorWindow;
    [SerializeField] private GameObject waitingWindow;
    [SerializeField] public Color DefaultColor;
    [SerializeField] public Color SelectedColor;
    [SerializeField] private GameObject[] buttons;

    private readonly List<GameObject> items = new List<GameObject>();
    public uint SelectedId;

    private void Start()
    {
        ShowGamesList();
    }

    public void ShowGamesList()
    {
        ClearItems();
        try
        {
            var games = ServerManager.GetInstance().ReadGames();
            foreach(var game in games)
            {
                DisplayGame(game);
            }
        }
        catch(VersionException e)
        {
            errorWindow.SetActive(true);
            errorWindow.GetComponentInChildren<Text>().text = e.Message;
            DeactivateButtons();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            errorWindow.SetActive(true);
            DeactivateButtons();
        }

        void DeactivateButtons() { foreach (var button in buttons) button.SetActive(false); }
    }

    private void DisplayGame(GameInfo game)
    {
        GameObject netGameObj = Instantiate(networkGamePrefab);
        NetworkGameItem netGame = netGameObj.GetComponent<NetworkGameItem>();

        netGame.GameInfo = game;
        netGame.NetworkGameScene = this;
        netGameObj.transform.SetParent(content.GetComponent<Transform>(), transform);

        RectTransform rect = netGameObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        netGame.DisplayGameInfo();
        netGame.SetDefaultColor();
        items.Add(netGameObj);
    }

    private void ClearItems()
    {
        foreach (GameObject item in items)
        {
            Destroy(item);
        }
        items.Clear();
    }

    public void ShowGameInfoWindow()
    {
        gameInfoWindow.SetActive(true);
    }

    public void SetDefaultColors()
    {
        foreach (GameObject gameObj in items)
        {
            gameObj.GetComponent<NetworkGameItem>().SetDefaultColor();
        }
    }

    public void JoinSelectedGame()
    {
        if(SelectedId != 0)
        {
            (bool connect, GameInfo gameInfo) = ServerManager.GetInstance().JoinGame(SelectedId);
            if (!connect)
            {
                errorWindow.SetActive(true);
                errorWindow.GetComponentInChildren<Text>().text = "Игра уже не существует";
                return;
            }
            gameInfo.Save();
            GoToGameScene();
        }
    }
    public void CreateGameConfirmClick()
    {
        GameInfo gameInfo = gameInfoWindow.GetComponent<GameInfoWindow>().GetGameInfo();
        if (gameInfo != null)
        {
            waitingWindow.SetActive(true);
            ServerManager.GetInstance().CreateGame(gameInfo,GoToGameScene);
        }
    }

    public void CancelWaiting()
    {
        ServerManager.GetInstance().CancelGameCreate();
        waitingWindow.SetActive(false);
    }

    private void GoToGameScene()
    {
        Settings.GameMode = GameMode.Network;
        Settings.ClearStartPosition();
        SceneManager.LoadScene(1);
    }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    }
}
