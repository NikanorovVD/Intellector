using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading;

public class NetworkGamesScene : MonoBehaviour
{
    [SerializeField] GameObject NetworkGamePrefab;
    [SerializeField] GameObject Content;
    [SerializeField] GameObject GameInfoWindow;
    [SerializeField] GameObject ErrorWindow;
    [SerializeField] GameObject WaitingWindow;
    [SerializeField] public Color DefaultColor;
    [SerializeField] public Color SelectedColor;
    [SerializeField] GameObject[] Buttons;

    private SignalrConnection signalrConnection;
    private HttpConnection httpConnection;

    private readonly List<GameObject> Items = new List<GameObject>();
    public int SelectedId;
    private int createdLobbyId;

    void Start()
    {
        httpConnection = ConnectionManager.HttpConnection;
        signalrConnection = ConnectionManager.SignalrConnection;

        if (!ConnectionManager.Authorized)
        {
            ErrorWindow.SetActive(true);
            ErrorWindow.GetComponentInChildren<Text>().text = "Игра уже не существует";
        }

        ShowGamesList();
    }

    public async void ShowGamesList()
    {
        ClearItems();
        try
        {
            IEnumerable<OpenLobbyDto> lobbies = await httpConnection.GetOpenLobbies();
            foreach (var lobby in lobbies)
            {
                DisplayGame(lobby);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ErrorWindow.SetActive(true);
            DeactivateButtons();
        }
    }

    private void DeactivateButtons()
    {
        foreach (var button in Buttons)
            button.SetActive(false);
    }

    private void DisplayGame(OpenLobbyDto game)
    {
        GameObject netGameObj = Instantiate(NetworkGamePrefab);
        NetworkGameItem netGame = netGameObj.GetComponent<NetworkGameItem>();

        netGame.GameInfo = game;
        netGame.NetworkGameScene = this;
        netGameObj.transform.SetParent(Content.GetComponent<Transform>(), transform);
        netGame.DisplayGameInfo();
        netGame.SetDefaultColor();
        Items.Add(netGameObj);
    }

    private void ClearItems()
    {
        foreach (GameObject item in Items)
        {
            Destroy(item);
        }
        Items.Clear();
    }

    public void ShowGameInfoWindow()
    {
        GameInfoWindow.SetActive(true);
    }

    public void SetDefaultColors()
    {
        foreach (GameObject game_obj in Items)
        {
            game_obj.GetComponent<NetworkGameItem>().SetDefaultColor();
        }
    }

    public async void JoinSelectedGame()
    {
        if (SelectedId != 0)
        {
            try
            {
                GameInfoResponse response = await httpConnection.JoinOpenLobby(SelectedId);

                GameSettings gameInfo = new GameSettings()
                {
                    GameId = response.GameId,
                    Team = response.PlayerColor == PlayerColor.White, 
                    TimeControl = response.TimeControl == null ? null : new TimeControl()
                    {
                        TotalSeconds = response.TimeControl.TotalSeconds,
                        AddedMinutes = response.TimeControl.AddedSeconds
                    }
                };
                gameInfo.Save();

                GoToGameScene();
            }
            catch (HttpRequestException)
            {
                // проверить на 404
                ErrorWindow.SetActive(true);
                ErrorWindow.GetComponentInChildren<Text>().text = "Игра уже не существует";
                return;
            }
        }
    }

    public async void CreateGameConfirmClick()
    {
        CreateOpenLobbyRequest gameInfo = GameInfoWindow.GetComponent<GameInfoWindow>().GetGameInfo();
        if (gameInfo != null)
        {
            WaitingWindow.SetActive(true);
            createdLobbyId = await httpConnection.CreateLobby(gameInfo);

            // подписка на signalR старт игры
            CancellationTokenSource tokenSource = new();
            CancellationToken cancellationToken = tokenSource.Token;
            signalrConnection.Connection.On<GameInfoResponse>("ReceiveGameInfo", gi =>
            {
                tokenSource.Cancel();
                GameSettings gameSettings = new GameSettings()
                {
                    GameId = gi.GameId,
                    Team = gi.PlayerColor == PlayerColor.White, 
                    TimeControl = gi.TimeControl == null ? null : new TimeControl()
                    {
                        TotalSeconds = gi.TimeControl.TotalSeconds,
                        AddedSeconds = gi.TimeControl.AddedSeconds
                    }
                };
                gameSettings.Save();
                MainTasks.AddTask(GoToGameScene);
            });

            // оправка обновлений
#pragma warning disable CS4014 
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await httpConnection.UpdateLobby(createdLobbyId);
                        await Task.Delay(5000, cancellationToken);
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
#pragma warning restore CS4014 
        }
    }

    public async void CancelWaiting()
    {
        await httpConnection.DeleteLobby(createdLobbyId);
        WaitingWindow.SetActive(false);
    }

    private void GoToGameScene()
    {
        Settings.GameMode = GameMode.Network;
        SceneManager.LoadScene(1);
    }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    }
}
