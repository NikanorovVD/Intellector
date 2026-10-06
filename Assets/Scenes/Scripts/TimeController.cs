using UnityEngine;

public class TimeController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private TimeControlView view;
    [SerializeField] private Board board;

    private int whiteTime;
    private int blackTime;

    private TimeControl timeControl;

    private int WhiteTime
    {
        get { return whiteTime; }
        set
        {
            whiteTime = value;
            view.DisplayWhiteTime(whiteTime);
        }
    }
    private int BlackTime
    {
        get { return blackTime; }
        set
        {
            blackTime = value;
            view.DisplayBlackTime(blackTime);
        }
    }

    private void Awake()
    {
        if (Settings.GameMode == GameMode.Network)
        {
            networkManager.TimeEvent += TimeReceived;
            networkManager.GameStartEvent += StartGame;
            board.RestartEvent += StartGame;
            board.EndGameEvent += (_, __) => EndGame();
        }
    }

    public void StartGame()
    {
        GameInfo gameInfo = GameInfo.Load();
        timeControl = gameInfo.TimeControl;
        view.Stop();
        if (timeControl == null || timeControl.Unlimited) return;

        view.Team = board.PlayerTeam;
        WhiteTime = timeControl.BaseMilliseconds;
        BlackTime = timeControl.BaseMilliseconds;
        view.Activate();
        view.StartRunTime();
    }

    public void EndGame()
    {
        if (timeControl != null && !timeControl.Unlimited)
            view.Stop();
    }

    private void TimeReceived(int time)
    {
        if (board.Turn)
        {
            WhiteTime = time;
            view.DisplayBlackTurn();
        }
        else
        {
            BlackTime = time;
            view.DisplayWhiteTurn();
        }
    }
}
