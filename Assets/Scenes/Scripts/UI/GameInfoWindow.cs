using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class GameInfoWindow : MonoBehaviour
{
    [SerializeField] private InputField nameInput;
    [SerializeField] private Dropdown timeControlDropDown;
    [SerializeField] private ColorSelector colorSelector;

    [SerializeField] private Text errorText;
    private void Awake()
    {
        nameInput.text = Settings.UserName;
        timeControlDropDown.options = new List<Dropdown.OptionData>();
        foreach(TimeControl time in TimeControlSelector.TimeControls)
        {
            timeControlDropDown.options.Add(new Dropdown.OptionData(time.ToString()));
        }
    }

    public GameInfo GetGameInfo()
    {
        string name = nameInput.text;
        if(!CheckName()) return null;

        TimeControl timeControl = TimeControlSelector.TimeControls[timeControlDropDown.value];
        ColorChoice color = colorSelector.Color;

        return new GameInfo { ID = 0, Color = color, Name = name, TimeControl = timeControl };
    }

    public void NameInputChanged()
    {
        CheckName();
    }

    private bool CheckName()
    {
        string errorMes;
        bool valid = UserNameValidator.CheckName(nameInput.text, out errorMes);
        errorText.text = errorMes;
        return valid;
    }
}
