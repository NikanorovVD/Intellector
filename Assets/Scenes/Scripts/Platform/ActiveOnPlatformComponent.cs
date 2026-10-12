using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum Platform
{
    Desktop,
    Android
}

public class ActiveOnPlatformComponent : MonoBehaviour
{
    [SerializeField] private Platform platform;

    private void Awake()
    {
        bool isAndroid;
#if UNITY_EDITOR
        isAndroid = AndroidSimulationMenu.IsAndroidSimulationOn();
#else
        isAndroid = Application.platform == RuntimePlatform.Android;
#endif
        bool active = platform switch
        {
            Platform.Android => isAndroid,
            Platform.Desktop => !isAndroid
        };
        gameObject.SetActive(active);
    }
}
