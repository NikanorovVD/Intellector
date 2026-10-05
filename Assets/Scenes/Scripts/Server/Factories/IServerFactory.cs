public interface IServerFactory
{
    IGameCreator MakeGameCreator();
    IGameJoiner MakeGameJoiner();
    IGamesReader MakeGamesReader();
    INetworkGameManager MakeNetworkGameManager();
    IServerListener MakeServerListener();
}
