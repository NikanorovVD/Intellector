using UnityEngine;

public interface IServerListenerObserver
{
    void OnMoveReceived(Vector2Int start, Vector2Int end, int transformInfo);
    void OnTimeReceived(int time);
    void OnExitReceived();
    void OnRematchReceived();
    void OnTimeOutReceived(bool exitTeam);
}
