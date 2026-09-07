using System;
using UnityEngine;

public struct AISettings
{
    public const int DefaultDepth = 6;
    public const int DefaultLevel = 4;
    public const int DefaultSearchTimeMs = 3000;
    public const int MinLevel = 0;
    public const int MaxLevel = 10;

    public AISearchMode Mode;
    public int Depth;
    public int SearchTimeMs;
    public int Level;

    public static AISettings Default => new AISettings
    {
        Mode = AISearchMode.Depth,
        Depth = DefaultDepth,
        SearchTimeMs = DefaultSearchTimeMs,
        Level = DefaultLevel
    };

    public AISettings Clamped()
    {
        return new AISettings
        {
            Mode = Mode,
            Depth = Mathf.Max(1, Depth),
            SearchTimeMs = Mathf.Max(1, SearchTimeMs),
            Level = Mathf.Clamp(Level, MinLevel, MaxLevel)
        };
    }

    public string DisplayName => Mode switch
    {
        AISearchMode.Time => $"minmax<{FormatSearchTimeShort(SearchTimeMs)}>",
        AISearchMode.Level => $"minmax<lvl{Level}>",
        AISearchMode.Depth => $"minmax<{Depth}>",
        _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null)
    };

    private static string FormatSearchTimeShort(int milliseconds)
    {
        if (milliseconds % 1000 == 0)
            return $"{milliseconds / 1000}s";
        return $"{milliseconds / 1000f:0.###}s";
    }
}
