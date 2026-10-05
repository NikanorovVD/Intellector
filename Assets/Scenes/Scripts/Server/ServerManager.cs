using System;
using System.Collections.Generic;

using UnityEngine;

public class ServerManager
{
    public ServerConnection Connection { get => ServerConnection.GetConnection(); }

    private readonly IGamesReader gamesReader;
    private readonly IGameJoiner gameJoiner;
    private readonly IGameCreator gameCreator;
    private readonly INetworkGameManager networkManager;
    private readonly IServerListener serverListener;

    private static ServerManager instance;

    private ServerManager()
    {
        gameCreator = Settings.ServerFactory.MakeGameCreator();
        gameJoiner = Settings.ServerFactory.MakeGameJoiner();
        gamesReader = Settings.ServerFactory.MakeGamesReader();
        networkManager = Settings.ServerFactory.MakeNetworkGameManager();
        serverListener = Settings.ServerFactory.MakeServerListener();
    }
    public static ServerManager GetInstance()
    {
        instance = instance ?? new ServerManager();
        return instance;
    }

    public void CreateGame(GameInfo gameInfo, Action onConnect) => gameCreator.CreateGame(gameInfo, onConnect);
    public void CancelGameCreate() => gameCreator.CancelGameCreate();
    public (bool, GameInfo) JoinGame(uint gameId) => gameJoiner.JoinGame(gameId);
    public List<GameInfo> ReadGames() => gamesReader.ReadGames();

    public void SendMove(Vector2Int start, Vector2Int end, int transformInfo) => networkManager.SendMove(start, end, transformInfo);
    public void SendRematch() => networkManager.SendRematch();
    public void SendExit() => networkManager.SendExit();
    public void RegisterObserver(IServerListenerObserver observer) => serverListener.RegisterObserver(observer);
    public void UnregisterObserver(IServerListenerObserver observer) => serverListener.UnregisterObserver(observer);
    public void ListenServer() => serverListener.ListenServer();
}
