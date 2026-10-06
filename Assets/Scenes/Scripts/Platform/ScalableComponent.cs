using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScalableComponent : MonoBehaviour
{
    [SerializeField] private float desktopScale = 1;
    [SerializeField] private float androidScale;

    private void Awake()
    {
        bool isAndroid;
#if UNITY_EDITOR
        isAndroid = AndroidSimulationMenu.IsAndroidSimulationOn();
#else
        isAndroid = Application.platform == RuntimePlatform.Android;
#endif
        transform.localScale = isAndroid
            ? new Vector3(androidScale, androidScale, 1)
            : new Vector3(desktopScale, desktopScale, 1);

    }
}
