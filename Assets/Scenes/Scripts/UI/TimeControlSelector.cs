using System.Collections.Generic;

public static class TimeControlSelector
{
    public static List<TimeControl> time_controls;
    static TimeControlSelector()
    {
        time_controls = new List<TimeControl>() { new(0, 0), new(1, 0), new(2, 1), new(3, 0), new(3, 2), new(5, 0), new(5, 3), new(10, 0), new(10, 5), new(15, 10), new(30, 0), new(30, 20) };
    }
}