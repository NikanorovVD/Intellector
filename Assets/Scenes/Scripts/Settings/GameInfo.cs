public class GameInfo
{
    private static GameInfo instance;

    public uint ID { get; set; }
    public string Name { get; set; }
    public TimeControl TimeControl { get; set; }
    public ColorChoice Color { get; set; }
    public bool Team { get; set; }

    public void Save()
    {
        instance = this;
    }

    public static GameInfo Load()
    {
        return instance ?? new();
    }
}
