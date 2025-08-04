public static class ServerRoutes 
{
    public const string GetOpenLobbies = "open";
    public const string Login = "Login";
    public const string CreateOpenLobby = "open";
    public static string JoinOpenLobby(int id) => $"join/open/{id}";
    public static string DeleteLobby(int id) => $"cancel/{id}";
    public static string UpdateLobby(int id) => $"update/{id}";
}
