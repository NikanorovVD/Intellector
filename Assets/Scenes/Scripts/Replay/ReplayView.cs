using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class ReplayView : MonoBehaviour
{
    private static readonly Color rowColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    private static readonly Color variationRowColor = new Color(0.28f, 0.18f, 0.32f, 0.9f);
    private static readonly Color excellentColor = new Color(0.11f, 0.30f, 0.16f, 0.9f);
    private static readonly Color inaccuracyColor = new Color(0.33f, 0.30f, 0.10f, 0.9f);
    private static readonly Color mistakeColor = new Color(0.37f, 0.22f, 0.07f, 0.9f);
    private static readonly Color blunderColor = new Color(0.37f, 0.11f, 0.11f, 0.9f);

    private const int VariationIndent = 16;
    private const string CopyIfenDoneLabel = "Скопировано";
    private const string PreAnalyzeCancelLabel = "Отмена";
    public const int DefaultPreAnalyzeDepth = 8;

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Text meta;
    [SerializeField] private Toggle engineToggle;
    [SerializeField] private Text evalText;
    [SerializeField] private Text bestMoveText;
    [SerializeField] private GameObject accuracyRow;
    [SerializeField] private GameObject whiteAccuracyGroup;
    [SerializeField] private GameObject blackAccuracyGroup;
    [SerializeField] private Text whiteAccuracyText;
    [SerializeField] private Text blackAccuracyText;
    [SerializeField] private GameObject evalBar;
    [SerializeField] private RectTransform evalBarFill;
    [SerializeField] private Button copyIfenButton;
    [SerializeField] private Text copyIfenText;
    [SerializeField] private Button preAnalyzeButton;
    [SerializeField] private Text preAnalyzeButtonText;
    [SerializeField] private InputField depthInput;
    [SerializeField] private GameObject preAnalyzeProgress;
    [SerializeField] private RectTransform preAnalyzeProgressFill;
    [SerializeField] private LayoutElement fileNameLayout;
    [SerializeField] private GameObject renameRow;
    [SerializeField] private InputField renameInput;
    [SerializeField] private Button renameCheck;
    [SerializeField] private Text renameError;
    [SerializeField] private Text fileDeleted;

    public event Action<bool> EngineToggled;
    public event Action<int, bool> MoveClicked;
    public event Action CopyIfenClicked;
    public event Action PreAnalyzeClicked;
    public event Action<string> RenameConfirmed;

    private readonly List<ListCell> cells = new();
    private IReadOnlyList<MoveQuality> mainQualities;
    private bool engineUiVisible;
    private bool engineHasAccuracy;
    private Coroutine copyIfenFeedback;
    private string copyIfenIdleLabel;
    private string preAnalyzeIdleLabel;
    private string currentFileName;
    private bool fileMissing;

    public bool EngineUiVisible => engineUiVisible;

    private struct ListCell
    {
        public Image Image;
        public Image Frame;
        public bool Variation;
        public int Ply;
        public Color Idle;
    }

    private void OnEnable()
    {
        if (copyIfenText != null)
            copyIfenIdleLabel = copyIfenText.text;
        if (preAnalyzeButtonText != null)
            preAnalyzeIdleLabel = preAnalyzeButtonText.text;
        if (engineToggle != null)
            engineToggle.onValueChanged.AddListener(OnEngineToggled);
        if (copyIfenButton != null)
            copyIfenButton.onClick.AddListener(OnCopyIfenClicked);
        if (preAnalyzeButton != null)
            preAnalyzeButton.onClick.AddListener(OnPreAnalyzeClicked);
        if (depthInput != null)
        {
            depthInput.onEndEdit.AddListener(OnDepthEndEdit);
            if (string.IsNullOrEmpty(depthInput.text))
                depthInput.text = DefaultPreAnalyzeDepth.ToString(CultureInfo.InvariantCulture);
        }
        if (renameInput != null)
        {
            renameInput.onValueChanged.AddListener(OnRenameEdited);
            renameInput.onSubmit.AddListener(OnRenameSubmitted);
        }
        if (renameCheck != null)
            renameCheck.onClick.AddListener(OnRenameSaveClicked);
    }

    private void OnDisable()
    {
        if (engineToggle != null)
            engineToggle.onValueChanged.RemoveListener(OnEngineToggled);
        if (copyIfenButton != null)
            copyIfenButton.onClick.RemoveListener(OnCopyIfenClicked);
        if (preAnalyzeButton != null)
            preAnalyzeButton.onClick.RemoveListener(OnPreAnalyzeClicked);
        if (depthInput != null)
            depthInput.onEndEdit.RemoveListener(OnDepthEndEdit);
        if (renameInput != null)
        {
            renameInput.onValueChanged.RemoveListener(OnRenameEdited);
            renameInput.onSubmit.RemoveListener(OnRenameSubmitted);
        }
        if (renameCheck != null)
            renameCheck.onClick.RemoveListener(OnRenameSaveClicked);
        if (copyIfenFeedback != null)
        {
            StopCoroutine(copyIfenFeedback);
            copyIfenFeedback = null;
        }
        if (copyIfenText != null)
            copyIfenText.text = copyIfenIdleLabel;
        if (preAnalyzeButtonText != null)
            preAnalyzeButtonText.text = preAnalyzeIdleLabel;
        CloseRename();
    }

    public void SetPanelActive(bool on)
    {
        if (panel != null)
            panel.SetActive(on);
        if (!on)
            CloseRename();
    }

    public bool IsRenaming => renameInput != null && renameInput.isFocused && !fileMissing;

    public void SetFileName(string name)
    {
        fileMissing = false;
        currentFileName = name ?? string.Empty;
        if (fileNameLayout != null && !fileNameLayout.gameObject.activeSelf)
            fileNameLayout.gameObject.SetActive(true);
        if (renameRow != null)
            renameRow.SetActive(true);
        if (fileDeleted != null)
            fileDeleted.gameObject.SetActive(false);
        if (renameInput != null && renameInput.text != currentFileName)
            renameInput.text = currentFileName;
        RefreshRenameCheck();
    }

    public void ClearFileName()
    {
        fileMissing = true;
        currentFileName = string.Empty;
        if (renameInput != null)
            renameInput.DeactivateInputField();
        if (renameRow != null)
            renameRow.SetActive(false);
        if (fileDeleted != null)
            fileDeleted.gameObject.SetActive(true);
        SetRenameError(string.Empty);
        RefreshRenameCheck();
    }

    public void CloseRename()
    {
        if (renameInput != null)
        {
            if (renameInput.text != currentFileName)
                renameInput.text = currentFileName ?? string.Empty;
            renameInput.DeactivateInputField();
        }
        SetRenameError(string.Empty);
        RefreshRenameCheck();
    }

    public void SetRenameError(string error)
    {
        if (renameError == null) return;
        string text = error ?? string.Empty;
        bool show = text.Length > 0;
        if (renameError.text == text && renameError.gameObject.activeSelf == show)
            return;
        renameError.text = text;
        renameError.gameObject.SetActive(show);
        RebuildPanelLayout();
    }

    public void ShowCopyIfenCopied()
    {
        if (copyIfenText == null) return;
        if (copyIfenFeedback != null)
            StopCoroutine(copyIfenFeedback);
        copyIfenFeedback = StartCoroutine(CopyIfenFeedback());
    }

    public void SetMeta(GameRecord record)
    {
        meta.text = FormatMeta(record);
    }

    public void SetMoveQualities(IReadOnlyList<MoveQuality> qualities)
    {
        mainQualities = qualities;
    }

    public void RebuildList(
        IReadOnlyList<ReplayMove> moves,
        IReadOnlyList<ReplayMove> variation,
        int variationFrom,
        int firstPly,
        int firstFullmove)
    {
        while (content.transform.childCount > 0)
            DestroyImmediate(content.transform.GetChild(0).gameObject);
        FillList(moves, variation, variationFrom, firstPly, firstFullmove);
    }

    public void Highlight(int mainIndex, int varIndex, bool onVariation)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            ListCell cell = cells[i];
            bool selected = cell.Variation
                ? onVariation && cell.Ply == varIndex
                : !onVariation && cell.Ply == mainIndex;
            cell.Image.color = cell.Idle;
            if (cell.Frame != null)
                cell.Frame.enabled = selected;
        }
    }

    public void SetEngineVisible(bool on)
    {
        engineUiVisible = on;
        if (evalText != null)
            evalText.gameObject.SetActive(on);
        if (bestMoveText != null)
            bestMoveText.gameObject.SetActive(on);
        if (evalBar != null)
            evalBar.SetActive(on);
        if (!on)
            SetAccuracy(null, null);
        if (!on)
            SetEvalBar(0.5f);
        RebuildPanelLayout();
    }

    public void SetEngineOn(bool on)
    {
        if (engineToggle != null)
        {
            if (engineToggle.isOn != on)
                engineToggle.isOn = on;
            else if (engineUiVisible != on)
                OnEngineToggled(on);
            return;
        }
        if (engineUiVisible != on)
            OnEngineToggled(on);
    }

    public int GetDepth()
    {
        if (depthInput == null || !int.TryParse(depthInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int depth))
            return DefaultPreAnalyzeDepth;
        return Mathf.Max(1, depth);
    }

    public void SetPreAnalyzeRunning(bool running)
    {
        if (preAnalyzeButtonText != null)
            preAnalyzeButtonText.text = running ? PreAnalyzeCancelLabel : preAnalyzeIdleLabel;
        if (depthInput != null)
            depthInput.interactable = !running;
        if (preAnalyzeProgress != null)
            preAnalyzeProgress.SetActive(running);
        if (!running)
            SetPreAnalyzeProgress(0, 1);
        RebuildPanelLayout();
    }

    public void SetPreAnalyzeProgress(int done, int total)
    {
        if (preAnalyzeProgressFill == null) return;
        float ratio = total <= 0 ? 0f : (float)done / total;
        preAnalyzeProgressFill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        preAnalyzeProgressFill.offsetMin = Vector2.zero;
        preAnalyzeProgressFill.offsetMax = Vector2.zero;
    }

    public void ClearEngine()
    {
        if (evalText != null)
            evalText.text = engineUiVisible ? "..." : string.Empty;
        if (bestMoveText != null)
            bestMoveText.text = string.Empty;
        SetAccuracy(null, null);
        SetEvalBar(0.5f);
        RebuildPanelLayout();
    }

    public void ShowEngine(string eval, float barRatio, string bestMove, double? whiteAccuracy = null, double? blackAccuracy = null)
    {
        if (evalText != null)
            evalText.text = eval;
        SetEvalBar(barRatio);
        if (bestMoveText != null)
            bestMoveText.text = bestMove ?? string.Empty;
        SetAccuracy(whiteAccuracy, blackAccuracy);
        RebuildPanelLayout();
    }

    private void SetAccuracy(double? white, double? black)
    {
        engineHasAccuracy = engineUiVisible && (white != null || black != null);
        if (whiteAccuracyText != null)
            whiteAccuracyText.text = white != null ? MoveAccuracy.FormatPercent(white.Value) : string.Empty;
        if (blackAccuracyText != null)
            blackAccuracyText.text = black != null ? MoveAccuracy.FormatPercent(black.Value) : string.Empty;
        if (whiteAccuracyGroup != null)
            whiteAccuracyGroup.SetActive(white != null);
        if (blackAccuracyGroup != null)
            blackAccuracyGroup.SetActive(black != null);
        if (accuracyRow != null)
            accuracyRow.SetActive(engineHasAccuracy);
    }

    private void OnEngineToggled(bool on)
    {
        SetEngineVisible(on);
        ClearEngine();
        EngineToggled?.Invoke(on);
    }

    private void SetEvalBar(float whiteShare)
    {
        if (evalBarFill == null) return;
        evalBarFill.anchorMax = new Vector2(Mathf.Clamp01(whiteShare), 1f);
        evalBarFill.offsetMin = Vector2.zero;
        evalBarFill.offsetMax = Vector2.zero;
    }

    private void FillList(
        IReadOnlyList<ReplayMove> moves,
        IReadOnlyList<ReplayMove> variation,
        int variationFrom,
        int firstPly,
        int firstFullmove)
    {
        cells.Clear();
        bool variationPlaced = variation.Count == 0;
        if (!variationPlaced && variationFrom == 0)
        {
            FillVariation(variation, firstPly, firstFullmove, variationFrom);
            variationPlaced = true;
        }

        int i = 0;
        int number = firstFullmove;
        if (firstPly == 1 && moves.Count > 0)
        {
            AddTurnRow(number, null, 0, false, MainNotation(moves, 0), 1, false, false);
            if (!variationPlaced && variationFrom <= 1)
            {
                FillVariation(variation, firstPly, firstFullmove, variationFrom);
                variationPlaced = true;
            }
            i = 1;
            number++;
        }
        for (; i < moves.Count; i += 2)
        {
            string black = i + 1 < moves.Count ? MainNotation(moves, i + 1) : null;
            int lastPly = black != null ? i + 2 : i + 1;
            AddTurnRow(number, MainNotation(moves, i), i + 1, false, black, black != null ? i + 2 : 0, false, false);
            if (!variationPlaced && variationFrom <= lastPly)
            {
                FillVariation(variation, firstPly, firstFullmove, variationFrom);
                variationPlaced = true;
            }
            number++;
        }
        if (!variationPlaced)
            FillVariation(variation, firstPly, firstFullmove, variationFrom);
    }

    private void FillVariation(
        IReadOnlyList<ReplayMove> variation,
        int firstPly,
        int firstFullmove,
        int variationFrom)
    {
        int originPly = firstPly + variationFrom;
        int i = 0;
        int number = firstFullmove + originPly / 2;
        if (originPly % 2 == 1 && variation.Count > 0)
        {
            AddTurnRow(number, null, 0, true, variation[0].Notation, 1, true, true);
            i = 1;
            number++;
        }
        for (; i < variation.Count; i += 2)
        {
            string black = i + 1 < variation.Count ? variation[i + 1].Notation : null;
            AddTurnRow(number, variation[i].Notation, i + 1, true, black, black != null ? i + 2 : 0, true, true);
            number++;
        }
    }

    private void AddTurnRow(int number, string white, int whitePly, bool whiteVar, string black, int blackPly, bool blackVar, bool indent)
    {
        GameObject item = SpawnRow(indent);
        item.transform.Find("Number").GetComponent<Text>().text = number + ".";
        Transform whiteCell = item.transform.Find("White");
        if (white == null)
        {
            whiteCell.GetComponent<Button>().interactable = false;
            whiteCell.GetComponentInChildren<Text>().text = "";
        }
        else
            BindCell(whiteCell, white, whiteVar, whitePly);
        Transform blackCell = item.transform.Find("Black");
        if (black == null)
        {
            blackCell.GetComponent<Button>().interactable = false;
            blackCell.GetComponentInChildren<Text>().text = "";
        }
        else
            BindCell(blackCell, black, blackVar, blackPly);
    }

    private Color IdleColorFor(bool isVariation, int ply)
    {
        if (isVariation)
            return variationRowColor;
        if (mainQualities == null || ply <= 0)
            return rowColor;
        int index = ply - 1;
        if (index >= mainQualities.Count)
            return rowColor;
        return ColorFor(mainQualities[index].Judgement);
    }

    private static Color ColorFor(MoveJudgement judgement)
    {
        return judgement switch
        {
            MoveJudgement.Inaccuracy => inaccuracyColor,
            MoveJudgement.Mistake => mistakeColor,
            MoveJudgement.Blunder => blunderColor,
            _ => excellentColor
        };
    }

    private string MainNotation(IReadOnlyList<ReplayMove> moves, int index)
    {
        string text = moves[index].Notation;
        if (mainQualities == null || index < 0 || index >= mainQualities.Count)
            return text;
        string glyph = MoveAccuracy.Glyph(mainQualities[index].Judgement);
        return glyph.Length == 0 ? text : text + glyph;
    }

    private GameObject SpawnRow(bool indent)
    {
        GameObject item = Instantiate(itemPrefab);
        item.transform.SetParent(content.transform, false);
        item.SetActive(true);
        item.transform.localScale = Vector3.one;
        if (indent)
        {
            var row = item.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(VariationIndent, 0, 0, 0);
        }
        return item;
    }

    private void OnCopyIfenClicked()
    {
        CopyIfenClicked?.Invoke();
    }

    private void OnPreAnalyzeClicked()
    {
        PreAnalyzeClicked?.Invoke();
    }

    private void OnRenameEdited(string _)
    {
        SetRenameError(string.Empty);
        RefreshRenameCheck();
    }

    private void OnRenameSubmitted(string _)
    {
        OnRenameSaveClicked();
    }

    private void OnRenameSaveClicked()
    {
        if (fileMissing || renameInput == null) return;
        string typed = (renameInput.text ?? string.Empty).Trim();
        if (string.Equals(typed, currentFileName ?? string.Empty, StringComparison.Ordinal))
            return;
        RenameConfirmed?.Invoke(renameInput.text);
    }

    private void OnDepthEndEdit(string _)
    {
        if (depthInput == null) return;
        depthInput.text = GetDepth().ToString(CultureInfo.InvariantCulture);
    }

    private IEnumerator CopyIfenFeedback()
    {
        copyIfenText.text = CopyIfenDoneLabel;
        yield return new WaitForSeconds(1.2f);
        copyIfenText.text = copyIfenIdleLabel;
        copyIfenFeedback = null;
    }

    private void BindCell(Transform cell, string text, bool isVariation, int ply)
    {
        cell.GetComponentInChildren<Text>().text = text;
        cell.GetComponent<Button>().onClick.AddListener(() => MoveClicked?.Invoke(ply, isVariation));
        Color idle = IdleColorFor(isVariation, ply);
        var image = cell.GetComponent<Image>();
        image.color = idle;
        Image frame = cell.Find("Frame").GetComponent<Image>();
        frame.enabled = false;
        cells.Add(new ListCell
        {
            Image = image,
            Frame = frame,
            Variation = isVariation,
            Ply = ply,
            Idle = idle
        });
    }

    private static string FormatMeta(GameRecord record)
    {
        var builder = new StringBuilder();
        builder.Append("<size=18>");
        builder.Append(Headline(record));
        builder.Append("</size>");

        string when = FormatWhen(record.Date, record.UTCTime);
        if (when != null)
        {
            builder.Append('\n');
            builder.Append(when);
        }

        string details = FormatDetails(record);
        if (details != null)
        {
            builder.Append('\n');
            builder.Append(details);
        }

        string termination = FormatTermination(record.Termination);
        if (termination != null)
        {
            builder.Append('\n');
            builder.Append(termination);
        }

        return builder.ToString();
    }

    private static string Headline(GameRecord record)
    {
        string white = DisplayPlayerName(record.White);
        string black = DisplayPlayerName(record.Black);
        string names;
        if (!string.IsNullOrEmpty(white) && !string.IsNullOrEmpty(black))
            names = white + " — " + black;
        else if (!string.IsNullOrEmpty(white))
            names = white;
        else
            names = black ?? string.Empty;

        string result = record.Result;
        if (result == GameRecord.UnfinishedResult)
            result = null;

        if (string.IsNullOrEmpty(names))
            return result ?? string.Empty;
        if (string.IsNullOrEmpty(result))
            return names;
        return names + "\n" + result;
    }

    private static string DisplayPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return AllowWrap(name);
    }

    private static string AllowWrap(string value)
    {
        if (value.Length < 2)
            return value;
        var builder = new StringBuilder(value.Length * 2 - 1);
        builder.Append(value[0]);
        for (int i = 1; i < value.Length; i++)
        {
            builder.Append('\u200B');
            builder.Append(value[i]);
        }
        return builder.ToString();
    }

    private static string FormatWhen(string date, string time)
    {
        string day = date;
        if (DateTime.TryParseExact(date, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            day = parsed.ToString("dd.MM.yyyy");
        string clock = time;
        if (!string.IsNullOrEmpty(time) && time.Length >= 5)
            clock = time.Substring(0, 5);
        if (string.IsNullOrEmpty(day))
            return string.IsNullOrEmpty(clock) ? null : clock;
        if (string.IsNullOrEmpty(clock))
            return day;
        return day + ", " + clock;
    }

    private static string FormatDetails(GameRecord record)
    {
        var parts = new List<string>();
        string mode = FormatGameMode(record.GameMode);
        if (mode != null)
            parts.Add(mode);
        if (!string.IsNullOrEmpty(record.Event) && record.Event != record.GameMode)
            parts.Add(record.Event);
        string clock = FormatTimeControl(record.TimeControl);
        if (clock != null)
            parts.Add(clock);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string FormatGameMode(string value)
    {
        if (!Enum.TryParse(value, out GameMode mode))
            return string.IsNullOrEmpty(value) ? null : value;
        return mode switch
        {
            GameMode.Local => "Локальная игра",
            GameMode.Network => "Сетевая игра",
            GameMode.AI => "Игра против ИИ",
            _ => null
        };
    }

    private static string FormatTimeControl(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "-" || value == "Unlimit")
            return null;
        return value;
    }

    private static string FormatTermination(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        if (Enum.TryParse(value, out EndGameReason reason))
            return IpgnFormatter.FormatTermination(reason);
        return value;
    }

    private void RefreshRenameCheck()
    {
        if (renameCheck == null || renameInput == null) return;
        string typed = (renameInput.text ?? string.Empty).Trim();
        bool changed = !fileMissing && !string.Equals(typed, currentFileName ?? string.Empty, StringComparison.Ordinal);
        if (renameCheck.gameObject.activeSelf == changed)
            return;
        renameCheck.gameObject.SetActive(changed);
        RebuildPanelLayout();
    }

    private void RebuildPanelLayout()
    {
        if (panel == null || !panel.activeInHierarchy) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
    }
}
