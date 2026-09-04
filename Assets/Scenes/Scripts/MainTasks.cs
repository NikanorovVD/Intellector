using System;
using System.Collections.Generic;
using UnityEngine;

// FIXME: костыльный инструмент маршализации, хочется верить что в Unity есть встроенные средства для этого
public class MainTasks : MonoBehaviour
{
    private static readonly object tasksLock = new();
    private static Queue<Action> tasks  = new Queue<Action>();

    private void Update()
    {
        try
        {
            Action task = null;
            lock (tasksLock)
            {
                if (tasks.Count != 0)
                    task = tasks.Dequeue();
            }
            task?.Invoke();
        }
        catch (Exception e) 
        { 
            LogWriter.WriteLog(e.ToString()); 
        }
    }

    public static void AddTask(Action task)
    {
        lock (tasksLock)
            tasks.Enqueue(task);
    }
}
