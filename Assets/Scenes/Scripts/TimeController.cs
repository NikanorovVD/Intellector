using UnityEngine;

public class TimeController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private TimeControlView view;
    [SerializeField] private Board board;

    private int whiteTime;
    private int blackTime;

    private TimeControl timeControl;

    // FIXME: дублирование состояния board.Turn
    private bool turn;

    // FIXME: дублирование состояния time_control.Active
    private bool active;

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
        active = timeControl.Active;
        if (!active) return;

        view.Team = board.PlayerTeam;
        turn = false;
        WhiteTime = timeControl.MaxMilliseconds;
        BlackTime = timeControl.MaxMilliseconds;
        view.Activate();
        view.StartRunTime();
    }

    public void EndGame()
    {
        if (active)
            view.Stop();
    }

    private void TimeReceived(int time)
    {
        if (turn) BlackTime = time;
        else WhiteTime = time;
        turn = !turn;
        if (turn) view.DisplayBlackTurn();
        else view.DisplayWhiteTurn();
    }
}
