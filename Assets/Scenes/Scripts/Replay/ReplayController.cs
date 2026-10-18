using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class ReplayController : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private PromotionHint promotionHint;
    [SerializeField] private ReplayView view;

    private HistoryMenu history;

    private List<ReplayMove> moves;
    private readonly List<ReplayMove> variation = new();
    private int mainIndex;
    private int variationFrom;
    private int varIndex;
    private bool onVariation;
    private int firstPly;
    private int firstFullmove;
    private int firstHalfmove;
    private ReplayMove pendingMove;
    private ReplayAnalysis analysis;
    private GameRecord record;

    private void Start()
    {
        if (Settings.GameMode != GameMode.Replay)
        {
            view.SetPanelActive(false);
            enabled = false;
            return;
        }

        history = HistoryMenu.FindInScene();
        if (history != null)
        {
            history.OpenReplayRenamed += OnHistoryRenamed;
            history.OpenReplayRemoved += OnHistoryRemoved;
        }

        view.MoveClicked += OnMoveClicked;
        view.EngineToggled += OnEngineToggled;
        view.CopyIfenClicked += OnCopyIfenClicked;
        view.PreAnalyzeClicked += OnPreAnalyzeClicked;
        view.SaveAnalysisClicked += OnSaveAnalysisClicked;
        view.SaveVariationClicked += OnSaveVariationClicked;
        view.ClearAnalysisClicked += OnClearAnalysisClicked;
        view.RenameConfirmed += OnRenameConfirmed;
        board.MoveStartEvent += MoveStartHandler;
        board.MoveEndEvent += MoveEndHandler;
        if (HasReplayFile())
            ShowCurrent();
        else
            view.SetPanelActive(false);
        if (history != null)
            history.Open();
    }

    private static bool HasReplayFile()
    {
        return !string.IsNullOrEmpty(Settings.ReplayFilePath) && File.Exists(Settings.ReplayFilePath);
    }

    public void ShowCurrent()
    {
        DetachAnalysis();
        variation.Clear();
        onVariation = false;
        varIndex = 0;
        variationFrom = 0;
        pendingMove = null;
        firstHalfmove = 0;

        string text = File.ReadAllText(Settings.ReplayFilePath);
        record = IpgnParser.Parse(text);
        if (record.SetUp == "1" && !string.IsNullOrEmpty(record.Ifen))
        {
            RecordedPosition start = IfenParser.Parse(record.Ifen);
            board.LoadPosition(start);
            firstHalfmove = start.HalfmoveClock;
        }
        else
            board.LoadPosition(IfenFormatter.Initial());
        board.IsGameOver = false;
        moves = ReplayExpander.Expand(record);
        IpgnFormatter.GetMovetextOrigin(record, out firstPly, out firstFullmove);
        mainIndex = 0;
        board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        board.HighlightHint(-Vector2Int.one, -Vector2Int.one);
        if (promotionHint != null)
            promotionHint.Clear();
        view.SetPanelActive(true);
        view.SetMeta(record);
        view.SetFileName(Path.GetFileNameWithoutExtension(Settings.ReplayFilePath));
        view.SetEngineVisible(false);
        view.SetPreAnalyzeRunning(false);
        analysis = new ReplayAnalysis();
        analysis.Updated += OnAnalysisUpdated;
        analysis.MoveJudged += OnMoveJudged;
        analysis.VariationJudged += OnVariationJudged;
        analysis.PreProgress += OnPreProgress;
        analysis.PreFinished += OnPreFinished;
        analysis.SaveableChanged += OnSaveableChanged;
        if (record.Analysis != null)
            analysis.Restore(record.Analysis, firstPly);
        UpdateAnalysisButtons();
        RefreshMoveQualities();
        AfterStep(false);
        view.SyncEngineToggle();
    }

    private void DetachAnalysis()
    {
        if (analysis == null)
            return;
        analysis.Updated -= OnAnalysisUpdated;
        analysis.MoveJudged -= OnMoveJudged;
        analysis.VariationJudged -= OnVariationJudged;
        analysis.PreProgress -= OnPreProgress;
        analysis.PreFinished -= OnPreFinished;
        analysis.SaveableChanged -= OnSaveableChanged;
        analysis.Stop();
        analysis = null;
    }

    private void OnDestroy()
    {
        if (history != null)
        {
            history.OpenReplayRenamed -= OnHistoryRenamed;
            history.OpenReplayRemoved -= OnHistoryRemoved;
        }
        if (view != null)
        {
            view.MoveClicked -= OnMoveClicked;
            view.EngineToggled -= OnEngineToggled;
            view.CopyIfenClicked -= OnCopyIfenClicked;
            view.PreAnalyzeClicked -= OnPreAnalyzeClicked;
            view.SaveAnalysisClicked -= OnSaveAnalysisClicked;
            view.SaveVariationClicked -= OnSaveVariationClicked;
            view.ClearAnalysisClicked -= OnClearAnalysisClicked;
            view.RenameConfirmed -= OnRenameConfirmed;
        }
        if (board != null)
        {
            board.MoveStartEvent -= MoveStartHandler;
            board.MoveEndEvent -= MoveEndHandler;
        }
        DetachAnalysis();
    }

    private void Update()
    {
        if (board.IsWaitingForTransformation) return;
        if (history != null && history.IsOpen)
            return;
        if (view != null && view.IsRenaming)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                view.CloseRename();
            return;
        }
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
        if (board.IsWaitingForTransformation) return;
        bool moved = onVariation ? ApplyVariationForward() : ApplyMainForward();
        if (moved)
            AfterStep();
    }

    private void Back()
    {
        if (board.IsWaitingForTransformation) return;
        bool moved = onVariation ? ApplyVariationBack() : ApplyMainBack();
        if (moved)
            AfterStep();
    }

    private void JumpToMain(int target)
    {
        if (board.IsWaitingForTransformation) return;
        GoToMain(target);
        AfterStep();
    }

    private void JumpToVariation(int target)
    {
        if (board.IsWaitingForTransformation) return;
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
        board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
        mainIndex++;
        return true;
    }

    private bool ApplyMainBack()
    {
        if (moves == null || mainIndex <= 0) return false;
        mainIndex--;
        ReplayMove move = moves[mainIndex];
        board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        return true;
    }

    private bool ApplyVariationForward()
    {
        if (varIndex >= variation.Count) return false;
        ReplayMove move = variation[varIndex];
        board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
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
        board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        if (varIndex == 0)
        {
            onVariation = false;
            mainIndex = variationFrom;
        }
        return true;
    }

    private void AfterStep(bool restartEngine = true)
    {
        board.ClearSelection();
        SyncTurn();
        ReplayMove last = CurrentLastMove();
        if (last == null)
            board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        else
            board.HighlightLastMove(last.From, last.To);
        view.Highlight(mainIndex, varIndex, onVariation);
        view.SetSaveVariationEnabled(variation.Count > 0 && HasReplayFile());
        if (restartEngine)
            RefreshEngine();
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
        board.Turn = (firstPly + ply) % 2 == 1;
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        IPiece moving = board.Pieces[start.x][start.y];
        if (moving == null) return;
        IPiece target = board.Pieces[end.x][end.y];
        bool castling = target != null && target.Team == moving.Team;
        bool capture = target != null && target.Team != moving.Team;
        PieceType? transformation = null;
        if (transformInfo != GameRecorder.NoTransformInfo && transformInfo != (int)moving.Type)
            transformation = (PieceType)transformInfo;
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
            FromBefore = board.GetTileState(start),
            ToBefore = board.GetTileState(end),
            Notation = IpgnFormatter.FormatMove(recorded)
        };
    }

    private void MoveEndHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        if (pendingMove == null) return;
        pendingMove.FromAfter = board.GetTileState(start);
        pendingMove.ToAfter = board.GetTileState(end);
        AcceptUserMove(pendingMove);
        pendingMove = null;
    }

    private void AcceptUserMove(ReplayMove move)
    {
        if (moves == null) return;
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
            analysis.ResetVariation(variationFrom);
            view.ClearVariationQualities(0);
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
        analysis.TrimVariation(varIndex);
        view.ClearVariationQualities(varIndex);
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

    private void OnCopyIfenClicked()
    {
        RecordedPosition position = board.ToRecordedPosition(CurrentHalfmoveClock(), CurrentFullmoveNumber());
        GUIUtility.systemCopyBuffer = IfenFormatter.Format(position);
        view.ShowCopyIfenCopied();
    }

    private void OnRenameConfirmed(string name)
    {
        if (!ReplayFile.TryRename(Settings.ReplayFilePath, name, out string dest, out string error))
        {
            view.SetRenameError(error);
            return;
        }
        Settings.ReplayFilePath = dest;
        view.SetFileName(Path.GetFileNameWithoutExtension(dest));
        view.CloseRename();
        if (history != null)
            history.RefreshListed();
    }

    private void OnHistoryRenamed(string path)
    {
        view.SetFileName(Path.GetFileNameWithoutExtension(path));
    }

    private void OnHistoryRemoved()
    {
        view.ClearFileName();
        view.SetSaveAnalysisEnabled(false);
        view.SetSaveVariationEnabled(false);
        view.SetClearAnalysisEnabled(false);
    }

    private int CurrentFullmoveNumber()
    {
        int ply = onVariation ? variationFrom + varIndex : mainIndex;
        return firstFullmove + (firstPly + ply) / 2;
    }

    private int CurrentHalfmoveClock()
    {
        int clock = firstHalfmove;
        int mainApplied = onVariation ? variationFrom : mainIndex;
        for (int i = 0; i < mainApplied; i++)
            clock = AdvanceHalfmove(clock, moves[i]);
        if (onVariation)
        {
            for (int i = 0; i < varIndex; i++)
                clock = AdvanceHalfmove(clock, variation[i]);
        }
        return clock;
    }

    private static int AdvanceHalfmove(int clock, ReplayMove move)
    {
        return ResetsHalfmove(move) ? 0 : clock + 1;
    }

    private static bool ResetsHalfmove(ReplayMove move)
    {
        if (move.FromBefore != null && move.FromBefore.Type == PieceType.Progressor)
            return true;
        return move.FromBefore != null && move.ToBefore != null && move.FromBefore.Team != move.ToBefore.Team;
    }

    private void OnEngineToggled(bool on)
    {
        analysis.SetEnabled(on);
        RefreshEngine();
    }

    private void OnPreAnalyzeClicked()
    {
        if (analysis.IsPreAnalyzing)
        {
            analysis.CancelPreAnalyze();
            return;
        }

        int depth = view.GetDepth();
        int total = (moves?.Count ?? 0) + 1;
        view.SetMoveQualities(null);
        view.RebuildList(moves, variation, variationFrom, firstPly, firstFullmove);
        view.Highlight(mainIndex, varIndex, onVariation);
        view.SetPreAnalyzeRunning(true);
        view.SetPreAnalyzeProgress(0, total);
        analysis.StartPreAnalyze(moves, ReplayExpander.CreateStartBoard(record), firstPly, depth);
        UpdateAnalysisButtons();
        RefreshEngine();
    }

    private void OnPreProgress(int done, int total)
    {
        view.SetPreAnalyzeProgress(done, total);
        RefreshEngine();
    }

    private void OnPreFinished()
    {
        view.SetPreAnalyzeRunning(false);
        UpdateAnalysisButtons();
        RefreshMoveQualities();
        RefreshEngine();
    }

    private void OnSaveAnalysisClicked()
    {
        if (analysis == null || record == null || !HasReplayFile())
            return;
        if (!analysis.TryExport(ReplayExpander.CreateStartBoard(record), moves, out List<PositionAnalysis> positions))
            return;
        record.Analysis = positions;
        File.WriteAllText(Settings.ReplayFilePath, IpgnFormatter.Format(record));
        view.ShowAnalysisSaved();
        UpdateAnalysisButtons();
    }

    private void OnSaveVariationClicked()
    {
        if (record == null || variation.Count == 0 || !HasReplayFile())
            return;
        if (!TryBuildAnalysisRecord(out GameRecord saved))
            return;

        string path = GameRecorder.CreateAnalysisFilePath();
        File.WriteAllText(path, IpgnFormatter.Format(saved));
        if (history != null)
            history.RefreshListed();
        view.ShowVariationSaved();
    }

    private bool TryBuildAnalysisRecord(out GameRecord saved)
    {
        var line = new List<RecordedMove>();
        int prefix = variationFrom;
        if (prefix > record.Moves.Count)
            prefix = record.Moves.Count;
        for (int i = 0; i < prefix; i++)
            line.Add(record.Moves[i]);
        for (int i = 0; i < variation.Count; i++)
        {
            if (!TryToRecordedMove(variation[i], out RecordedMove move))
            {
                saved = null;
                return false;
            }
            line.Add(move);
        }

        DateTime utcNow = DateTime.UtcNow;
        saved = new GameRecord
        {
            Event = GameRecorder.AnalysisEvent,
            Site = record.Site,
            Date = utcNow.ToString("yyyy.MM.dd"),
            UTCTime = utcNow.ToString("HH:mm:ss"),
            White = record.White,
            Black = record.Black,
            Result = GameRecord.UnfinishedResult,
            TimeControl = record.TimeControl,
            GameMode = record.GameMode,
            AppVersion = record.AppVersion,
            SetUp = record.SetUp,
            Ifen = record.Ifen,
            Moves = line
        };
        return true;
    }

    private static bool TryToRecordedMove(ReplayMove move, out RecordedMove recorded)
    {
        recorded = null;
        if (move?.FromBefore == null)
            return false;

        bool castling = move.ToBefore != null && move.ToBefore.Team == move.FromBefore.Team;
        PieceType? transformation = null;
        if (!castling && move.ToAfter != null && move.ToAfter.Type != move.FromBefore.Type)
            transformation = move.ToAfter.Type;
        recorded = new RecordedMove
        {
            Piece = move.FromBefore.Type,
            From = move.From,
            To = move.To,
            Capture = move.ToBefore != null && !castling,
            Castling = castling,
            Transformation = transformation
        };
        return true;
    }

    private void OnClearAnalysisClicked()
    {
        if (analysis == null || record == null)
            return;
        if (record.Analysis == null && !analysis.CanSave)
            return;

        bool removeFromFile = record.Analysis != null && HasReplayFile();
        bool wasRunning = analysis.IsPreAnalyzing;
        analysis.Discard();
        record.Analysis = null;
        if (removeFromFile)
            File.WriteAllText(Settings.ReplayFilePath, IpgnFormatter.Format(record));
        if (wasRunning)
            view.SetPreAnalyzeRunning(false);
        RefreshMoveQualities();
        UpdateAnalysisButtons();
        if (analysis.Enabled)
            RefreshEngine();
        else
        {
            view.HideEngine();
            ClearEngineBoard();
        }
        view.ShowAnalysisCleared();
    }

    private void OnSaveableChanged()
    {
        UpdateAnalysisButtons();
    }

    private void UpdateAnalysisButtons()
    {
        view.SetSaveAnalysisEnabled(analysis != null && analysis.CanSave);
        view.SetClearAnalysisEnabled(analysis != null && (analysis.CanSave || record != null && record.Analysis != null));
    }

    private void RefreshMoveQualities()
    {
        if (analysis != null && analysis.TryGetMoveQuality(out GameMoveQuality quality))
            view.SetMoveQualities(quality.Moves);
        else
            view.SetMoveQualities(null);
        view.RebuildList(moves, variation, variationFrom, firstPly, firstFullmove);
        view.Highlight(mainIndex, varIndex, onVariation);
    }

    private void RefreshEngine()
    {
        if (analysis == null)
            return;

        MoveResult cached = default;
        bool hasCache = !onVariation && analysis.TryGetMain(mainIndex, out cached);
        if (hasCache)
            ShowResult(cached);
        else if (!analysis.Enabled || analysis.IsPreAnalyzing)
            ShowAccuracyOnly();

        if (analysis.IsPreAnalyzing || !analysis.Enabled)
        {
            analysis.StopLive();
            if (!hasCache)
                ClearEngineBoard();
            return;
        }

        if (!hasCache)
        {
            ClearEngineBoard();
            view.ClearEngine();
        }

        if (onVariation)
        {
            analysis.AnalyzeVariation(
                board,
                varIndex,
                variationFrom,
                firstPly,
                varIndex > 0 ? VariationPositionBoard(varIndex - 1) : null);
            return;
        }

        analysis.Analyze(
            board,
            mainIndex,
            (moves?.Count ?? 0) + 1,
            firstPly,
            mainIndex > 0 ? PositionBoard(mainIndex - 1) : null);
    }

    private NotationBoard VariationPositionBoard(int varPos)
    {
        NotationBoard notation = PositionBoard(variationFrom);
        for (int i = 0; i < varPos && i < variation.Count; i++)
            notation.Apply(variation[i]);
        return notation;
    }

    private void ShowAccuracyOnly()
    {
        double? whiteAccuracy = null;
        double? blackAccuracy = null;
        if (analysis.TryGetMoveQuality(out GameMoveQuality quality))
        {
            whiteAccuracy = quality.WhiteAccuracy;
            blackAccuracy = quality.BlackAccuracy;
        }
        view.ShowAccuracy(whiteAccuracy, blackAccuracy);
    }

    private void ShowResult(MoveResult result)
    {
        string eval = ReplayAnalysis.FormatMark(result.Mark) + "  глубина " + result.Depth;
        double? whiteAccuracy = null;
        double? blackAccuracy = null;
        if (analysis.TryGetMoveQuality(out GameMoveQuality quality))
        {
            whiteAccuracy = quality.WhiteAccuracy;
            blackAccuracy = quality.BlackAccuracy;
        }
        if (result.Move.HasValue)
        {
            view.ShowEngine(eval, ReplayAnalysis.BarRatio(result.Mark), ReplayAnalysis.FormatBestMove(result.Move.Value, board), whiteAccuracy, blackAccuracy);
            ReplayAnalysis.HintMove(result.Move.Value, board, promotionHint);
        }
        else
        {
            view.ShowEngine(eval, ReplayAnalysis.BarRatio(result.Mark), string.Empty, whiteAccuracy, blackAccuracy);
            ClearEngineBoard();
        }
    }

    private NotationBoard PositionBoard(int ply)
    {
        NotationBoard notation = ReplayExpander.CreateStartBoard(record);
        if (moves == null)
            return notation;
        for (int i = 0; i < ply && i < moves.Count; i++)
            notation.Apply(moves[i]);
        return notation;
    }

    private void OnMoveJudged(int moveIndex, MoveQuality quality)
    {
        if (view == null)
            return;
        view.ApplyMoveQuality(moveIndex, quality);
        if (analysis != null && analysis.TryGetMoveQuality(out GameMoveQuality game))
            view.SetPlayerAccuracy(game.WhiteAccuracy, game.BlackAccuracy);
    }

    private void OnVariationJudged(int moveIndex, MoveQuality quality)
    {
        if (view == null)
            return;
        view.ApplyVariationQuality(moveIndex, quality);
    }

    private void ClearEngineBoard()
    {
        board.HighlightHint(-Vector2Int.one, -Vector2Int.one);
        if (promotionHint != null)
            promotionHint.Clear();
    }

    private void OnAnalysisUpdated(MoveResult result)
    {
        if (analysis == null || !analysis.Enabled)
            return;
        if (!onVariation && analysis.TryGetMain(mainIndex, out MoveResult cached) && result.Depth <= cached.Depth)
            return;
        if (!onVariation)
            analysis.TryDeepenMain(mainIndex, result);
        if (!result.Move.HasValue && result.Depth == 0 && result.Mark == 0)
        {
            if (!onVariation && analysis.TryGetMain(mainIndex, out _))
                return;
            view.ClearEngine();
            ClearEngineBoard();
            return;
        }
        ShowResult(result);
    }
}
