public class TimeControl 
{
    private int totalMilliseconds;
    private int addedMilliseconds;

    public bool Active { get => totalMilliseconds != 0; }
    public int TotalMilliseconds { get => totalMilliseconds; }
    public int AddedMilliseconds { get => addedMilliseconds; }

    public int TotalMinutes { 
        get { return totalMilliseconds / 60000; }
        set { totalMilliseconds = value * 60000; }
    }
    public int AddedMinutes
    {
        get { return totalMilliseconds / 60000; }
        set { totalMilliseconds = value * 60000; }
    }

    public int TotalSeconds
    {
        get { return totalMilliseconds / 1000; }
        set { totalMilliseconds = value * 1000; }
    }
    public int AddedSeconds { 
        get { return addedMilliseconds / 1000; }
        set { addedMilliseconds = value * 1000;}
    }

    public TimeControl(int totalMinutes, int addedSeconds)
    {
        TotalMinutes = totalMinutes;
        AddedSeconds = addedSeconds;
    }
    public TimeControl()
    { }

    public override string ToString()
    {
        if (totalMilliseconds == 0) return "Unlimited";
        return $"{TotalMinutes} + {AddedSeconds}";
    }
}
