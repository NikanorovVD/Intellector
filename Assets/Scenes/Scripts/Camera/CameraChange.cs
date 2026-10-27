using System;
using System.Threading.Tasks;

using Cinemachine;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraChange : MonoBehaviour
{
    [SerializeField] private CinemachineFreeLook whiteCamera;
    [SerializeField] private CinemachineFreeLook blackCamera;
    [SerializeField] private CinemachineVirtualCamera topCamera;

    [SerializeField] private Board board;

    private Side currentTeam;
    private bool topCameraOn;

    public event Action<Side> ViewSideChanged;

    public Side ViewSide => currentTeam;

    private const int CameraAutoRotationDelayMs = 200;

    private void Start()
    {
        topCameraOn = false;
        currentTeam = board.PlayerTeam;

        SwitchCamera();

        board.RestartEvent += () =>
        {
            currentTeam = board.PlayerTeam;
            SwitchCamera();
        };

        if (Settings.GameMode == GameMode.Local && Settings.AutoRotateCameraInLocalGame)
        {
            board.MoveEndEvent += async (Vector2Int _, Vector2Int _, int _) =>
            {
                if (board.IsGameOver) return;
                await Task.Delay(CameraAutoRotationDelayMs);
                currentTeam = Sides.Opposite(currentTeam);
                SwitchCamera();
            };
        }
    }

    private void Update()
    {
        if (IsTextInputFocused())
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchTopCamera();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateCamera();
        }
    }

    private static bool IsTextInputFocused()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null)
            return false;

        InputField input = selected.GetComponent<InputField>();
        return input != null && input.isFocused;
    }

    public void SwitchTopCamera()
    {
        topCameraOn = !topCameraOn;
        SwitchCamera();
    }

    public void RotateCamera()
    {
        currentTeam = Sides.Opposite(currentTeam);
        SwitchCamera();
    }

    private void SwitchCamera()
    {
        if (topCameraOn)
        {
            if (currentTeam == Side.White)
            {
                SwitchToTopCamera();
                RotateTopCameraToWhite();
            }
            else
            {
                SwitchToTopCamera();
                RotateTopCameraToBlack();
            }
        }
        else
        {
            if (currentTeam == Side.White)
            {
                SwitchToWhiteMainCamera();
            }
            else
            {
                SwitchToBlackMainCamera();
            }
        }

        ViewSideChanged?.Invoke(currentTeam);
    }

    private void SwitchToWhiteMainCamera()
    {
        topCamera.gameObject.SetActive(false);
        blackCamera.gameObject.SetActive(false);
        whiteCamera.gameObject.SetActive(true);
    }

    private void SwitchToBlackMainCamera()
    {
        topCamera.gameObject.SetActive(false);
        blackCamera.gameObject.SetActive(true);
        whiteCamera.gameObject.SetActive(false);
    }

    private void SwitchToTopCamera()
    {
        topCamera.gameObject.SetActive(true);
        blackCamera.gameObject.SetActive(false);
        whiteCamera.gameObject.SetActive(false);
    }

    private void RotateTopCameraToWhite()
    {
        topCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private void RotateTopCameraToBlack()
    {
        topCamera.transform.rotation = Quaternion.Euler(90, 0, 180);
    }
}
