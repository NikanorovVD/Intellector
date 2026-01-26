using Cinemachine;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField]
    private CinemachineFreeLook _currentCamera;

    private void Start()
    {
        _currentCamera.m_YAxis.m_MaxSpeed = 0;
        _currentCamera.m_XAxis.m_MaxSpeed = 0;
    }

    void Update()
    {
        if (!_currentCamera)
        {
            _currentCamera = GetComponent<CinemachineFreeLook>();
            _currentCamera.m_YAxis.m_MaxSpeed = 0;
            _currentCamera.m_XAxis.m_MaxSpeed = 0;
            return;
        }
        if (Input.GetMouseButtonDown(1))
        {
            _currentCamera.m_YAxis.m_MaxSpeed = 10;
            _currentCamera.m_XAxis.m_MaxSpeed = 800;
        }
        if (Input.GetMouseButtonUp(1))
        {
            _currentCamera.m_YAxis.m_MaxSpeed = 0;
            _currentCamera.m_XAxis.m_MaxSpeed = 0;
        }
    }
}