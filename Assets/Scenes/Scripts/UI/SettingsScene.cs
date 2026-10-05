using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsScene : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer;

    private InputField nameInput;
    private Text errorText;
    private Text materialName;
    private Text autoRotateCameraText;

    private void Awake()
    {
        nameInput = uiContainer.transform.Find("Content/NameInput").GetComponent<InputField>();
        errorText = uiContainer.transform.Find("Content/NameInput/ErrorMessage").GetComponent<Text>();
        materialName = uiContainer.transform.Find("Content/MaterialChoose/CurrentMaterialName").GetComponent<Text>();
        autoRotateCameraText = uiContainer.transform.Find("Content/AutorotateChoose/CurrentAutorotate").GetComponent<Text>();
    }

    private void Start()
    {
        ShowCurrentSettings();
    }

    private void ShowCurrentSettings()
    {
        nameInput.text = Settings.UserName;
        materialName.text = MaterialSelector.MaterialName(Settings.PieceMaterials);
        autoRotateCameraText.text = Settings.AutoRotateCameraInLocalGame ? "Да" : "Нет";
    }

    public void InputChanged() => CheckName();

    private bool CheckName()
    {
        string errorMes;
        bool valid = UserNameValidator.CheckName(nameInput.text, out errorMes);
        errorText.text = errorMes;
        return valid;
    }

    public void SaveButtonClick()
    {
        if (CheckName())
        {
            Settings.UserName = nameInput.text;
            Exit();
        }
    }

    public void CancelButtonClick()
    {
        ShowCurrentSettings();
        errorText.text = String.Empty;
    }

    public void SwitchMaterial(int direction)
    {
        int newMaterialsNumber = ((int)Settings.PieceMaterials + direction);
        int maxNumber = Enum.GetNames(typeof(PieceMaterials)).Length;
        if (newMaterialsNumber >= maxNumber) newMaterialsNumber = 0;
        if (newMaterialsNumber < 0) newMaterialsNumber = maxNumber - 1;
        Settings.PieceMaterials = (PieceMaterials)(newMaterialsNumber);
        materialName.text = MaterialSelector.MaterialName(Settings.PieceMaterials);
    }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    }

    public void SwitchCameraAutoRotation()
    {
        Settings.AutoRotateCameraInLocalGame = !Settings.AutoRotateCameraInLocalGame;
        autoRotateCameraText.text = Settings.AutoRotateCameraInLocalGame ? "Да" : "Нет";
    }
}
