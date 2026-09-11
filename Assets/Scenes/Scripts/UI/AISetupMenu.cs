using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AISetupMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Text title;
    [SerializeField] Text modeName;
    [SerializeField] Text valueLabel;
    [SerializeField] InputField valueInput;
    [SerializeField] InputField ifenInput;
    [SerializeField] Toggle customStartToggle;
    [SerializeField] GameObject[] ifenControls;
    [SerializeField] Text errorText;
    [SerializeField] GameObject[] aiControls;

    private AISettings ai;
    private bool refreshing;
    private GameMode pendingMode = GameMode.AI;

    private void Awake()
    {
        valueInput.onValidateInput += ValidateChar;
    }

    private void OnDestroy()
    {
        if (valueInput != null)
            valueInput.onValidateInput -= ValidateChar;
    }

    public void Open()
    {
        Open(GameMode.AI);
    }

    public void Open(GameMode mode)
    {
        pendingMode = mode;
        if (mode == GameMode.AI)
            ai = Settings.AI;
        Refresh();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void SwitchMode(int direction)
    {
        if (pendingMode != GameMode.AI) return;
        int count = Enum.GetNames(typeof(AISearchMode)).Length;
        int next = ((int)ai.Mode + direction) % count;
        if (next < 0)
            next += count;
        ai.Mode = (AISearchMode)next;
        Refresh();
    }

    public void ChangeValue(int delta)
    {
        if (pendingMode != GameMode.AI) return;
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
        if (refreshing)
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
        if (pendingMode == GameMode.AI && !TryReadInput(out _))
            return;
        if (!TryReadIfen(out _))
            return;

        if (pendingMode == GameMode.AI)
            Settings.AI = ai;
        Settings.GameMode = pendingMode;
        SceneManager.LoadScene(1);
    }

    private void Refresh()
    {
        refreshing = true;
        title.text = pendingMode == GameMode.Local ? "Локальная игра" : "Игра против ИИ";
        bool aiUi = pendingMode == GameMode.AI;
        for (int i = 0; i < aiControls.Length; i++)
            aiControls[i].SetActive(aiUi);
        if (aiUi)
        {
            modeName.text = ModeTitle(ai.Mode);
            valueLabel.text = ValueTitle(ai.Mode);
            valueInput.keyboardType = ai.Mode == AISearchMode.Time
                ? TouchScreenKeyboardType.DecimalPad
                : TouchScreenKeyboardType.NumberPad;
            valueInput.text = FormatInputValue();
        }
        ifenInput.text = Settings.StartIfen ?? string.Empty;
        bool custom = !string.IsNullOrEmpty(Settings.StartIfen);
        customStartToggle.SetIsOnWithoutNotify(custom);
        SetIfenVisible(custom);
        errorText.text = string.Empty;
        refreshing = false;
    }

    public void CustomStartChanged(bool on)
    {
        if (refreshing) return;
        SetIfenVisible(on);
        errorText.text = string.Empty;
    }

    private void SetIfenVisible(bool on)
    {
        for (int i = 0; i < ifenControls.Length; i++)
            ifenControls[i].SetActive(on);
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
