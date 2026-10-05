public class TCPServerFactory : IServerFactory
{
    public IGameCreator MakeGameCreator()
    {
        return new GameCreator();
    }

    public IGameJoiner MakeGameJoiner()
    {
        return new GameJoiner();
    }

    public IGamesReader MakeGamesReader()
    {
        return new GamesReader();
    }

    public INetworkGameManager MakeNetworkGameManager()
    {
        return new NetworkGameManager();
    }

    public IServerListener MakeServerListener()
    {
        return new ServerListener();
    }
}
