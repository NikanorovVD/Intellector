using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LocalSetupMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private InputField ifenInput;
    [SerializeField] private Toggle customStartToggle;
    [SerializeField] private GameObject[] ifenControls;
    [SerializeField] private Text errorText;
    [SerializeField] private Dropdown timeControlDropDown;
    [SerializeField] private GameObject[] timeControls;
    [SerializeField] private AISetupMenu aiSetup;

    private bool refreshing;
    private bool open;
    private static int savedTimeControlIndex;

    private void Awake()
    {
        FillTimeControls();
    }

    private void Start()
    {
        if (ArrangeSession.TryTakeReturn(GameMode.Local))
            Open();
    }

    private void OnDestroy()
    {
        if (timeControlDropDown != null)
            timeControlDropDown.onValueChanged.RemoveListener(TimeControlChanged);
    }

    public void Open()
    {
        open = true;
        if (aiSetup != null)
            aiSetup.HideControls();
        Refresh();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void HideControls()
    {
        open = false;
        for (int i = 0; i < timeControls.Length; i++)
            timeControls[i].SetActive(false);
    }

    public void Close()
    {
        if (!open)
            return;

        open = false;
        panel.SetActive(false);
    }

    public void StartGame()
    {
        if (!open)
            return;
        if (!TryReadIfen(out _))
            return;

        Settings.PlayerTeam = Side.White;
        SaveTimeControl();
        Settings.GameMode = GameMode.Local;
        SceneManager.LoadScene(1);
    }

    public void CustomStartChanged(bool on)
    {
        if (!open || refreshing)
            return;

        SetIfenVisible(on);
        errorText.text = string.Empty;
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

        ArrangeSession.Enter(GameMode.Local, backup);
        Settings.GameMode = GameMode.Local;
        SceneManager.LoadScene(1);
    }

    private void Refresh()
    {
        refreshing = true;
        title.text = "Локальная игра";
        for (int i = 0; i < timeControls.Length; i++)
            timeControls[i].SetActive(true);
        ifenInput.text = Settings.StartIfen ?? string.Empty;
        bool custom = !string.IsNullOrEmpty(Settings.StartIfen);
        customStartToggle.SetIsOnWithoutNotify(custom);
        SetIfenVisible(custom);
        errorText.text = string.Empty;
        refreshing = false;
    }

    private void SetIfenVisible(bool on)
    {
        for (int i = 0; i < ifenControls.Length; i++)
            ifenControls[i].SetActive(on);
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

    private void FillTimeControls()
    {
        if (timeControlDropDown == null)
            return;

        timeControlDropDown.onValueChanged.RemoveListener(TimeControlChanged);
        timeControlDropDown.ClearOptions();
        var options = new List<Dropdown.OptionData>();
        foreach (TimeControl time in TimeControlSelector.TimeControls)
            options.Add(new Dropdown.OptionData(time.ToString()));
        timeControlDropDown.AddOptions(options);
        int index = Mathf.Clamp(savedTimeControlIndex, 0, options.Count - 1);
        timeControlDropDown.SetValueWithoutNotify(index);
        timeControlDropDown.RefreshShownValue();
        timeControlDropDown.onValueChanged.AddListener(TimeControlChanged);
    }

    private void TimeControlChanged(int index)
    {
        savedTimeControlIndex = index;
    }

    private void SaveTimeControl()
    {
        TimeControl timeControl = TimeControl.Unlimited;
        if (timeControlDropDown != null)
        {
            int index = timeControlDropDown.value;
            if (index >= 0 && index < TimeControlSelector.TimeControls.Count)
            {
                savedTimeControlIndex = index;
                timeControl = TimeControlSelector.TimeControls[index];
            }
        }

        new GameInfo { TimeControl = timeControl }.Save();
    }
}
