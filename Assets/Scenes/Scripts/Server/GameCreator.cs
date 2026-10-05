using System;
using System.Net.Sockets;
using System.Threading.Tasks;

using static Networking;

public interface IGameCreator
{
    void CreateGame(GameInfo gameInfo, Action onConnect);
    void CancelGameCreate();
}

public class GameCreator : IGameCreator
{
    private static bool stillWaiting;
    private GameInfo gameInfo;
    private Task waiter;
    private bool connected;

    public async void CreateGame(GameInfo gameInfo, Action onConnect)
    {
        this.gameInfo = gameInfo;
        stillWaiting = true;
        waiter = Task.Run(() => ConnectionWait());
        await waiter;
        if (connected)
        {
            onConnect();
        }
        else
        {
            ServerConnection.GetConnection().Close();
        }
    }

    public void CancelGameCreate()
    {
        stillWaiting = false;
    }

    private void ConnectionWait()
    {
        const byte WhiteTeamCode = 0;
        const byte BlackTeamCode = 1;
        const byte CreateGameRequestCode = 40;
        const byte ContinueWaitingCode = 1;
        const byte StopCode = 0;

        TcpClient server = ServerConnection.GetConnection().Client;
        NetworkStream stream = server.GetStream();

        SendCode(CreateGameRequestCode, stream);
        SendGameInfo(gameInfo, stream);
        do
        {
            byte serverAns = RecvCode(stream);
            if (serverAns == WhiteTeamCode || serverAns == BlackTeamCode)
            {
                gameInfo.Team = serverAns == BlackTeamCode;
                gameInfo.Save();
                stillWaiting = false;
                connected = true;
                return;
            }
            SendCode((stillWaiting) ? ContinueWaitingCode : StopCode, stream);
        } while (stillWaiting);
    }
}
