using System;
using System.Collections.Generic;

public class TimeContol 
{
    private int max_time;
    private int added_time;

    // FIXME: неочевидный флаг, что в игре есть контроль времени
    public bool Active { get => max_time != 0; }
    public int MaxMilliseconds { get => max_time; }
    public int AddMilliseconds { get => added_time; }

    // FIXME: неудобные свойства, макс. время нельзя задать точнее минут
    public int MaxMinutes { 
        get { return max_time / 60000; }
        set { max_time = value * 60000; }
    }
    public int AddedSeconds { 
        get { return added_time / 1000; }
        set { added_time = value * 1000;}
    }
    public TimeContol(int minutes, int add_seconds)
    {
        MaxMinutes = minutes;
        AddedSeconds = add_seconds;
    }
    public override string ToString()
    {
        if (max_time == 0) return "Unlimit";
        return $"{MaxMinutes} + {AddedSeconds}";
    }
}

public static class TimeControlSelector
{
    public static List<TimeContol> time_controls;
    static TimeControlSelector()
    {
        time_controls = new List<TimeContol>() { new(0, 0), new(1,0), new(2, 1), new(3, 0), new(3, 2), new(5, 0), new(5, 3), new(10, 0), new(10, 5), new(15, 10), new(30, 0), new(30, 20) };
    }
}