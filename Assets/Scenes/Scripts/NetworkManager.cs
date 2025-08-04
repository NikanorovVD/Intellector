using UnityEngine;
using System;
using System.Threading.Tasks;


public class NetworkManager : MonoBehaviour
{
    [SerializeField] private Board board;

    static bool ready_for_rematch = false;

    public event Action ExitEvent;
    public event Action RematchEvent;
    public event Action GameStartEvent;

    public delegate void TimeReceived(int time);
    public event TimeReceived TimeEvent;
    private int gameId;

    private SignalrConnection connection;

    void Start()
    {
        if (Settings.GameMode == GameMode.Network)
        {
            gameId = GameSettings.Load().GameId.Value;
            connection = ConnectionManager.SignalrConnection;

            connection.OnMoveReceived(OnMoveReceived);
            board.MoveEvent += MoveEventHandler;

            GameStartEvent?.Invoke();
        }
    }

    async void MoveEventHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        PieceType? transform = transform_info == 200 ? null : (PieceType)transform_info;
        await connection.SendMove(gameId, new Move(start.x, start.y, end.x, end.y, transform));
        Debug.Log($"Îòïðàâêà õîäà: {start} ; {end} ; {transform_info}");
    }

    public async void AskRematch()
    {
        //ÇÀ×ÅÌ ÒÀÊ ÑËÎÆÍÎ ÒÎ?????
        if (board.NetworkGame)
        {
            //ServerManager.GetInstance().SendRematch();
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
        //ServerManager.GetInstance().SendExit();
    }

    public void OnMoveReceived(Vector2Int start, Vector2Int end, int transform_info)
    {
        Debug.Log($"Ïîëó÷åí õîä: {start} -> {end} : {transform_info} ");
        MainTasks.AddTask(() => board.MovePiece(start, end, true, transform_info));
    }

    public void OnMoveReceived(MoveResponse response)
    {
        if (
            (board.PlayerTeam == false && response.PlayerColor == PlayerColor.Black) ||
            (board.PlayerTeam == true && response.PlayerColor == PlayerColor.White)
            )
        {
            Move move = response.Move;
            int transform = move.Transformation == null ? 200 : (int)move.Transformation;
            OnMoveReceived(new Vector2Int(move.StartX, move.StartY), new Vector2Int(move.EndX, move.EndY), transform);
        }
    }

    public void OnTimeReceived(int time)
    {
        MainTasks.AddTask(() => TimeEvent?.Invoke(time));
    }

    public void OnExitReceived()
    {
        MainTasks.AddTask(() =>
        {
            board.GameOver(board.PlayerTeam, true);
            ExitEvent?.Invoke();
        });
    }

    public void OnRematchReceived()
    {
        ready_for_rematch = true;
        MainTasks.AddTask(() => RematchEvent?.Invoke());
        ;
    }

    public void OnTimeOutReceived(bool exit_team)
    {
        MainTasks.AddTask(() => board.GameOver(exit_team));
    }
}
