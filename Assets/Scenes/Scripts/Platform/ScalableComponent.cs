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
        // TODO: вынести флаг того что у нас Android в переменную
#if UNITY_EDITOR
        transform.localScale = AndroidSimulationMenu.IsAndroidSimulationOn() ?
            new Vector3(androidScale, androidScale, 1) :
            new Vector3(desktopScale, desktopScale, 1);
        return;
#endif
        transform.localScale = Application.platform == RuntimePlatform.Android ?
            new Vector3(androidScale, androidScale, 1) :
            new Vector3(desktopScale, desktopScale, 1);
    }
}
