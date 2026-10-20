using System;
using System.Threading;

using UnityEngine;

public static class MainTasks
{
    private static SynchronizationContext context;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Capture()
    {
        context = SynchronizationContext.Current;
    }

    public static void AddTask(Action task)
    {
        if (context != null && SynchronizationContext.Current == context)
        {
            task();
            return;
        }

        context.Post(_ => task(), null);
    }
}
