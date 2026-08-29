using System;
using System.IO;

// FIXME: нет времени в логах; почему-то пишем в файл, но не в консоль, хотя #if для редактора
public class LogWriter
{
    const string LogFilePath = "log.txt";

    public static void WriteLog(string message)
    {
#if UNITY_EDITOR
        try
        {
            using (StreamWriter LogStream = new StreamWriter(LogFilePath, true))
            {
                LogStream.WriteLine(message);
            }
        }
        catch (Exception) { }
#endif
    }
}