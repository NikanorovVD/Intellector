using System.Net.Sockets;

using static Networking;

public interface IGameJoiner
{
    (bool, GameInfo) JoinGame(uint gameId);
}

public class GameJoiner : IGameJoiner
{
    public (bool, GameInfo) JoinGame(uint gameId)
    {
        const byte JoinGameCode = 30;
        const byte NoSuchGameAns = 99;

        TcpClient server = ServerConnection.GetConnection().Client;
        NetworkStream stream = server.GetStream();

        SendCode(JoinGameCode, stream);
        SendCode((byte)gameId, stream);

        GameInfo gameInfo = RecvGameInfo(stream);

        byte ans = RecvCode(stream);
        if (ans == NoSuchGameAns) return (false, null);

        bool team = ans != 0;
        gameInfo.Team = team;

        return (true, gameInfo);
    }
}
