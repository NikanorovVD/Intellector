using System;
using System.Collections.Generic;
using UnityEngine;

// FIXME: костыльный инструмент маршализации, хочется верить что в Unity есть встроенные средства для этого
public class MainTasks : MonoBehaviour
{
    private static Queue<Action> tasks  = new Queue<Action>();

    private void Update()
    {
        try
        {
            if (tasks.Count != 0)
            {
                Action task = tasks.Dequeue();
                task();
            }
        }
        catch (Exception e) 
        { 
            LogWriter.WriteLog(e.ToString()); 
        }
    }

    public static void AddTask(Action task)
    {
        tasks.Enqueue(task);
    }
}
