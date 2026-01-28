using Cinemachine;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class CameraChange : MonoBehaviour
{
    public CinemachineFreeLook WhiteCamera;
    public CinemachineFreeLook BlackCamera;
    public CinemachineVirtualCamera TopCamera;

    [SerializeField] private Board board;

    private bool currentTeam;
    private bool topCameraOn;

    private const int cameraAutoRotationDalayMs = 200;

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

        if(Settings.GameMode == GameMode.Local && Settings.AutoRotateCameraInLocalGame)
        {
            board.MoveEndEvent += async (Vector2Int _, Vector2Int _, int _) =>
            {
                if(board.game_over) return;
                await Task.Delay(cameraAutoRotationDalayMs);
                currentTeam = !currentTeam;
                SwitchCamera();
            };
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            topCameraOn = !topCameraOn;
            SwitchCamera();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentTeam = !currentTeam;
            SwitchCamera();
        }
    }
    private void SwitchCamera()
    {
        if (topCameraOn)
        {
            if (currentTeam == false)
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
            if (currentTeam == false)
            {
                SwitchToWhiteMainCamera();
            }
            else
            {
                SwitchToBlackMainCamera();
            }
        }
    }


    private void SwitchToWhiteMainCamera()
    {
        TopCamera.gameObject.SetActive(false);
        BlackCamera.gameObject.SetActive(false);
        WhiteCamera.gameObject.SetActive(true);
    }

    private void SwitchToBlackMainCamera()
    {
        TopCamera.gameObject.SetActive(false);
        BlackCamera.gameObject.SetActive(true);
        WhiteCamera.gameObject.SetActive(false);
    }

    private void SwitchToTopCamera()
    {
        TopCamera.gameObject.SetActive(true);
        BlackCamera.gameObject.SetActive(false);
        WhiteCamera.gameObject.SetActive(false);
    }

    private void RotateTopCameraToWhite()
    {
        TopCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private void RotateTopCameraToBlack()
    {
        TopCamera.transform.rotation = Quaternion.Euler(90, 0, 180);
    }
}