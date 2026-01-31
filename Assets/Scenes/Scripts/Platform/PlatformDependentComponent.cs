using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum Platform
{
    Desktop,
    Android
}

public class PlatformDependentComponent : MonoBehaviour
{
    [SerializeField] private Platform platform;
    void Awake()
    {
#if UNITY_EDITOR
        bool active = platform switch
        {
            Platform.Android => AndroidSimulationMenu.IsAndroidSimulationOn(),
            Platform.Desktop => !AndroidSimulationMenu.IsAndroidSimulationOn()
        };
        gameObject.SetActive(active);
        return;
#endif

        gameObject.SetActive(Application.platform == RuntimePlatform.Android);
    }
}
