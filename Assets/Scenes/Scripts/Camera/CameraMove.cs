using Cinemachine;

using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField]
    private CinemachineFreeLook currentCamera;

    private void Start()
    {
        currentCamera.m_YAxis.m_MaxSpeed = 0;
        currentCamera.m_XAxis.m_MaxSpeed = 0;
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = GetComponent<CinemachineFreeLook>();
            currentCamera.m_YAxis.m_MaxSpeed = 0;
            currentCamera.m_XAxis.m_MaxSpeed = 0;
            return;
        }
        if (Input.GetMouseButtonDown(1))
        {
            currentCamera.m_YAxis.m_MaxSpeed = 10;
            currentCamera.m_XAxis.m_MaxSpeed = 800;
        }
        if (Input.GetMouseButtonUp(1))
        {
            currentCamera.m_YAxis.m_MaxSpeed = 0;
            currentCamera.m_XAxis.m_MaxSpeed = 0;
        }
    }
}
