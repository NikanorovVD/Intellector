using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsScene : MonoBehaviour
{
    [SerializeField] private GameObject UIContainer;

    private InputField NameInput;
    private Text ErrorText;
    private Text MaterialName;
    private Text AutoRotateCameraText;

    private void Awake()
    {
        NameInput = UIContainer.transform.Find("Content/NameInput").GetComponent<InputField>();
        ErrorText = UIContainer.transform.Find("Content/NameInput/ErrorMessage").GetComponent<Text>();
        MaterialName = UIContainer.transform.Find("Content/MaterialChoose/CurrentMaterialName").GetComponent<Text>();
        AutoRotateCameraText = UIContainer.transform.Find("Content/AutorotateChoose/CurrentAutorotate").GetComponent<Text>();
    }

    void Start()
    {
        ShowCurrentSettings();
    }

    private void ShowCurrentSettings()
    {
        NameInput.text = Settings.UserName;
        MaterialName.text = MaterialSelector.MaterialName(Settings.PieceMaterials);
        AutoRotateCameraText.text = Settings.AutoRotateCameraInLocalGame ? "Да" : "Нет";
    }

    public void InputChanged() => CheckName();

    private bool CheckName()
    {
        string error_mes;
        bool valid = UserNameValidator.CheckName(NameInput.text, out error_mes);
        ErrorText.text = error_mes;
        return valid;
    }

    public void SaveButtonClick()
    {
        if (CheckName())
        {
            Settings.UserName = NameInput.text;
            Exit();
        }
    }

    public void CancelButtonClick()
    {
        ShowCurrentSettings();
        ErrorText.text = String.Empty;
    }

    public void SwitchMaterial(int direction)
    {
        int new_materials_number = ((int)Settings.PieceMaterials + direction);
        int max_number = Enum.GetNames(typeof(PieceMaterials)).Length;
        if (new_materials_number >= max_number) new_materials_number = 0;
        if (new_materials_number < 0) new_materials_number = max_number - 1;
        Settings.PieceMaterials = (PieceMaterials)(new_materials_number);
        MaterialName.text = MaterialSelector.MaterialName(Settings.PieceMaterials);
    }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    }

    public void SwitchCameraAutoRotation()
    {
        Settings.AutoRotateCameraInLocalGame = !Settings.AutoRotateCameraInLocalGame;
        AutoRotateCameraText.text = Settings.AutoRotateCameraInLocalGame ? "Да" : "Нет";
    }
}
