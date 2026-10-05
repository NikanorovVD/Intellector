using System.Net.Sockets;

using static Networking;

public class ServerConnection
{
    public TcpClient Client { get; private set; }

    private static ServerConnection instance;

    private ServerConnection(TcpClient client)
    {
        Client = client;
    }

    public void Close() => Client.Close();

    public static ServerConnection GetConnection()
    {
        if (instance == null || !instance.Client.Connected) instance = new ServerConnection(ConnectToServer());
        return instance;
    }

    private static TcpClient ConnectToServer()
    {
        Connection connection = Settings.GetConnection();
        TcpClient client = new TcpClient(connection.ServerIP, connection.Port);

        SendString(connection.Password, client.GetStream());
        CheckVersion(client.GetStream());

        return client;
    }

    private static void CheckVersion(NetworkStream stream)
    {
        SendInt(Settings.AppVersion, stream);
        int serverVersion = RecvInt(stream);
        if (Settings.AppVersion != serverVersion)
        {
            throw new VersionException(
             $"\"Неподходящая версия\n" +
             $"Версия сервера - {VerToStr(serverVersion)}\n" +
             $"Используемая версия клиента - {VerToStr(Settings.AppVersion)}\""
             );
        }

        string VerToStr(int ver) => $"{ver / 10}.{ver % 10}";
    }
}
