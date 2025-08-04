using System.Threading.Tasks;
using UnityEngine;

public class AppInitializer 
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async void Initialize()
    {
        InitializeMainTasksDispatcher();
        await InitializeSignalrConnection();
    }

    static void InitializeMainTasksDispatcher()
    {
        if (!Object.FindObjectOfType<MainTasks>())
        {
            GameObject mainTasksDispatcher = new GameObject("MainTasks");
            mainTasksDispatcher.AddComponent<MainTasks>();
            Object.DontDestroyOnLoad(mainTasksDispatcher);
        }
    }

    static async Task InitializeSignalrConnection()
    {
        if (ConnectionManager.Authorized)
        {
            await ConnectionManager.SignalrConnection.StartAsync();
        }
    }
}
