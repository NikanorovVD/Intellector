using System;
using System.IO;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class LogWriter
{
    private const string LogDirectory = "Logs";
    private static readonly object fileLock = new object();

    public static void Debug(string message) => Write(LogLevel.Debug, message);

    public static void Info(string message) => Write(LogLevel.Info, message);

    public static void Warning(string message) => Write(LogLevel.Warning, message);

    public static void Error(string message) => Write(LogLevel.Error, message);

    public static void Error(Exception exception)
    {
        if (exception == null) return;

        Append(LogLevel.Error, exception.ToString());
#if UNITY_EDITOR
        UnityEngine.Debug.LogException(exception);
#endif
    }

    private static void Write(LogLevel level, string message)
    {
        Append(level, message);
#if UNITY_EDITOR
        switch (level)
        {
            case LogLevel.Warning:
                UnityEngine.Debug.LogWarning(message);
                break;
            case LogLevel.Error:
                UnityEngine.Debug.LogError(message);
                break;
            default:
                UnityEngine.Debug.Log(message);
                break;
        }
#endif
    }

    private static void Append(LogLevel level, string message)
    {
        DateTime now = DateTime.Now;
        string line = $"{now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        string path = Path.Combine(LogDirectory, $"{now:yyyy-MM-dd}.log");

        try
        {
            lock (fileLock)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (Exception fileError)
        {
            ReportFileError(fileError);
        }
    }

    private static void ReportFileError(Exception fileError)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.LogError(fileError);
#endif
    }
}
