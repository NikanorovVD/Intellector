public class TimeControlDto
{
    public int TotalSeconds { get; set; }
    public int AddedSeconds { get; set; }
    public TimeControlDto(int totalSeconds, int addedSeconds)
    {
        TotalSeconds = totalSeconds;
        AddedSeconds = addedSeconds;
    }
}
