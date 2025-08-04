public class GameSettings
{
    private static GameSettings _instance;
    public int? GameId { get;  set; }
    public TimeControl TimeControl { get; set; }
    public bool Team { get; set; }

    public void Save()
    {
        _instance = this;
    }

    public static GameSettings Load()
    {
        return _instance ?? new();
    }
}
