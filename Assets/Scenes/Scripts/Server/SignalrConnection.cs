using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class SignalrConnection
{
    private string serverUrl;
    private string authToken;

    public SignalrConnection(string serverUrl, string authToken)
    {
        this.serverUrl = serverUrl + "Game"; // костыль
        this.authToken = authToken;
    }

    //private
    public HubConnection Connection { get; private set; }


    public async Task StartAsync()
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(serverUrl, options => options.AccessTokenProvider = () => Task.FromResult(authToken))
            .WithAutomaticReconnect()
            .Build();

        await Connection.StartAsync();
    }

    public void OnMoveReceived(Action<MoveResponse> action)
    {
        Connection.On<MoveResponse>("ReceiveMove", action);
    }

    public async Task SendMove(int gameId, Move move)
    {
        await Connection.InvokeAsync("SendMove", gameId, move);
    }
}
