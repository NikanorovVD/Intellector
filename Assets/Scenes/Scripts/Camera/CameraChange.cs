using System;
using System.Threading.Tasks;

using Cinemachine;

using UnityEngine;

public class CameraChange : MonoBehaviour
{
    [SerializeField] private CinemachineFreeLook whiteCamera;
    [SerializeField] private CinemachineFreeLook blackCamera;
    [SerializeField] private CinemachineVirtualCamera topCamera;

    [SerializeField] private Board board;

    private bool currentTeam;
    private bool topCameraOn;

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
                if (board.GameOver) return;
                await Task.Delay(CameraAutoRotationDelayMs);
                currentTeam = !currentTeam;
                SwitchCamera();
            };
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchTopCamera();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateCamera();
        }
    }

    public void SwitchTopCamera()
    {
        topCameraOn = !topCameraOn;
        SwitchCamera();
    }

    public void RotateCamera()
    {
        currentTeam = !currentTeam;
        SwitchCamera();
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
