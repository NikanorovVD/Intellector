using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScaleableComponent : MonoBehaviour
{
    [SerializeField] private float DesktopScale = 1;
    [SerializeField] private float AndroidScale;

    void Awake()
    {
        // TODO: вынести флаг того что у нас Android в переменную
#if UNITY_EDITOR
        transform.localScale = AndroidSimulationMenu.IsAndroidSimulationOn() ? 
            new Vector3(AndroidScale, AndroidScale, 1) :
            new Vector3(DesktopScale, DesktopScale, 1);
        return;
#endif
        transform.localScale = Application.platform == RuntimePlatform.Android ?
            new Vector3(AndroidScale, AndroidScale, 1) :
            new Vector3(DesktopScale, DesktopScale, 1);
    }
}
