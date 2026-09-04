using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ReplayController : MonoBehaviour
{
    static readonly Color RowColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    static readonly Color SelectedRowColor = new Color(0.45f, 0.4f, 0.15f, 1f);

    private const float NumberWidth = 22f;
    private const float CellWidth = 110f;
    private const float RowSpacing = 4f;
    private const float RowWidth = NumberWidth + CellWidth * 2 + RowSpacing * 2;

    [SerializeField] public Board Board;
    [SerializeField] GameObject panel;
    [SerializeField] GameObject content;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Text meta;

    private List<ReplayMove> moves;
    private readonly List<Image> rows = new();
    private int index;
    private int firstPly;
    private int firstFullmove;

    void Start()
    {
        if (Settings.GameMode != GameMode.Replay)
        {
            panel.SetActive(false);
            enabled = false;
            return;
        }

        string text = File.ReadAllText(Settings.ReplayFilePath);
        GameRecord record = IpgnParser.Parse(text);
        if (record.SetUp == "1" && !string.IsNullOrEmpty(record.Ifen))
            Board.LoadPosition(IfenParser.Parse(record.Ifen));
        moves = ReplayExpander.Expand(record);
        IpgnFormatter.GetMovetextOrigin(record, out firstPly, out firstFullmove);
        index = 0;
        Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        meta.text = FormatMeta(record);
        panel.SetActive(true);
        FillList();
        FitPanelWidth();
        HighlightList();
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

    private static void AppendMetaLine(StringBuilder builder, string value)
    {
        if (!string.IsNullOrEmpty(value))
            builder.AppendLine(value);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Forward();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            Back();
    }

    private void Forward()
    {
        if (ApplyForward())
            AfterStep();
    }

    private void Back()
    {
        if (ApplyBack())
            AfterStep();
    }

    private void JumpTo(int target)
    {
        if (moves == null) return;
        target = Mathf.Clamp(target, 0, moves.Count);
        while (index < target && ApplyForward()) { }
        while (index > target && ApplyBack()) { }
        AfterStep();
    }

    private bool ApplyForward()
    {
        if (moves == null || index >= moves.Count) return false;
        ReplayMove move = moves[index];
        Board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
        index++;
        return true;
    }

    private bool ApplyBack()
    {
        if (moves == null || index <= 0) return false;
        index--;
        ReplayMove move = moves[index];
        Board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        return true;
    }

    private void AfterStep()
    {
        if (index == 0)
            Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
        else
        {
            ReplayMove move = moves[index - 1];
            Board.HighlightLastMove(move.From, move.To);
        }
        HighlightList();
    }

    private void FillList()
    {
        int i = 0;
        int number = firstFullmove;
        if (firstPly == 1 && moves.Count > 0)
        {
            AddTurnRow(number, null, 0, moves[0].Notation, 1);
            i = 1;
            number++;
        }
        for (; i < moves.Count; i += 2)
        {
            string black = i + 1 < moves.Count ? moves[i + 1].Notation : null;
            AddTurnRow(number, moves[i].Notation, i + 1, black, i + 2);
            number++;
        }
    }

    private void AddTurnRow(int number, string white, int whiteTarget, string black, int blackTarget)
    {
        GameObject item = SpawnRow();
        item.transform.Find("Number").GetComponent<Text>().text = number + ".";
        Transform whiteCell = item.transform.Find("White");
        if (white == null)
        {
            whiteCell.GetComponent<Button>().interactable = false;
            whiteCell.GetComponentInChildren<Text>().text = "";
        }
        else
            BindCell(whiteCell, white, whiteTarget);
        Transform blackCell = item.transform.Find("Black");
        if (black == null)
        {
            blackCell.GetComponent<Button>().interactable = false;
            blackCell.GetComponentInChildren<Text>().text = "";
        }
        else
            BindCell(blackCell, black, blackTarget);
    }

    private GameObject SpawnRow()
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
        hlg.padding = new RectOffset(0, 0, 0, 0);

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
        float width = RowWidth;
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

        var rt = panel.GetComponent<RectTransform>();
        float scaleY = Mathf.Max(rt.localScale.y, 0.01f);
        var parent = (RectTransform)rt.parent;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parent.rect.height / scaleY);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void BindCell(Transform cell, string text, int target)
    {
        cell.GetComponentInChildren<Text>().text = text;
        cell.GetComponent<Button>().onClick.AddListener(() => JumpTo(target));
        rows.Add(cell.GetComponent<Image>());
    }

    private void HighlightList()
    {
        for (int i = 0; i < rows.Count; i++)
            rows[i].color = i == index - 1 ? SelectedRowColor : RowColor;
    }
}
