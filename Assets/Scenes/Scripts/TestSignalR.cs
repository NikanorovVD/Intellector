using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

public static class IgnoreCertErrors
{
    // отключение проверки сертификатов
    public static void DisableCertificateCheck()
    {
        ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        };
    }
}

public class TestSignalR : MonoBehaviour
{
    async void Awake()
    {
        IgnoreCertErrors.DisableCertificateCheck();

        var connection = new HubConnectionBuilder()
       .WithUrl("https://localhost:7240/Game")
       .WithAutomaticReconnect()
       .Build();

        await connection.StartAsync();

        connection.On<string, string>("ReceiveMessage",
            (user, message) =>
            {
                MainTasks.AddTask(() =>
                {
                    Debug.Log($"{user}: {message}");
                });
            });

        await connection.InvokeAsync("SendMessage", "user", "message");
    }
}
