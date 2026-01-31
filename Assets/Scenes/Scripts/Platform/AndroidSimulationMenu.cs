#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AndroidSimulationMenu
{
    public static bool IsAndroidSimulationOn()
    {
        return EditorPrefs.GetBool("AndroidSimulationActive", false);
    }

    [MenuItem("Tools/Android/Simulation ON")]
    private static void EnableAndroidSim()
    {
        EditorPrefs.SetBool("AndroidSimulationActive", true);
    }

    [MenuItem("Tools/Android/Simulation OFF")]
    private static void DisableAndroidSim()
    {
        EditorPrefs.SetBool("AndroidSimulationActive", false);
    }   
}
#endif
