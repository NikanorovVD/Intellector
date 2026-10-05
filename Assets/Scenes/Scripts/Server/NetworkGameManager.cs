using static Networking;

using UnityEngine;

public interface INetworkGameManager
{
    void SendMove(Vector2Int start, Vector2Int end, int transformInfo);
    void SendExit();
    void SendRematch();
}

public class NetworkGameManager : INetworkGameManager
{
    public void SendMove(Vector2Int start, Vector2Int end, int transformInfo)
    {
        Networking.SendMove(
            new byte[5] { (byte)start.x, (byte)start.y, (byte)end.x, (byte)end.y, (byte)transformInfo },
            ServerConnection.GetConnection().Client.GetStream());
    }
    public void SendExit()
    {
        const byte ExitCode = 111;
        SendCode(ExitCode, ServerConnection.GetConnection().Client.GetStream());
        ServerConnection.GetConnection().Close();
    }
    public void SendRematch()
    {
        const byte RematchCode = 222;
        SendCode(RematchCode, ServerConnection.GetConnection().Client.GetStream());
    }
}
