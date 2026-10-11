public static class ArrangeSession
{
    public static bool Arranging { get; private set; }
    public static string Backup { get; private set; }
    public static GameMode Mode { get; private set; }

    private static bool hasEnter;
    private static GameMode enterMode;
    private static string enterBackup;

    private static bool hasReturn;
    private static GameMode returnMode;

    public static void Enter(GameMode mode, string backup)
    {
        hasEnter = true;
        enterMode = mode;
        enterBackup = backup;
    }

    public static bool TryTakeEnter()
    {
        if (!hasEnter)
            return false;

        hasEnter = false;
        Arranging = true;
        Mode = enterMode;
        Backup = enterBackup;
        enterBackup = null;
        return true;
    }

    public static void Clear()
    {
        Arranging = false;
        Backup = null;
        Mode = default;
    }

    public static void ReturnTo(GameMode mode)
    {
        hasReturn = true;
        returnMode = mode;
    }

    public static bool TryTakeReturn(out GameMode mode)
    {
        if (!hasReturn)
        {
            mode = default;
            return false;
        }

        hasReturn = false;
        mode = returnMode;
        return true;
    }
}
