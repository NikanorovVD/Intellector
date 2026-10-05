using System.Net.Sockets;
using System;
using System.Threading.Tasks;

using static LogWriter;

using UnityEngine;

public class NetworkManager : MonoBehaviour, IServerListenerObserver
{
    [SerializeField] private Board board;
    [SerializeField] private GameObject waitScreen;

    private static bool readyForRematch = false;

    public event Action ExitEvent;
    public event Action RematchEvent;
    public event Action GameStartEvent;

    public delegate void TimeReceived(int time);
    public event TimeReceived TimeEvent;

    private void Start()
    {
        if (Settings.GameMode == GameMode.Network)
        {
            ServerConnection connection = ServerConnection.GetConnection();

            board.MoveStartEvent += MoveEventHandler;
            ServerManager.GetInstance().RegisterObserver(this);
            new TaskFactory().StartNew(ServerManager.GetInstance().ListenServer, TaskCreationOptions.LongRunning);
            GameStartEvent?.Invoke();
        }
    }

    public void ExecuteReceivedMove(Vector2Int start, Vector2Int end, int transformInfo)
    {
        try
        {
           board.MovePiece(start, end, transformInfo);
        }
        catch (Exception e)
        {
            WriteLog(e.Message);
        }
    }

    private void MoveEventHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        if (board.Pieces[start.x][start.y].Team == board.PlayerTeam)
        {
            ServerManager.GetInstance().SendMove(start, end, transformInfo);
            WriteLog($"Отправка хода: {start} ; {end} ; {transformInfo}");
        }
    }

    public async void AskRematch()
    {
        if (board.NetworkGame)
        {
            ServerManager.GetInstance().SendRematch();
            // FIXME: переусложненная логика, можно было этот код повесить OnRematchReceived
            await new TaskFactory().StartNew(() => { while (!readyForRematch) { } }, TaskCreationOptions.LongRunning);

            MainTasks.AddTask(() => board.Restart());
            readyForRematch = false;
            return;
        }
        else
        {
            board.Restart();
        }
    }

    public void SendExit()
    {
        ServerManager.GetInstance().SendExit();
    }

    public void OnMoveReceived(Vector2Int start, Vector2Int end, int transformInfo)
    {
        WriteLog($"Получен ход: {start} -> {end} : {transformInfo} ");
        MainTasks.AddTask(() => ExecuteReceivedMove(start, end, transformInfo));
    }

    public void OnTimeReceived(int time)
    {
        MainTasks.AddTask(() => TimeEvent?.Invoke(time));
    }

    public void OnExitReceived()
    {
        MainTasks.AddTask(() => {
            board.GameOver(board.PlayerTeam, EndGameReason.Exit);
            ExitEvent?.Invoke();
        });
    }

    public void OnRematchReceived()
    {
        readyForRematch = true;
        MainTasks.AddTask(() => RematchEvent?.Invoke());
    }

    public void OnTimeOutReceived(bool exitTeam)
    {
        MainTasks.AddTask(() => board.GameOver(exitTeam, EndGameReason.TimesUp));
    }
}
