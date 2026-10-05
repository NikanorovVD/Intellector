using System;
using System.Collections.Generic;

public class TimeControl
{
    private int maxTime;
    private int addedTime;

    // FIXME: неочевидный флаг, что в игре есть контроль времени
    public bool Active { get => maxTime != 0; }
    public int MaxMilliseconds { get => maxTime; }
    public int AddMilliseconds { get => addedTime; }

    // FIXME: неудобные свойства, макс. время нельзя задать точнее минут
    public int MaxMinutes {
        get { return maxTime / 60000; }
        set { maxTime = value * 60000; }
    }
    public int AddedSeconds {
        get { return addedTime / 1000; }
        set { addedTime = value * 1000;}
    }
    public TimeControl(int minutes, int addSeconds)
    {
        MaxMinutes = minutes;
        AddedSeconds = addSeconds;
    }
    public override string ToString()
    {
        if (maxTime == 0) return "Unlimit";
        return $"{MaxMinutes} + {AddedSeconds}";
    }
}

public static class TimeControlSelector
{
    public static List<TimeControl> TimeControls;
    static TimeControlSelector()
    {
        TimeControls = new List<TimeControl>() { new(0, 0), new(1,0), new(2, 1), new(3, 0), new(3, 2), new(5, 0), new(5, 3), new(10, 0), new(10, 5), new(15, 10), new(30, 0), new(30, 20) };
    }
}
