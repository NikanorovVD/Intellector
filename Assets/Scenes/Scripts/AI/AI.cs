using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class AI : MonoBehaviour
{
    [SerializeField] private Board main_board;
    public const int AI_depth = 8;
    public const int AI_MOVE_DELAY_MS = 0;

    public static bool AI_team = true;
    public static string DisplayName => $"minmax<{AI_depth}>";

    private bool lastMoveWasProgressive;

    private static readonly Dictionary<EngineFigure, int> FigureToUnityType = new Dictionary<EngineFigure, int>
    {
        { EngineFigure.WhiteProgressor, (int)PieceType.progressor },
        { EngineFigure.BlackProgressor, (int)PieceType.progressor },
        { EngineFigure.WhiteDominator, (int)PieceType.dominator },
        { EngineFigure.BlackDominator, (int)PieceType.dominator },
        { EngineFigure.WhiteLiberator, (int)PieceType.liberator },
        { EngineFigure.BlackLiberator, (int)PieceType.liberator },
        { EngineFigure.WhiteAgressor, (int)PieceType.agressor },
        { EngineFigure.BlackAgressor, (int)PieceType.agressor },
        { EngineFigure.WhiteDefensor, (int)PieceType.defensor },
        { EngineFigure.BlackDefensor, (int)PieceType.defensor },
        { EngineFigure.WhiteIntellector, (int)PieceType.intellector },
        { EngineFigure.BlackIntellector, (int)PieceType.intellector }
    };

    private async void Start()
    {
        if (Settings.GameMode != GameMode.AI) return;

        main_board.MoveStartEvent += (start, end, _) =>
        {
            lastMoveWasProgressive = EngineUtils.IsProgressiveMove(
                BoardToEngine.ToEngineFigure(main_board.pieces[start.x][start.y]),
                BoardToEngine.ToEngineFigure(main_board.pieces[end.x][end.y]));
        };
        main_board.MoveEndEvent += (_, _, _) =>
        {
            BoardToEngine.CreateEngine(main_board).RememberPlayed(lastMoveWasProgressive);
        };
        main_board.RestartEvent += Engine.ClearPlayedHistory;

        if (!AI_team) await MakeAIMove();

        main_board.MoveEndEvent += async (_, _, _) =>
        {
            if (main_board.Turn)
            {
                await Task.Delay(AI_MOVE_DELAY_MS);
                await MakeAIMove();
            }
        };
    }

    private async Task MakeAIMove()
    {
        var engine = BoardToEngine.CreateEngine(main_board);
        var result = await Task.Run(() => engine.BestMoveByDepth(AI_depth));
        if (result.Move == null)
        {
            Debug.LogError("Engine вернул null");
            return;
        }

        var move = result.Move.Value;
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        int endType = FigureToUnityType[move.Figure];

        main_board.MovePiece(
            new Vector2Int(fromX, fromY),
            new Vector2Int(toX, toY),
            endType
        );
    }
}
