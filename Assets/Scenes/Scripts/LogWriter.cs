using System;
using System.IO;

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