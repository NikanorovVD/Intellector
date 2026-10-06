using System.Collections.Generic;

public class TimeControl
{
    public static readonly TimeControl Unlimited = new TimeControl(0, 0, true);

    public int BaseMilliseconds { get; }
    public int IncrementMilliseconds { get; }
    public bool Unlimited { get; }

    public TimeControl(int baseMilliseconds, int incrementMilliseconds)
        : this(baseMilliseconds, incrementMilliseconds, false)
    {
    }

    private TimeControl(int baseMilliseconds, int incrementMilliseconds, bool unlimited)
    {
        BaseMilliseconds = baseMilliseconds;
        IncrementMilliseconds = incrementMilliseconds;
        Unlimited = unlimited;
    }

    public override string ToString()
    {
        if (Unlimited) return "Unlimit";

        int minutes = BaseMilliseconds / 60000;
        int extraSeconds = (BaseMilliseconds % 60000) / 1000;
        int incrementSeconds = IncrementMilliseconds / 1000;
        if (extraSeconds == 0) return $"{minutes} + {incrementSeconds}";
        return $"{minutes}:{extraSeconds:D2} + {incrementSeconds}";
    }
}

public static class TimeControlSelector
{
    public static List<TimeControl> TimeControls;
    static TimeControlSelector()
    {
        const int MinuteMs = 60_000;
        const int SecondMs = 1_000;
        TimeControls = new List<TimeControl>()
        {
            TimeControl.Unlimited,
            new(1 * MinuteMs, 0),
            new(2 * MinuteMs, 1 * SecondMs),
            new(3 * MinuteMs, 0),
            new(3 * MinuteMs, 2 * SecondMs),
            new(5 * MinuteMs, 0),
            new(5 * MinuteMs, 3 * SecondMs),
            new(10 * MinuteMs, 0),
            new(10 * MinuteMs, 5 * SecondMs),
            new(15 * MinuteMs, 10 * SecondMs),
            new(30 * MinuteMs, 0),
            new(30 * MinuteMs, 20 * SecondMs)
        };
    }
}
