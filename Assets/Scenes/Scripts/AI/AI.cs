using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField] private Board mainBoard;
    public const int AiMoveDelayMs = 0;

    private bool AiTeam => !mainBoard.PlayerTeam;

    private bool lastMoveWasProgressive;

    private static readonly Dictionary<EngineFigure, int> figureToUnityType = new Dictionary<EngineFigure, int>
    {
        { EngineFigure.WhiteProgressor, (int)PieceType.Progressor },
        { EngineFigure.BlackProgressor, (int)PieceType.Progressor },
        { EngineFigure.WhiteDominator, (int)PieceType.Dominator },
        { EngineFigure.BlackDominator, (int)PieceType.Dominator },
        { EngineFigure.WhiteLiberator, (int)PieceType.Liberator },
        { EngineFigure.BlackLiberator, (int)PieceType.Liberator },
        { EngineFigure.WhiteAgressor, (int)PieceType.Agressor },
        { EngineFigure.BlackAgressor, (int)PieceType.Agressor },
        { EngineFigure.WhiteDefensor, (int)PieceType.Defensor },
        { EngineFigure.BlackDefensor, (int)PieceType.Defensor },
        { EngineFigure.WhiteIntellector, (int)PieceType.Intellector },
        { EngineFigure.BlackIntellector, (int)PieceType.Intellector }
    };

    private async void Start()
    {
        if (Settings.GameMode != GameMode.AI) return;

        mainBoard.MoveStartEvent += (start, end, _) =>
        {
            lastMoveWasProgressive = EngineUtils.IsProgressiveMove(
                BoardToEngine.ToEngineFigure(mainBoard.Pieces[start.x][start.y]),
                BoardToEngine.ToEngineFigure(mainBoard.Pieces[end.x][end.y]));
        };
        mainBoard.MoveEndEvent += (_, _, _) =>
        {
            BoardToEngine.CreateEngine(mainBoard).RememberPlayed(lastMoveWasProgressive);
        };
        mainBoard.RestartEvent += () =>
        {
            Engine.ClearPlayedHistory();
            if (mainBoard.Turn == AiTeam)
                _ = MakeAIMove();
        };

        if (mainBoard.Turn == AiTeam) await MakeAIMove();

        mainBoard.MoveEndEvent += async (_, _, _) =>
        {
            if (mainBoard.GameOver) return;
            if (mainBoard.Turn == AiTeam)
            {
                await Task.Delay(AiMoveDelayMs);
                await MakeAIMove();
            }
        };
    }

    private async Task MakeAIMove()
    {
        if (mainBoard.GameOver) return;
        var engine = BoardToEngine.CreateEngine(mainBoard);
        if (engine.TryGetTerminalResult(out _)) return;
        var result = await Task.Run(() => Search(engine));
        if (result.Move == null)
        {
            Debug.LogError("Engine вернул null");
            return;
        }

        var move = result.Move.Value;
        var (fromX, fromY) = EngineUtils.EngineIndexToUnity(move.From);
        var (toX, toY) = EngineUtils.EngineIndexToUnity(move.To);
        int endType = figureToUnityType[move.Figure];

        mainBoard.MovePiece(
            new Vector2Int(fromX, fromY),
            new Vector2Int(toX, toY),
            endType
        );
    }

    private static MoveResult Search(Engine engine)
    {
        var ai = Settings.AI;
        return ai.Mode switch
        {
            AISearchMode.Time => engine.BestMoveByTime(ai.SearchTimeMs),
            AISearchMode.Level => engine.BestMoveByLevel(ai.Level),
            AISearchMode.Depth => engine.BestMoveByDepth(ai.Depth),
            _ => throw new ArgumentOutOfRangeException(nameof(ai.Mode), ai.Mode, null)
        };
    }
}
