using System;
using System.Collections.Generic;
using System.Net.Sockets;

using static Networking;

public interface IGamesReader
{
    List<GameInfo> ReadGames();
}

public class GamesReader : IGamesReader
{
    public List<GameInfo> ReadGames()
    {
        const byte GamesListRequest = 100;

        TcpClient server = ServerConnection.GetConnection().Client;
        NetworkStream stream = server.GetStream();

        SendCode(GamesListRequest, stream);
        int gameCount = RecvInt(stream);

        List<GameInfo> games = new List<GameInfo>();
        for (int i = 0; i < gameCount; i++)
        {
            games.Add(RecvGameInfo(stream));
        }
        return games;
    }
}

public class VersionException : Exception
{
    public VersionException(string message) : base(message) { }
}
