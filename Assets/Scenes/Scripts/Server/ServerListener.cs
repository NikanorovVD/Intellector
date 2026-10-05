using System.Collections.Generic;

using static Networking;

using UnityEngine;

public class ServerListener : IServerListener
{
    private List<IServerListenerObserver> observers = new List<IServerListenerObserver>();
    public void RegisterObserver(IServerListenerObserver observer)
    {
        observers.Add(observer);
    }
    public void UnregisterObserver(IServerListenerObserver observer)
    {
        observers.Remove(observer);
    }

    public void ListenServer()
    {
        const byte MoveCode = 10;
        const byte TimeCode = 20;
        const byte WhiteTimeOutCode = 30;
        const byte BlackTimeOutCode = 31;
        const byte ExitCode = 111;
        const byte RematchCode = 222;

        var serverStream = ServerConnection.GetConnection().Client.GetStream();
        while (true)
        {
            byte code = RecvCode(serverStream);
            switch (code)
            {
                case MoveCode:
                    byte[] move = RecvMove(serverStream);
                    Vector2Int start = new Vector2Int(move[0], move[1]);
                    Vector2Int end = new Vector2Int(move[2], move[3]);
                    int transformInfo = move[4];
                    MoveReceived(start, end, transformInfo);
                    break;
                case TimeCode:
                    int time = RecvInt(serverStream);
                    TimeReceived(time);
                    break;
                case ExitCode:
                    ExitReceived();
                    break;
                case WhiteTimeOutCode:
                    TimeOutReceived(true);
                    break;
                case BlackTimeOutCode:
                    TimeOutReceived(false);
                    break;
                case RematchCode:
                    RematchReceived();
                    break;
            }
        }
    }
    public void ExitReceived()
    {
        foreach(var observer in observers) observer.OnExitReceived();
    }
    public void MoveReceived(Vector2Int start, Vector2Int end, int transformInfo)
    {
        foreach (var observer in observers) observer.OnMoveReceived(start, end, transformInfo);
    }
    public void RematchReceived()
    {
        foreach (var observer in observers) observer.OnRematchReceived();
    }
    public void TimeOutReceived(bool exitTeam)
    {
        foreach (var observer in observers) observer.OnTimeOutReceived(exitTeam);
    }
    public void TimeReceived(int time)
    {
        foreach (var observer in observers) observer.OnTimeReceived(time);
    }
}
