using System.Threading.Tasks;
using System;

public class ConnectionManager
{
    private static SignalrConnection signalrConnection;
    private static HttpConnection httpConnection;

    private static readonly Connection connectionSettings;
    public static bool Authorized { get; private set; }
    public static bool SignalrStarted { get; private set; } = false;

    public static HttpConnection HttpConnection
    {
        get => httpConnection;
    }
    public static SignalrConnection SignalrConnection
    {
        get => Authorized ? signalrConnection : throw new InvalidOperationException("SignalR connection requires authorization");
    }

    //public static async Task<SignalrConnection> StartedSignalrConnection()
    //{
    //    if (SignalrStarted)
    //    {
    //        return SignalrConnection;
    //    }
    //    else 
    //    {
    //        await SignalrConnection.StartAsync();
    //        SignalrStarted = true;
    //        return SignalrConnection;
    //    }
    //}

    static ConnectionManager()
    {
        connectionSettings = Settings.GetConnection();

        // проверка доступности сервера

        if (!string.IsNullOrEmpty(connectionSettings.AccessToken))
        {
            CreateAuthorizedConnections(connectionSettings.ServerUrl, connectionSettings.AccessToken);
        }
        else
        {
            CreateUnauthorizedConnections(connectionSettings.ServerUrl);
        }
    }

    public static async Task AuthorizeAsync(string username, string password)
    {
        string accessToken = await httpConnection.Authenticate(username, password);
        signalrConnection = new SignalrConnection(connectionSettings.ServerUrl, accessToken);
        await signalrConnection.StartAsync();
    }


    private static void CreateAuthorizedConnections(string serverUrl, string accessToken)
    {
        httpConnection = new HttpConnection(serverUrl, accessToken);
        signalrConnection = new SignalrConnection(serverUrl, accessToken);
        Authorized = true;
    }

    private static void CreateUnauthorizedConnections(string serverUrl)
    {
        httpConnection = new HttpConnection(serverUrl);
        signalrConnection = null;
        Authorized = false;
    }
}
