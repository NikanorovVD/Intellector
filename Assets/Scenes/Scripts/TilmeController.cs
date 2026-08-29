using UnityEngine;

public class TimeController : MonoBehaviour
{
    [SerializeField] private NetworkManager network_manager;
    [SerializeField] private TimeControlView view;
    [SerializeField] private Board board;

    private int white_time;
    private int black_time;

    private TimeContol time_control;

    // FIXME: дублирование состояния board.Turn
    private bool turn;

    // FIXME: дублирование состояния time_control.Active
    private bool active;
    
    private int WhiteTime
    {
        get { return white_time; }
        set
        {
            white_time = value;
            view.DisplayWhiteTime(white_time);
        }
    }
    private int BlackTime
    {
        get { return black_time; }
        set
        {
            black_time = value;
            view.DisplayBlackTime(black_time);
        }
    }

    private void Awake()
    {
        if (Settings.GameMode == GameMode.Network)
        {
            network_manager.TimeEvent += TimeReceived;
            network_manager.GameStartEvent += StartGame;
            board.RestartEvent += StartGame;
            board.EndGameEvent += (_, __) => EndGame();
        }
    }

    public void StartGame()
    {
        GameInfo gameInfo = GameInfo.Load();
        time_control = gameInfo.TimeContol;
        active = time_control.Active;
        if (!active) return;

        view.Team = board.PlayerTeam;
        turn = false;
        WhiteTime = time_control.MaxMilliseconds;
        BlackTime = time_control.MaxMilliseconds;
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
