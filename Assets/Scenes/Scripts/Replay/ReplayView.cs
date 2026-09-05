using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ReplayView : MonoBehaviour
{
    static readonly Color RowColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    static readonly Color VariationRowColor = new Color(0.28f, 0.18f, 0.32f, 0.9f);
    static readonly Color SelectedRowColor = new Color(0.45f, 0.4f, 0.15f, 1f);

    private const float NumberWidth = 22f;
    private const float CellWidth = 110f;
    private const float RowSpacing = 4f;
    private const float RowWidth = NumberWidth + CellWidth * 2 + RowSpacing * 2;
    private const int VariationIndent = 16;
    private const float EngineUiOffHeight = 28f;
    private const float EngineUiOnHeight = 90f;

    [SerializeField] GameObject panel;
    [SerializeField] GameObject content;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Text meta;
    [SerializeField] Toggle engineToggle;
    [SerializeField] Text evalText;
    [SerializeField] Text bestMoveText;
    [SerializeField] LayoutElement engineLayout;
    [SerializeField] GameObject evalBar;
    [SerializeField] RectTransform evalBarFill;

    public event Action<bool> EngineToggled;
    public event Action<int, bool> MoveClicked;

    private readonly List<ListCell> cells = new();
    private bool engineUiVisible;

    public bool EngineUiVisible => engineUiVisible;

    private struct ListCell
    {
        public Image Image;
        public bool Variation;
        public int Ply;
    }

    void OnEnable()
    {
        if (engineToggle != null)
            engineToggle.onValueChanged.AddListener(OnEngineToggled);
    }

    void OnDisable()
    {
        if (engineToggle != null)
            engineToggle.onValueChanged.RemoveListener(OnEngineToggled);
    }

    public void SetPanelActive(bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }

    public void SetMeta(GameRecord record)
    {
        meta.text = FormatMeta(record);
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
        FitPanelWidth();
    }

    public void Highlight(int mainIndex, int varIndex, bool onVariation)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            ListCell cell = cells[i];
            bool selected = cell.Variation
                ? onVariation && cell.Ply == varIndex
                : !onVariation && cell.Ply == mainIndex;
            Color idle = cell.Variation ? VariationRowColor : RowColor;
            cell.Image.color = selected ? SelectedRowColor : idle;
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
        if (engineLayout != null)
        {
            engineLayout.minHeight = on ? EngineUiOnHeight : EngineUiOffHeight;
            engineLayout.preferredHeight = on ? EngineUiOnHeight : EngineUiOffHeight;
        }
        if (!on)
            SetEvalBar(0.5f);
    }

    public void ClearEngine()
    {
        if (evalText != null)
            evalText.text = engineUiVisible ? "..." : string.Empty;
        if (bestMoveText != null)
            bestMoveText.text = string.Empty;
        SetEvalBar(0.5f);
    }

    public void ShowEngine(string eval, float barRatio, string bestMove)
    {
        if (evalText != null)
            evalText.text = eval;
        SetEvalBar(barRatio);
        if (bestMoveText != null)
            bestMoveText.text = bestMove ?? string.Empty;
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
            AddTurnRow(number, null, 0, false, moves[0].Notation, 1, false, false);
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
            string black = i + 1 < moves.Count ? moves[i + 1].Notation : null;
            int lastPly = black != null ? i + 2 : i + 1;
            AddTurnRow(number, moves[i].Notation, i + 1, false, black, black != null ? i + 2 : 0, false, false);
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

    private GameObject SpawnRow(bool indent)
    {
        GameObject item = Instantiate(itemPrefab);
        item.transform.SetParent(content.transform, false);
        item.SetActive(true);
        item.transform.localScale = Vector3.one;

        var hlg = item.GetComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childControlWidth = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = RowSpacing;
        hlg.padding = new RectOffset(indent ? VariationIndent : 0, 0, 0, 0);

        Transform number = item.transform.Find("Number");
        number.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        SetColumnWidth(number, NumberWidth, 0);
        SetColumnWidth(item.transform.Find("White"), CellWidth, 0);
        SetColumnWidth(item.transform.Find("Black"), CellWidth, 0);
        item.transform.Find("White").GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
        item.transform.Find("Black").GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
        return item;
    }

    private static void SetColumnWidth(Transform column, float width, float flexible)
    {
        LayoutElement element = column.GetComponent<LayoutElement>();
        element.minWidth = width;
        element.preferredWidth = width;
        element.flexibleWidth = flexible;
        Text label = column.GetComponentInChildren<Text>();
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private void FitPanelWidth()
    {
        var contentRect = content.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        float width = RowWidth + VariationIndent;
        for (int i = 0; i < content.transform.childCount; i++)
        {
            var child = content.transform.GetChild(i) as RectTransform;
            if (child != null)
                width = Mathf.Max(width, LayoutUtility.GetPreferredWidth(child));
        }
        var padding = content.GetComponent<HorizontalOrVerticalLayoutGroup>().padding;
        width += padding.left + padding.right;
        var panelPadding = panel.GetComponent<HorizontalOrVerticalLayoutGroup>().padding;
        width += panelPadding.left + panelPadding.right;

        float inner = width - panelPadding.left - panelPadding.right;
        meta.horizontalOverflow = HorizontalWrapMode.Wrap;
        var metaLayout = meta.GetComponent<LayoutElement>();
        metaLayout.minWidth = inner;
        metaLayout.preferredWidth = inner;
        if (engineLayout != null)
        {
            engineLayout.minWidth = inner;
            engineLayout.preferredWidth = inner;
        }

        var rt = panel.GetComponent<RectTransform>();
        float scaleY = Mathf.Max(rt.localScale.y, 0.01f);
        var parent = (RectTransform)rt.parent;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parent.rect.height / scaleY);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void BindCell(Transform cell, string text, bool isVariation, int ply)
    {
        cell.GetComponentInChildren<Text>().text = text;
        cell.GetComponent<Button>().onClick.AddListener(() => MoveClicked?.Invoke(ply, isVariation));
        cells.Add(new ListCell
        {
            Image = cell.GetComponent<Image>(),
            Variation = isVariation,
            Ply = ply
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
}
