using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameInfoWindow : MonoBehaviour
{
    [SerializeField] InputField NameInput;
    [SerializeField] Dropdown TimeControlDropDown;
    [SerializeField] ColorSelector ColorSelector;

    [SerializeField] Text ErrorText;
    private void Awake()
    {
        NameInput.text = Settings.UserName;
        TimeControlDropDown.options = new List<Dropdown.OptionData>();
        foreach(TimeControl time in TimeControlSelector.time_controls)
        {
            TimeControlDropDown.options.Add(new Dropdown.OptionData(time.ToString()));
        }
    }

    public CreateOpenLobbyRequest GetGameInfo()
    {
        string Name = NameInput.text;
        if(!CheckName()) return null;

        TimeControl timeContol = TimeControlSelector.time_controls[TimeControlDropDown.value];
        ColorChoice color = ColorSelector.Color;

        return new CreateOpenLobbyRequest
        {
            ColorChoice = color,
            Rating = true, // edit
            TimeControl = new TimeControlDto(timeContol.TotalSeconds, timeContol.AddedSeconds)
        }; 
    }

    public void NameInputChanged()
    {
        CheckName();
    }

    private bool CheckName()
    {
        string error_mes;
        bool valid = UserNameValidator.CheckName(NameInput.text, out error_mes);
        ErrorText.text = error_mes;
        return valid;
    }
}
