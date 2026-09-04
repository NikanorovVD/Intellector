using System;

public static class EngineUtils
{
    public static EngineColor GetColor(EngineFigure f) => (EngineColor)((int)f & 1);
    public static EngineFigure OppositeFigure(EngineFigure f) => (EngineFigure)((int)f ^ 1);
    public static bool IsIntellector(EngineFigure f) =>
        f == EngineFigure.WhiteIntellector || f == EngineFigure.BlackIntellector;
    public static bool IsProgressor(EngineFigure f) =>
        f == EngineFigure.WhiteProgressor || f == EngineFigure.BlackProgressor;
    public static bool IsProgressiveMove(EngineFigure fromFig, EngineFigure toFig) =>
        IsProgressor(fromFig) || toFig != EngineFigure.Empty;
    public static EngineFigure WithColor(EngineFigure whiteFigure, EngineColor color) =>
        (EngineFigure)((int)whiteFigure + (int)color);
    public static EngineColor Opposite(EngineColor color) => (EngineColor)((int)color ^ 1);

    private static readonly int[] ColumnStart = { 0, 7, 13, 20, 26, 33, 39, 46, 52 };
    private static readonly (int x, int y)[] UnityFromEngine = new (int x, int y)[59];

    static EngineUtils()
    {
        int i = 0;
        for (int x = 0; x < ColumnStart.Length; x++)
        {
            int height = x % 2 == 0 ? 7 : 6;
            for (int y = 0; y < height; y++)
                UnityFromEngine[i++] = (x, y);
        }
    }

    public static string IndexToTileName(int index)
    {
        if (index < 0 || index >= UnityFromEngine.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Индекс клетки вне диапазона 0..58.");
        var (x, y) = UnityFromEngine[index];
        return $"{(char)('a' + x)}{y + 1}";
    }

    public static EngineFigure CharToFigure(char letter)
    {
        switch (letter)
        {
            case EngineChars.WhiteProgressor: return EngineFigure.WhiteProgressor;
            case EngineChars.WhiteDominator: return EngineFigure.WhiteDominator;
            case EngineChars.WhiteLiberator: return EngineFigure.WhiteLiberator;
            case EngineChars.WhiteAgressor: return EngineFigure.WhiteAgressor;
            case EngineChars.WhiteDefensor: return EngineFigure.WhiteDefensor;
            case EngineChars.WhiteIntellector: return EngineFigure.WhiteIntellector;
            case EngineChars.BlackProgressor: return EngineFigure.BlackProgressor;
            case EngineChars.BlackDominator: return EngineFigure.BlackDominator;
            case EngineChars.BlackLiberator: return EngineFigure.BlackLiberator;
            case EngineChars.BlackAgressor: return EngineFigure.BlackAgressor;
            case EngineChars.BlackDefensor: return EngineFigure.BlackDefensor;
            case EngineChars.BlackIntellector: return EngineFigure.BlackIntellector;
            case EngineChars.Empty: return EngineFigure.Empty;
            default: throw new ArgumentOutOfRangeException(nameof(letter), letter, "Неизвестный символ фигуры.");
        }
    }

    public static char FigureToChar(EngineFigure figure)
    {
        switch (figure)
        {
            case EngineFigure.WhiteProgressor: return EngineChars.WhiteProgressor;
            case EngineFigure.WhiteDominator: return EngineChars.WhiteDominator;
            case EngineFigure.WhiteLiberator: return EngineChars.WhiteLiberator;
            case EngineFigure.WhiteAgressor: return EngineChars.WhiteAgressor;
            case EngineFigure.WhiteDefensor: return EngineChars.WhiteDefensor;
            case EngineFigure.WhiteIntellector: return EngineChars.WhiteIntellector;
            case EngineFigure.BlackProgressor: return EngineChars.BlackProgressor;
            case EngineFigure.BlackDominator: return EngineChars.BlackDominator;
            case EngineFigure.BlackLiberator: return EngineChars.BlackLiberator;
            case EngineFigure.BlackAgressor: return EngineChars.BlackAgressor;
            case EngineFigure.BlackDefensor: return EngineChars.BlackDefensor;
            case EngineFigure.BlackIntellector: return EngineChars.BlackIntellector;
            case EngineFigure.Empty: return EngineChars.Empty;
            default: throw new ArgumentOutOfRangeException(nameof(figure), figure, "Неизвестная фигура.");
        }
    }

    public static EngineColor CharToColor(char letter) =>
        (letter == EngineChars.WhiteToMove) ? EngineColor.White : EngineColor.Black;

    public static int GetEngineIndex(int x, int y) => ColumnStart[x] + y;

    public static (int x, int y) EngineIndexToUnity(int index) => UnityFromEngine[index];
}
