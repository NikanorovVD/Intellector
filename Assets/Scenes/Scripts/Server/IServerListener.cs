using UnityEngine;

public interface IServerListener
{
    void RegisterObserver(IServerListenerObserver observer);
    void UnregisterObserver(IServerListenerObserver observer);
    void ListenServer();
    void MoveReceived(Vector2Int start, Vector2Int end, int transformInfo);
    void TimeReceived(int time);
    void ExitReceived();
    void RematchReceived();
    void TimeOutReceived(bool exitTeam);
}
