using System;
using UnityEngine;
using UnityEngine.UI;

public class Logger : MonoBehaviour
{
    [SerializeField] public Text logText;

    void OnEnable()
    {
        //Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        //Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        logText.text += condition + ' ';
    }
}
