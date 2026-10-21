using System;
using System.Globalization;
using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AISetupMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private Text modeName;
    [SerializeField] private Text valueLabel;
    [SerializeField] private InputField valueInput;
    [SerializeField] private InputField ifenInput;
    [SerializeField] private Toggle customStartToggle;
    [SerializeField] private GameObject[] ifenControls;
    [SerializeField] private Text errorText;
    [SerializeField] private GameObject[] aiControls;
    [SerializeField] private ColorSelector colorSelector;
    [SerializeField] private LocalSetupMenu localSetup;

    private AISettings ai;
    private bool refreshing;
    private bool open;

    private void Awake()
    {
        valueInput.onValidateInput += ValidateChar;
        EnsureColorSelector();
    }

    private void Start()
    {
        if (ArrangeSession.TryTakeReturn(GameMode.AI))
            Open();
    }

    private void OnValidate()
    {
        EnsureColorSelector();
    }

    private void EnsureColorSelector()
    {
        if (colorSelector == null && panel != null)
            colorSelector = panel.GetComponentInChildren<ColorSelector>(true);
    }

    private void OnDestroy()
    {
        if (valueInput != null)
            valueInput.onValidateInput -= ValidateChar;
    }

    public void Open()
    {
        open = true;
        if (localSetup != null)
            localSetup.HideControls();
        ai = Settings.AI;
        Refresh();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        EnsureColorSelector();
        if (colorSelector != null)
            colorSelector.SetColor(ai.Color);
    }

    public void HideControls()
    {
        open = false;
        for (int i = 0; i < aiControls.Length; i++)
            aiControls[i].SetActive(false);
    }

    public void Close()
    {
        if (!open)
            return;

        open = false;
        panel.SetActive(false);
    }

    public void SwitchMode(int direction)
    {
        if (!open)
            return;

        int count = Enum.GetNames(typeof(AISearchMode)).Length;
        int next = ((int)ai.Mode + direction) % count;
        if (next < 0)
            next += count;
        ai.Mode = (AISearchMode)next;
        Refresh();
    }

    public void ChangeValue(int delta)
    {
        if (!open)
            return;

        if (!TryReadInput(out _))
        {
            Refresh();
            return;
        }

        switch (ai.Mode)
        {
            case AISearchMode.Time:
                ai.SearchTimeMs = Mathf.Max(1, ai.SearchTimeMs + delta * 1000);
                break;
            case AISearchMode.Level:
                ai.Level = Mathf.Clamp(ai.Level + delta, AISettings.MinLevel, AISettings.MaxLevel);
                break;
            case AISearchMode.Depth:
                ai.Depth = Mathf.Max(1, ai.Depth + delta);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ai.Mode), ai.Mode, null);
        }
        Refresh();
    }

    public void InputChanged()
    {
        if (!open || refreshing)
            return;

        string raw = valueInput.text ?? string.Empty;
        string filtered = FilterNumberText(raw);
        if (filtered != raw)
        {
            int caret = valueInput.caretPosition;
            refreshing = true;
            valueInput.text = filtered;
            refreshing = false;
            valueInput.caretPosition = Mathf.Clamp(caret, 0, filtered.Length);
        }

        TryReadInput(out _);
    }

    public void StartGame()
    {
        if (!open)
            return;
        if (!TryReadInput(out _))
            return;
        if (!TryReadIfen(out _))
            return;

        EnsureColorSelector();
        if (colorSelector != null)
            ai.Color = colorSelector.Color;
        Settings.AI = ai;
        Settings.PlayerTeam = ColorSelector.ResolveTeam(ai.Color);
        new GameInfo { TimeControl = TimeControl.Unlimited }.Save();
        Settings.GameMode = GameMode.AI;
        SceneManager.LoadScene(1);
    }

    private void Refresh()
    {
        refreshing = true;
        title.text = "Игра против ИИ";
        for (int i = 0; i < aiControls.Length; i++)
            aiControls[i].SetActive(true);
        modeName.text = ModeTitle(ai.Mode);
        valueLabel.text = ValueTitle(ai.Mode);
        valueInput.keyboardType = ai.Mode == AISearchMode.Time
            ? TouchScreenKeyboardType.DecimalPad
            : TouchScreenKeyboardType.NumberPad;
        valueInput.text = FormatInputValue();
        ifenInput.text = Settings.StartIfen ?? string.Empty;
        bool custom = !string.IsNullOrEmpty(Settings.StartIfen);
        customStartToggle.SetIsOnWithoutNotify(custom);
        SetIfenVisible(custom);
        errorText.text = string.Empty;
        refreshing = false;
    }

    public void CustomStartChanged(bool on)
    {
        if (!open || refreshing)
            return;

        SetIfenVisible(on);
        errorText.text = string.Empty;
    }

    private void SetIfenVisible(bool on)
    {
        for (int i = 0; i < ifenControls.Length; i++)
            ifenControls[i].SetActive(on);
    }

    public void BeginArrange()
    {
        if (!open)
            return;

        string backup = Settings.StartIfen;
        string raw = (ifenInput.text ?? string.Empty).Trim();
        if (raw.Length == 0)
            Settings.ClearStartPosition();
        else if (!Settings.TrySetStartIfen(raw, out string error))
        {
            errorText.text = error;
            return;
        }

        ArrangeSession.Enter(GameMode.AI, backup);
        Settings.GameMode = GameMode.Local;
        SceneManager.LoadScene(1);
    }

    private char ValidateChar(string text, int charIndex, char addedChar)
    {
        if (addedChar >= '0' && addedChar <= '9')
            return addedChar;

        if (ai.Mode != AISearchMode.Time)
            return '\0';

        if (addedChar != '.' && addedChar != ',')
            return '\0';

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '.' || text[i] == ',')
                return '\0';
        }

        return addedChar;
    }

    private string FilterNumberText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var filtered = new StringBuilder(raw.Length);
        bool hasSeparator = false;
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c >= '0' && c <= '9')
            {
                filtered.Append(c);
                continue;
            }

            if (ai.Mode == AISearchMode.Time && !hasSeparator && (c == '.' || c == ','))
            {
                filtered.Append(c);
                hasSeparator = true;
            }
        }

        return filtered.ToString();
    }

    private bool TryReadInput(out string error)
    {
        string raw = (valueInput.text ?? string.Empty).Trim().Replace(',', '.');
        if (raw.Length == 0)
        {
            error = "Введите значение";
            errorText.text = error;
            return false;
        }

        switch (ai.Mode)
        {
            case AISearchMode.Time:
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) || seconds <= 0)
                {
                    error = "Введите положительное время в секундах";
                    errorText.text = error;
                    return false;
                }
                ai.SearchTimeMs = Mathf.Max(1, (int)Math.Round(seconds * 1000.0));
                break;
            case AISearchMode.Level:
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedLevel)
                    || parsedLevel < AISettings.MinLevel || parsedLevel > AISettings.MaxLevel)
                {
                    error = $"Уровень должен быть целым числом от {AISettings.MinLevel} до {AISettings.MaxLevel}";
                    errorText.text = error;
                    return false;
                }
                ai.Level = parsedLevel;
                break;
            case AISearchMode.Depth:
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedDepth)
                    || parsedDepth < 1)
                {
                    error = "Глубина должна быть положительным целым числом";
                    errorText.text = error;
                    return false;
                }
                ai.Depth = parsedDepth;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ai.Mode), ai.Mode, null);
        }

        error = null;
        errorText.text = string.Empty;
        return true;
    }

    private bool TryReadIfen(out string error)
    {
        if (!customStartToggle.isOn)
        {
            Settings.ClearStartPosition();
            error = null;
            errorText.text = string.Empty;
            return true;
        }

        string raw = (ifenInput.text ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            error = "Введите IFEN";
            errorText.text = error;
            return false;
        }
        if (!Settings.TrySetStartIfen(raw, out error))
        {
            errorText.text = error;
            return false;
        }

        error = null;
        errorText.text = string.Empty;
        return true;
    }

    private string FormatInputValue()
    {
        return ai.Mode switch
        {
            AISearchMode.Depth => ai.Depth.ToString(CultureInfo.InvariantCulture),
            AISearchMode.Time => FormatSeconds(ai.SearchTimeMs),
            AISearchMode.Level => ai.Level.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(ai.Mode), ai.Mode, null)
        };
    }

    private static string FormatSeconds(int milliseconds)
    {
        if (milliseconds % 1000 == 0)
            return (milliseconds / 1000).ToString(CultureInfo.InvariantCulture);
        return (milliseconds / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string ModeTitle(AISearchMode searchMode)
    {
        return searchMode switch
        {
            AISearchMode.Depth => "Поиск на глубину",
            AISearchMode.Time => "Поиск на время",
            AISearchMode.Level => "Уровень сложности",
            _ => throw new ArgumentOutOfRangeException(nameof(searchMode), searchMode, null)
        };
    }

    private static string ValueTitle(AISearchMode searchMode)
    {
        return searchMode switch
        {
            AISearchMode.Depth => "Глубина:",
            AISearchMode.Time => "Время, с:",
            AISearchMode.Level => "Уровень:",
            _ => throw new ArgumentOutOfRangeException(nameof(searchMode), searchMode, null)
        };
    }
}
