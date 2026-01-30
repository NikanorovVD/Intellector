using UnityEngine;

public class AndroidOnlyComponent : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(Application.platform == RuntimePlatform.Android);
    }
}
