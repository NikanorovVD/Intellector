using API.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


public class HttpConnection
{
    private HttpClient httpClient;
    public HttpConnection(string serverUrl)
    {
        httpClient = new HttpClient() { BaseAddress = new Uri(serverUrl) };
    }

    public HttpConnection(string serverUrl, string authToken)
        : this(serverUrl)
    {
        SetAuthToken(authToken);
    }


    public async Task<string> Authenticate(string userName, string password)
    {
        string token = await GetAuthToken(userName, password);
        SetAuthToken(token);
        return token;
    }

    public void SetAuthToken(string authToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    public async Task<string> GetAuthToken(string userName, string password)
    {
        AuthRequest authRequest = new AuthRequest() { UserName = userName, Password = password };
        TokenDto tokenDto = await RequestAsync<AuthRequest, TokenDto>(ServerRoutes.Login, HttpMethod.Post, authRequest);
        string token = tokenDto.Token;
        return token;
    }

    public async Task<IEnumerable<OpenLobbyDto>> GetOpenLobbies()
    {
        return await RequestAsync<IEnumerable<OpenLobbyDto>>(ServerRoutes.GetOpenLobbies, HttpMethod.Get);
    }

    public async Task<GameInfoResponse> JoinOpenLobby(int lobbyId)
    {
        return await RequestAsync<GameInfoResponse>(ServerRoutes.JoinOpenLobby(lobbyId), HttpMethod.Post);
    }

    public async Task<int> CreateLobby(CreateOpenLobbyRequest request)
    {
        return await RequestAsync<CreateOpenLobbyRequest, int>(ServerRoutes.CreateOpenLobby, HttpMethod.Post, request);
    }

    public async Task DeleteLobby(int id)
    {
        await RequestAsync(ServerRoutes.DeleteLobby(id), HttpMethod.Delete);
    }

    public async Task UpdateLobby(int id)
    {
        await RequestAsync(ServerRoutes.UpdateLobby(id), HttpMethod.Patch);
    }

    private async Task<TResponse> RequestAsync<TResponse>(string url, HttpMethod method)
    {
        HttpRequestMessage request = new(method, url);
        return await RequestAsync<TResponse>(request);
    }

    private async Task<TResponse> RequestAsync<TRequest, TResponse>(string url, HttpMethod method, TRequest requestObj)
    {
        string jsonString = JsonConvert.SerializeObject(requestObj);
        StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        HttpRequestMessage request = new(method, url)
        {
            Content = content
        };

        return await RequestAsync<TResponse>(request);
    }

    private async Task<TResponse> RequestAsync<TResponse>(HttpRequestMessage request)
    {
        HttpResponseMessage response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"request fail with status code {response.StatusCode}");
        TResponse responseObj = JsonConvert.DeserializeObject<TResponse>(await response.Content.ReadAsStringAsync());
        return responseObj;
    }

    private async Task RequestAsync(string url, HttpMethod method)
    {
        HttpRequestMessage request = new(method, url);
        HttpResponseMessage response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"request fail with status code {response.StatusCode}");
    }
}
