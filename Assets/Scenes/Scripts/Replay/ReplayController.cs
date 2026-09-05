using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayController : MonoBehaviour
{
    [SerializeField] public Board Board;
    [SerializeField] ReplayView view;

    private List<ReplayMove> moves;
    private readonly List<ReplayMove> variation = new();
    private int mainIndex;
    private int variationFrom;
    private int varIndex;
    private bool onVariation;
    private int firstPly;
    private int firstFullmove;
    private ReplayMove pendingMove;
    private ReplayAnalysis analysis;

    void Start()
    {
        if (Settings.GameMode != GameMode.Replay)
        {
            view.SetPanelActive(false);
            enabled = false;
            return;
        }

        string text = File.ReadAllText(Settings.ReplayFilePath);
        GameRecord record = IpgnParser.Parse(text);
        if (record.SetUp == "1" && !string.IsNullOrEmpty(record.Ifen))
            Board.LoadPosition(IfenParser.Parse(record.Ifen));
        moves = ReplayExpander.Expand(record);
        IpgnFormatter.GetMovetextOrigin(record, out firstPly, out firstFullmove);
        mainIndex = 0;
        Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        Board.HighlightHint(-Vector2Int.one, -Vector2Int.one);
        view.SetPanelActive(true);
        view.SetMeta(record);
        view.SetEngineVisible(false);
        view.RebuildList(moves, variation, variationFrom, firstPly, firstFullmove);
        view.MoveClicked += OnMoveClicked;
        view.EngineToggled += OnEngineToggled;
        Board.MoveStartEvent += MoveStartHandler;
        Board.MoveEndEvent += MoveEndHandler;
        analysis = new ReplayAnalysis();
        analysis.Updated += OnAnalysisUpdated;
        AfterStep(false);
    }

    void OnDestroy()
    {
        if (view != null)
        {
            view.MoveClicked -= OnMoveClicked;
            view.EngineToggled -= OnEngineToggled;
        }
        if (Board != null)
        {
            Board.MoveStartEvent -= MoveStartHandler;
            Board.MoveEndEvent -= MoveEndHandler;
        }
        if (analysis != null)
        {
            analysis.Updated -= OnAnalysisUpdated;
            analysis.Stop();
        }
    }

    void Update()
    {
        if (Board.wait_for_transformation) return;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Forward();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            Back();
    }

    private void OnMoveClicked(int ply, bool isVariation)
    {
        if (isVariation) JumpToVariation(ply);
        else JumpToMain(ply);
    }

    private void Forward()
    {
        if (Board.wait_for_transformation) return;
        bool moved = onVariation ? ApplyVariationForward() : ApplyMainForward();
        if (moved)
            AfterStep();
    }

    private void Back()
    {
        if (Board.wait_for_transformation) return;
        bool moved = onVariation ? ApplyVariationBack() : ApplyMainBack();
        if (moved)
            AfterStep();
    }

    private void JumpToMain(int target)
    {
        if (Board.wait_for_transformation) return;
        GoToMain(target);
        AfterStep();
    }

    private void JumpToVariation(int target)
    {
        if (Board.wait_for_transformation) return;
        if (variation.Count == 0) return;
        target = Mathf.Clamp(target, 0, variation.Count);
        GoToMain(variationFrom);
        onVariation = target > 0;
        varIndex = 0;
        while (varIndex < target && ApplyVariationForward()) { }
        AfterStep();
    }

    private void GoToMain(int target)
    {
        if (moves == null) return;
        target = Mathf.Clamp(target, 0, moves.Count);
        if (onVariation)
        {
            while (varIndex > 0 && ApplyVariationBack()) { }
            onVariation = false;
            mainIndex = variationFrom;
        }
        while (mainIndex < target && ApplyMainForward()) { }
        while (mainIndex > target && ApplyMainBack()) { }
    }

    private bool ApplyMainForward()
    {
        if (moves == null || mainIndex >= moves.Count) return false;
        ReplayMove move = moves[mainIndex];
        Board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
        mainIndex++;
        return true;
    }

    private bool ApplyMainBack()
    {
        if (moves == null || mainIndex <= 0) return false;
        mainIndex--;
        ReplayMove move = moves[mainIndex];
        Board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        return true;
    }

    private bool ApplyVariationForward()
    {
        if (varIndex >= variation.Count) return false;
        ReplayMove move = variation[varIndex];
        Board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
        varIndex++;
        return true;
    }

    private bool ApplyVariationBack()
    {
        if (varIndex <= 0)
        {
            if (!onVariation) return false;
            onVariation = false;
            mainIndex = variationFrom;
            return true;
        }
        varIndex--;
        ReplayMove move = variation[varIndex];
        Board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        if (varIndex == 0)
        {
            onVariation = false;
            mainIndex = variationFrom;
        }
        return true;
    }

    private void AfterStep(bool restartEngine = true)
    {
        Board.ClearSelection();
        SyncTurn();
        ReplayMove last = CurrentLastMove();
        if (last == null)
            Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        else
            Board.HighlightLastMove(last.From, last.To);
        view.Highlight(mainIndex, varIndex, onVariation);
        if (restartEngine && analysis != null && analysis.Enabled)
        {
            ClearEngineBoard();
            view.ClearEngine();
            analysis.Analyze(Board);
        }
    }

    private ReplayMove CurrentLastMove()
    {
        if (onVariation)
            return varIndex > 0 ? variation[varIndex - 1] : null;
        return mainIndex > 0 ? moves[mainIndex - 1] : null;
    }

    private void SyncTurn()
    {
        int ply = onVariation ? variationFrom + varIndex : mainIndex;
        Board.Turn = (firstPly + ply) % 2 == 1;
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        IPiece moving = Board.pieces[start.x][start.y];
        if (moving == null) return;
        IPiece target = Board.pieces[end.x][end.y];
        bool castling = target != null && target.Team == moving.Team;
        bool capture = target != null && target.Team != moving.Team;
        PieceType? transformation = null;
        if (transform_info != GameRecorder.NoTransformInfo && transform_info != (int)moving.Type)
            transformation = (PieceType)transform_info;
        var recorded = new RecordedMove
        {
            Piece = moving.Type,
            From = start,
            To = end,
            Capture = capture,
            Castling = castling,
            Transformation = transformation
        };
        pendingMove = new ReplayMove
        {
            From = start,
            To = end,
            FromBefore = Board.GetTileState(start),
            ToBefore = Board.GetTileState(end),
            Notation = IpgnFormatter.FormatMove(recorded)
        };
    }

    private void MoveEndHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        if (pendingMove == null) return;
        pendingMove.FromAfter = Board.GetTileState(start);
        pendingMove.ToAfter = Board.GetTileState(end);
        AcceptUserMove(pendingMove);
        pendingMove = null;
    }

    private void AcceptUserMove(ReplayMove move)
    {
        if (!onVariation)
        {
            if (mainIndex < moves.Count && SameMove(move, moves[mainIndex]))
            {
                mainIndex++;
                AfterStep();
                return;
            }
            variationFrom = mainIndex;
            variation.Clear();
            variation.Add(move);
            onVariation = true;
            varIndex = 1;
            view.RebuildList(moves, variation, variationFrom, firstPly, firstFullmove);
            AfterStep();
            return;
        }

        if (varIndex < variation.Count && SameMove(move, variation[varIndex]))
        {
            varIndex++;
            AfterStep();
            return;
        }
        if (varIndex < variation.Count)
            variation.RemoveRange(varIndex, variation.Count - varIndex);
        variation.Add(move);
        varIndex = variation.Count;
        view.RebuildList(moves, variation, variationFrom, firstPly, firstFullmove);
        AfterStep();
    }

    private static bool SameMove(ReplayMove a, ReplayMove b)
    {
        return a.From == b.From && a.To == b.To
            && SameTile(a.FromAfter, b.FromAfter) && SameTile(a.ToAfter, b.ToAfter);
    }

    private static bool SameTile(TileState? a, TileState? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Type == b.Type && a.Team == b.Team;
    }

    private void OnEngineToggled(bool on)
    {
        if (on)
        {
            ClearEngineBoard();
            analysis.SetEnabled(true, Board);
        }
        else
        {
            analysis.SetEnabled(false, Board);
            ClearEngineBoard();
        }
    }

    private void ClearEngineBoard()
    {
        Board.HighlightHint(-Vector2Int.one, -Vector2Int.one);
    }

    private void OnAnalysisUpdated(MoveResult result)
    {
        if (!view.EngineUiVisible)
        {
            view.ClearEngine();
            ClearEngineBoard();
            return;
        }
        if (!result.Move.HasValue && result.Depth == 0 && result.Mark == 0)
        {
            view.ClearEngine();
            ClearEngineBoard();
            return;
        }
        string eval = ReplayAnalysis.FormatMark(result.Mark) + "  глубина " + result.Depth;
        if (result.Move.HasValue)
        {
            view.ShowEngine(eval, ReplayAnalysis.BarRatio(result.Mark), ReplayAnalysis.FormatBestMove(result.Move.Value, Board));
            ReplayAnalysis.HintMove(result.Move.Value, Board);
        }
        else
        {
            view.ShowEngine(eval, ReplayAnalysis.BarRatio(result.Mark), string.Empty);
            ClearEngineBoard();
        }
    }
}
