using System;
using System.Threading;

using UnityEngine;
using UnityEngine.UI;

public class TimeControlView : MonoBehaviour
{
    [SerializeField] private GameObject gameObject;
    [SerializeField] private Text opponentTimeLabel;
    [SerializeField] private Text myTimeLabel;
    [SerializeField] private GameObject opponentPanel;
    [SerializeField] private GameObject myPanel;

    public delegate void DisplayTime(int time);

    public DisplayTime DisplayWhiteTime;
    public DisplayTime DisplayBlackTime;
    public Action DisplayWhiteTurn;
    public Action DisplayBlackTurn;

    private int myTime;
    private int opponentTime;
    private bool turn;
    private bool timeRun;

    private Thread myTimeRunner;
    private Thread opponentTimeRunner;

    public bool Team
    {
        set
        {
            turn = !value;
            if (value)
            {
                DisplayWhiteTime = DisplayOpponentTime;
                DisplayBlackTime = DisplayMyTime;

                DisplayWhiteTurn = DisplayOpponentTurn;
                DisplayBlackTurn = DisplayMyTurn;
            }
            else
            {
                DisplayWhiteTime = DisplayMyTime;
                DisplayBlackTime = DisplayOpponentTime;

                DisplayWhiteTurn = DisplayMyTurn;
                DisplayBlackTurn = DisplayOpponentTurn;
            }
        }
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void StartRunTime()
    {
        timeRun = true;
        DisplayWhiteTurn();
        myTimeRunner = new Thread(() => ShowRunningTime(DisplayMyTime, ref myTime, true));
        opponentTimeRunner = new Thread(() => ShowRunningTime(DisplayOpponentTime, ref opponentTime, false));
        myTimeRunner.Start();
        opponentTimeRunner.Start();
    }
    public void Stop()
    {
        if (myTimeRunner == null && opponentTimeRunner == null) return;

        timeRun = false;
        myTimeRunner?.Join();
        opponentTimeRunner?.Join();
    }
    private void DisplayOpponentTime(int time)
    {
        opponentTime = time;
        MainTasks.AddTask(() => { if (opponentTimeLabel != null) opponentTimeLabel.text = TimeToString(time); });
    }

    private void DisplayMyTime(int time)
    {
        myTime = time;
        MainTasks.AddTask(() => { if (myTimeLabel != null) myTimeLabel.text = TimeToString(time); });
    }

    private void DisplayOpponentTurn()
    {
        turn = false;

        opponentPanel.SetActive(true);
        myPanel.SetActive(false);
    }

    private void DisplayMyTurn()
    {
        turn = true;

        myPanel.SetActive(true);
        opponentPanel.SetActive(false);
    }

    private string TimeToString(int milliseconds)
    {
        int totalSeconds = milliseconds / 1000;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }

    private void ShowRunningTime(DisplayTime displayTime, ref int time, bool neededTurn)
    {
        const int SleepTime = 100;
        while (timeRun)
        {
            if (turn != neededTurn)
            {
                Thread.Sleep(SleepTime);
                continue;
            }
            DateTime beginTime = DateTime.Now;
            Thread.Sleep(SleepTime);
            DateTime endTime = DateTime.Now;
            int elapsedTime = (int)(endTime - beginTime).TotalMilliseconds;
            displayTime(time - elapsedTime);
        }
    }
}
