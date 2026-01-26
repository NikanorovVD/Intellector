using System.Net.Sockets;
using UnityEngine;
using System;
using static LogWriter;
using System.Threading.Tasks;
using Assets.Scenes.Scripts.Server;

public class NetworkManager : MonoBehaviour, IServerListenerObserver
{
    [SerializeField] private Board board;
    [SerializeField] private GameObject WaitScreen;


    static bool ready_for_rematch = false;

    public event Action ExitEvent;
    public event Action RematchEvent;
    public event Action GameStartEvent;

    public delegate void TimeReceived(int time);
    public event TimeReceived TimeEvent;
    

    void Start()
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

    public void ExecuteReceivedMove(Vector2Int start, Vector2Int end, int transform_info)
    {
        try
        {
           board.MovePiece(start, end, transform_info);
        }
        catch (Exception e)
        {
            WriteLog(e.Message);
        }
    }

    void MoveEventHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        if (board.pieces[start.x][start.y].Team == board.PlayerTeam)
        {
            ServerManager.GetInstance().SendMove(start, end, transform_info);
            WriteLog($"Îòïðàâêà õîäà: {start} ; {end} ; {transform_info}");
        }
    }

    public async void AskRematch()
    {
        //ÇÀ×ÅÌ ÒÀÊ ÑËÎÆÍÎ ÒÎ?????
        if (board.NetworkGame)
        {
            ServerManager.GetInstance().SendRematch();
            await new TaskFactory().StartNew(() => { while (!ready_for_rematch) { } }, TaskCreationOptions.LongRunning);

            MainTasks.AddTask(() => board.Restart());
            ready_for_rematch = false;
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

    public void OnMoveReceived(Vector2Int start, Vector2Int end, int transform_info)
    {
        WriteLog($"Ïîëó÷åí õîä: {start} -> {end} : {transform_info} ");
        MainTasks.AddTask(() => ExecuteReceivedMove(start, end, transform_info));
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
        ready_for_rematch = true;
        MainTasks.AddTask(() => RematchEvent?.Invoke());
    }

    public void OnTimeOutReceived(bool exit_team)
    {
        MainTasks.AddTask(() => board.GameOver(exit_team, EndGameReason.TimesUp));
    }
}
