using System;

using UnityEngine;

public class LocalTimeController : MonoBehaviour
{
    [SerializeField] private TimeControlView view;
    [SerializeField] private Board board;
    [SerializeField] private CameraChange cameraChange;

    private int whiteTime;
    private int blackTime;
    private TimeControl timeControl;
    private bool running;
    private bool clockVisible;
    private bool bound;
    private float pendingMilliseconds;

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

    private void Start()
    {
        if (Settings.GameMode != GameMode.Local || !Settings.Playing)
            return;

        board.RestartEvent += StartGame;
        board.MoveEndEvent += MoveEnded;
        board.EndGameEvent += GameEnded;
        if (cameraChange != null)
            cameraChange.ViewSideChanged += ViewingSideChanged;
        bound = true;
        StartGame();
    }

    private void OnDestroy()
    {
        if (cameraChange != null)
            cameraChange.ViewSideChanged -= ViewingSideChanged;
        if (!bound || board == null)
            return;

        board.RestartEvent -= StartGame;
        board.MoveEndEvent -= MoveEnded;
        board.EndGameEvent -= GameEnded;
    }

    private void Update()
    {
        if (!running || board.IsGameOver)
            return;

        pendingMilliseconds += Time.deltaTime * 1000f;
        int step = (int)pendingMilliseconds;
        if (step <= 0)
            return;

        pendingMilliseconds -= step;
        if (board.Turn == Side.White)
            WhiteTime = Math.Max(0, whiteTime - step);
        else
            BlackTime = Math.Max(0, blackTime - step);

        int left = board.Turn == Side.White ? whiteTime : blackTime;
        if (left > 0)
            return;

        running = false;
        board.GameOver(Sides.Opposite(board.Turn), EndGameReason.TimesUp);
    }

    private void StartGame()
    {
        GameInfo gameInfo = GameInfo.Load();
        timeControl = gameInfo.TimeControl;
        running = false;
        clockVisible = false;
        pendingMilliseconds = 0f;
        if (timeControl == null || timeControl.IsUnlimited)
            return;

        view.Team = ViewingSide();
        WhiteTime = timeControl.BaseMilliseconds;
        BlackTime = timeControl.BaseMilliseconds;
        view.Activate();
        ShowTurn();
        clockVisible = true;
        running = true;
    }

    private void MoveEnded(Vector2Int start, Vector2Int end, int transformInfo)
    {
        if (!running || board.IsGameOver)
            return;

        Side moved = Sides.Opposite(board.Turn);
        int next = timeControl.IncrementMilliseconds;
        if (moved == Side.White)
            WhiteTime = whiteTime + next;
        else
            BlackTime = blackTime + next;
        ShowTurn();
    }

    private void GameEnded(Side? winner, EndGameReason reason)
    {
        running = false;
    }

    private void ViewingSideChanged(Side side)
    {
        if (!clockVisible)
            return;

        view.Team = side;
        WhiteTime = whiteTime;
        BlackTime = blackTime;
        ShowTurn();
    }

    private Side ViewingSide()
    {
        return cameraChange != null ? cameraChange.ViewSide : board.PlayerTeam;
    }

    private void ShowTurn()
    {
        if (board.Turn == Side.White)
            view.DisplayWhiteTurn();
        else
            view.DisplayBlackTurn();
    }
}
