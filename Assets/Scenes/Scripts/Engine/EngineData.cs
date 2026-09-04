using System;
using System.Collections.Generic;

public enum EngineFigure
{
    WhiteProgressor = 0,
    BlackProgressor = 1,
    WhiteDominator = 2,
    BlackDominator = 3,
    WhiteLiberator = 4,
    BlackLiberator = 5,
    WhiteAgressor = 6,
    BlackAgressor = 7,
    WhiteDefensor = 8,
    BlackDefensor = 9,
    WhiteIntellector = 10,
    BlackIntellector = 11,
    Empty = 12
}

public enum EngineColor
{
    White = 0,
    Black = 1
}

public enum MoveGenMode { All, CapturesOnly }

public static class EngineChars
{
    public const char WhiteProgressor = 'p';
    public const char BlackProgressor = 'P';
    public const char WhiteDominator = 'd';
    public const char BlackDominator = 'D';
    public const char WhiteLiberator = 'l';
    public const char BlackLiberator = 'L';
    public const char WhiteAgressor = 'a';
    public const char BlackAgressor = 'A';
    public const char WhiteDefensor = 'f';
    public const char BlackDefensor = 'F';
    public const char WhiteIntellector = 'i';
    public const char BlackIntellector = 'I';
    public const char Empty = '-';
    public const char WhiteToMove = 'w';
    public const char BlackToMove = 'b';
}

public struct EngineMove
{
    public int From { get; set; }
    public int To { get; set; }
    public EngineFigure Figure { get; set; }
    public EngineMove(int from, int to, EngineFigure figure) => (From, To, Figure) = (from, to, figure);
}

public struct MoveResult
{
    public EngineMove? Move { get; set; }
    public double Mark { get; set; }
    public int Depth { get; set; }
    public int Progress { get; set; }
    public List<EngineMove> BestLine { get; set; }
}

public struct HashRecord
{
    public EngineMove? Move { get; set; }
    public int Depth { get; set; }
    public double Mark { get; set; }
    public double Alpha { get; set; }
    public double Beta { get; set; }
}

public struct PruningEntry
{
    public int All { get; set; }
    public int Best { get; set; }
}

public struct MoveHistoryEntry
{
    public int From { get; set; }
    public int To { get; set; }
    public EngineFigure FromFigure { get; set; }
    public EngineFigure ToFigure { get; set; }
    public EngineFigure NewFigure { get; set; }
    public Dictionary<int, int> SavedPositionHistory { get; set; }
}

public static class EngineTables
{
    public static readonly double[] Marks = { 100, -100, 600, -600, 170, -170, 200, -200, 150, -150, 100000, -100000, 0 };
    public static double MarkOf(EngineFigure f) => Marks[(int)f];
    public static double PriceOf(EngineFigure f, int cell) => Price[(int)f][cell];

    public static readonly int[][][] MMoves = new int[][][]
    {
        /*a1*/new[]{new[]{1,2,3,4,5,6},new[]{7,14,21,28,35,42,49,56},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a2*/new[]{new[]{2,3,4,5,6},new[]{8,15,22,29,36,43,50,57},new[]{7,13},new[]{0},new int[]{},new int[]{}},
        /*a3*/new[]{new[]{3,4,5,6},new[]{9,16,23,30,37,44,51,58},new[]{8,14,20,26},new[]{1,0},new int[]{},new int[]{}},
        /*a4*/new[]{new[]{4,5,6},new[]{10,17,24,31,38,45},new[]{9,15,21,27,33,39},new[]{2,1,0},new int[]{},new int[]{}},
        /*a5*/new[]{new[]{5,6},new[]{11,18,25,32},new[]{10,16,22,28,34,40,46,52},new[]{3,2,1,0},new int[]{},new int[]{}},
        /*a6*/new[]{new[]{6},new[]{12,19},new[]{11,17,23,29,35,41,47,53},new[]{4,3,2,1,0},new int[]{},new int[]{}},
        /*a7*/new[]{new int[]{},new int[]{},new[]{12,18,24,30,36,42,48,54},new[]{5,4,3,2,1,0},new int[]{},new int[]{}},
        /*b1*/new[]{new[]{8,9,10,11,12},new[]{14,21,28,35,42,49,56},new[]{13},new int[]{},new[]{0},new[]{1}},
        /*b2*/new[]{new[]{9,10,11,12},new[]{15,22,29,36,43,50,57},new[]{14,20,26},new[]{7},new[]{1},new[]{2}},
        /*b3*/new[]{new[]{10,11,12},new[]{16,23,30,37,44,51,58},new[]{15,21,27,33,39},new[]{8,7},new[]{2},new[]{3}},
        /*b4*/new[]{new[]{11,12},new[]{17,24,31,38,45},new[]{16,22,28,34,40,46,52},new[]{9,8,7},new[]{3},new[]{4}},
        /*b5*/new[]{new[]{12},new[]{18,25,32},new[]{17,23,29,35,41,47,53},new[]{10,9,8,7},new[]{4},new[]{5}},
        /*b6*/new[]{new int[]{},new[]{19},new[]{18,24,30,36,42,48,54},new[]{11,10,9,8,7},new[]{5},new[]{6}},
        /*c1*/new[]{new[]{14,15,16,17,18,19},new[]{20,27,34,41,48,55},new int[]{},new int[]{},new int[]{},new[]{7,1}},
        /*c2*/new[]{new[]{15,16,17,18,19},new[]{21,28,35,42,49,56},new[]{20,26},new[]{13},new[]{7,0},new[]{8,2}},
        /*c3*/new[]{new[]{16,17,18,19},new[]{22,29,36,43,50,57},new[]{21,27,33,39},new[]{14,13},new[]{8,1},new[]{9,3}},
        /*c4*/new[]{new[]{17,18,19},new[]{23,30,37,44,51,58},new[]{22,28,34,40,46,52},new[]{15,14,13},new[]{9,2},new[]{10,4}},
        /*c5*/new[]{new[]{18,19},new[]{24,31,38,45},new[]{23,29,35,41,47,53},new[]{16,15,14,13},new[]{10,3},new[]{11,5}},
        /*c6*/new[]{new[]{19},new[]{25,32},new[]{24,30,36,42,48,54},new[]{17,16,15,14,13},new[]{11,4},new[]{12,6}},
        /*c7*/new[]{new int[]{},new int[]{},new[]{25,31,37,43,49,55},new[]{18,17,16,15,14,13},new[]{12,5},new int[]{}},
        /*d1*/new[]{new[]{21,22,23,24,25},new[]{27,34,41,48,55},new[]{26},new int[]{},new[]{13},new[]{14,8,2}},
        /*d2*/new[]{new[]{22,23,24,25},new[]{28,35,42,49,56},new[]{27,33,39},new[]{20},new[]{14,7,0},new[]{15,9,3}},
        /*d3*/new[]{new[]{23,24,25},new[]{29,36,43,50,57},new[]{28,34,40,46,52},new[]{21,20},new[]{15,8,1},new[]{16,10,4}},
        /*d4*/new[]{new[]{24,25},new[]{30,37,44,51,58},new[]{29,35,41,47,53},new[]{22,21,20},new[]{16,9,2},new[]{17,11,5}},
        /*d5*/new[]{new[]{25},new[]{31,38,45},new[]{30,36,42,48,54},new[]{23,22,21,20},new[]{17,10,3},new[]{18,12,6}},
        /*d6*/new[]{new int[]{},new[]{32},new[]{31,37,43,49,55},new[]{24,23,22,21,20},new[]{18,11,4},new[]{19}},
        /*e1*/new[]{new[]{27,28,29,30,31,32},new[]{33,40,47,54},new int[]{},new int[]{},new int[]{},new[]{20,14,8,2}},
        /*e2*/new[]{new[]{28,29,30,31,32},new[]{34,41,48,55},new[]{33,39},new[]{26},new[]{20,13},new[]{21,15,9,3}},
        /*e3*/new[]{new[]{29,30,31,32},new[]{35,42,49,56},new[]{34,40,46,52},new[]{27,26},new[]{21,14,7,0},new[]{22,16,10,4}},
        /*e4*/new[]{new[]{30,31,32},new[]{36,43,50,57},new[]{35,41,47,53},new[]{28,27,26},new[]{22,15,8,1},new[]{23,17,11,5}},
        /*e5*/new[]{new[]{31,32},new[]{37,44,51,58},new[]{36,42,48,54},new[]{29,28,27,26},new[]{23,16,9,2},new[]{24,18,12,6}},
        /*e6*/new[]{new[]{32},new[]{38,45},new[]{37,43,49,55},new[]{30,29,28,27,26},new[]{24,17,10,3},new[]{25,19}},
        /*e7*/new[]{new int[]{},new int[]{},new[]{38,44,50,56},new[]{31,30,29,28,27,26},new[]{25,18,11,4},new int[]{}},
        /*f1*/new[]{new[]{34,35,36,37,38},new[]{40,47,54},new[]{39},new int[]{},new[]{26},new[]{27,21,15,9,3}},
        /*f2*/new[]{new[]{35,36,37,38},new[]{41,48,55},new[]{40,46,52},new[]{33},new[]{27,20,13},new[]{28,22,16,10,4}},
        /*f3*/new[]{new[]{36,37,38},new[]{42,49,56},new[]{41,47,53},new[]{34,33},new[]{28,21,14,7,0},new[]{29,23,17,11,5}},
        /*f4*/new[]{new[]{37,38},new[]{43,50,57},new[]{42,48,54},new[]{35,34,33},new[]{29,22,15,8,1},new[]{30,24,18,12,6}},
        /*f5*/new[]{new[]{38},new[]{44,51,58},new[]{43,49,55},new[]{36,35,34,33},new[]{30,23,16,9,2},new[]{31,25,19}},
        /*f6*/new[]{new int[]{},new[]{45},new[]{44,50,56},new[]{37,36,35,34,33},new[]{31,24,17,10,3},new[]{32}},
        /*g1*/new[]{new[]{40,41,42,43,44,45},new[]{46,53},new int[]{},new int[]{},new int[]{},new[]{33,27,21,15,9,3}},
        /*g2*/new[]{new[]{41,42,43,44,45},new[]{47,54},new[]{46,52},new[]{39},new[]{33,26},new[]{34,28,22,16,10,4}},
        /*g3*/new[]{new[]{42,43,44,45},new[]{48,55},new[]{47,53},new[]{40,39},new[]{34,27,20,13},new[]{35,29,23,17,11,5}},
        /*g4*/new[]{new[]{43,44,45},new[]{49,56},new[]{48,54},new[]{41,40,39},new[]{35,28,21,14,7,0},new[]{36,30,24,18,12,6}},
        /*g5*/new[]{new[]{44,45},new[]{50,57},new[]{49,55},new[]{42,41,40,39},new[]{36,29,22,15,8,1},new[]{37,31,25,19}},
        /*g6*/new[]{new[]{45},new[]{51,58},new[]{50,56},new[]{43,42,41,40,39},new[]{37,30,23,16,9,2},new[]{38,32}},
        /*g7*/new[]{new int[]{},new int[]{},new[]{51,57},new[]{44,43,42,41,40,39},new[]{38,31,24,17,10,3},new int[]{}},
        /*h1*/new[]{new[]{47,48,49,50,51},new[]{53},new[]{52},new int[]{},new[]{39},new[]{40,34,28,22,16,10,4}},
        /*h2*/new[]{new[]{48,49,50,51},new[]{54},new[]{53},new[]{46},new[]{40,33,26},new[]{41,35,29,23,17,11,5}},
        /*h3*/new[]{new[]{49,50,51},new[]{55},new[]{54},new[]{47,46},new[]{41,34,27,20,13},new[]{42,36,30,24,18,12,6}},
        /*h4*/new[]{new[]{50,51},new[]{56},new[]{55},new[]{48,47,46},new[]{42,35,28,21,14,7,0},new[]{43,37,31,25,19}},
        /*h5*/new[]{new[]{51},new[]{57},new[]{56},new[]{49,48,47,46},new[]{43,36,29,22,15,8,1},new[]{44,38,32}},
        /*h6*/new[]{new int[]{},new[]{58},new[]{57},new[]{50,49,48,47,46},new[]{44,37,30,23,16,9,2},new[]{45}},
        /*i1*/new[]{new[]{53,54,55,56,57,58},new int[]{},new int[]{},new int[]{},new int[]{},new[]{46,40,34,28,22,16,10,4}},
        /*i2*/new[]{new[]{54,55,56,57,58},new int[]{},new int[]{},new[]{52},new[]{46,39},new[]{47,41,35,29,23,17,11,5}},
        /*i3*/new[]{new[]{55,56,57,58},new int[]{},new int[]{},new[]{53,52},new[]{47,40,33,26},new[]{48,42,36,30,24,18,12,6}},
        /*i4*/new[]{new[]{56,57,58},new int[]{},new int[]{},new[]{54,53,52},new[]{48,41,34,27,20,13},new[]{49,43,37,31,25,19}},
        /*i5*/new[]{new[]{57,58},new int[]{},new int[]{},new[]{55,54,53,52},new[]{49,42,35,28,21,14,7,0},new[]{50,44,38,32}},
        /*i6*/new[]{new[]{58},new int[]{},new int[]{},new[]{56,55,54,53,52},new[]{50,43,36,29,22,15,8,1},new[]{51,45}},
        /*i7*/new[]{new int[]{},new int[]{},new int[]{},new[]{57,56,55,54,53,52},new[]{51,44,37,30,23,16,9,2},new int[]{}},
    };

    public static readonly int[][][] AMoves = new int[][][]
    {
        /*a1*/new[]{new[]{8,16,24,32},new[]{13,26,39,52},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a2*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a4*/new[]{new[]{11,19},new[]{16,29,42,55},new[]{8,13},new int[]{},new int[]{},new int[]{}},
        /*a5*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*a7*/new[]{new int[]{},new[]{19,32,45,58},new[]{11,16,21,26},new int[]{},new int[]{},new int[]{}},
        /*b1*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*b2*/new[]{new[]{16,24,32},new[]{21,34,47},new[]{13},new[]{0},new int[]{},new[]{3}},
        /*b3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*b4*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*b5*/new[]{new[]{19},new[]{24,37,50},new[]{16,21,26},new[]{3},new int[]{},new[]{6}},
        /*b6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*c1*/new[]{new[]{21,29,37,45},new[]{26,39,52},new int[]{},new int[]{},new[]{0},new[]{8,3}},
        /*c2*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*c3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*c4*/new[]{new[]{24,32},new[]{29,42,55},new[]{21,26},new[]{8,0},new[]{3},new[]{11,6}},
        /*c5*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*c6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*c7*/new[]{new int[]{},new[]{32,45,58},new[]{24,29,34,39},new[]{11,3},new[]{6},new int[]{}},
        /*d1*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*d2*/new[]{new[]{29,37,45},new[]{34,47},new[]{26},new[]{13},new[]{8},new[]{16,11,6}},
        /*d3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*d4*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*d5*/new[]{new[]{32},new[]{37,50},new[]{29,34,39},new[]{16,8,0},new[]{11},new[]{19}},
        /*d6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*e1*/new[]{new[]{34,42,50,58},new[]{39,52},new int[]{},new int[]{},new[]{13,0},new[]{21,16,11,6}},
        /*e2*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*e3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*e4*/new[]{new[]{37,45},new[]{42,55},new[]{34,39},new[]{21,13},new[]{16,3},new[]{24,19}},
        /*e5*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*e6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*e7*/new[]{new int[]{},new[]{45,58},new[]{37,42,47,52},new[]{24,16,8,0},new[]{19,6},new int[]{}},
        /*f1*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*f2*/new[]{new[]{42,50,58},new[]{47},new[]{39},new[]{26},new[]{21,8},new[]{29,24,19}},
        /*f3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*f4*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*f5*/new[]{new[]{45},new[]{50},new[]{42,47,52},new[]{29,21,13},new[]{24,11},new[]{32}},
        /*f6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*g1*/new[]{new[]{47,55},new[]{52},new int[]{},new int[]{},new[]{26,13,0},new[]{34,29,24,19}},
        /*g2*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*g3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*g4*/new[]{new[]{50,58},new[]{55},new[]{47,52},new[]{34,26},new[]{29,16,3},new[]{37,32}},
        /*g5*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*g6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*g7*/new[]{new int[]{},new[]{58},new[]{50,55},new[]{37,29,21,13},new[]{32,19,6},new int[]{}},
        /*h1*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*h2*/new[]{new[]{55},new int[]{},new[]{52},new[]{39},new[]{34,21,8},new[]{42,37,32}},
        /*h3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*h4*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*h5*/new[]{new[]{58},new int[]{},new[]{55},new[]{42,34,26},new[]{37,24,11},new[]{45}},
        /*h6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*i1*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new[]{39,26,13,0},new[]{47,42,37,32}},
        /*i2*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*i3*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*i4*/new[]{new int[]{},new int[]{},new int[]{},new[]{47,39},new[]{42,29,16,3},new[]{50,45}},
        /*i5*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*i6*/new[]{new int[]{},new int[]{},new int[]{},new int[]{},new int[]{},new int[]{}},
        /*i7*/new[]{new int[]{},new int[]{},new int[]{},new[]{50,42,34,26},new[]{45,32,19,6},new int[]{}},
    };

    public static readonly int[][] LLongMoves = new int[][]
    {
        new[]{2,14},        new[]{3,15,13},       new[]{4,16,14,0},
        new[]{5,17,15,1},   new[]{6,18,16,2},     new[]{19,17,3},      new[]{18,4},
        new[]{9,21},        new[]{10,22,20},       new[]{11,23,21,7},
        new[]{12,24,22,8},  new[]{25,23,9},        new[]{24,10},
        new[]{15,27,1},     new[]{16,28,26,0,2},   new[]{17,29,27,13,1,3},
        new[]{18,30,28,14,2,4},new[]{31,29,15,3,5,19},new[]{32,30,16,4,6}, new[]{31,17,5},
        new[]{22,34,8},     new[]{23,35,33,7,9},   new[]{24,36,34,20,8,10},
        new[]{25,37,35,21,9,11},new[]{38,36,22,10,12},new[]{37,23,11},
        new[]{28,40,14},    new[]{29,41,39,13,15}, new[]{30,42,40,26,14,16},
        new[]{31,43,41,27,15,17},new[]{32,44,42,28,16,18},new[]{45,43,29,17,19},new[]{44,30,18},
        new[]{35,47,21},    new[]{36,48,46,20,22}, new[]{37,49,47,33,21,23},
        new[]{38,50,48,34,22,24},new[]{51,49,35,23,25},new[]{50,36,24},
        new[]{41,53,27},    new[]{42,54,52,26,28}, new[]{43,55,53,39,27,29},
        new[]{44,56,54,40,28,30},new[]{45,57,55,41,29,31},new[]{58,56,42,30,32},new[]{57,43,31},
        new[]{48,34},       new[]{49,33,35},        new[]{50,46,34,36},
        new[]{51,47,35,37}, new[]{48,36,38},        new[]{49,37},
        new[]{54,40},       new[]{55,39,41},        new[]{56,52,40,42},
        new[]{57,53,41,43}, new[]{58,54,42,44},     new[]{55,43,45},     new[]{56,44},
    };

    public static readonly int[][] LShortMoves = new int[][]
    {
        new[]{1,7},           new[]{2,8,7,0},         new[]{3,9,8,1},
        new[]{4,10,9,2},      new[]{5,11,10,3},       new[]{6,12,11,4},       new[]{12,5},
        new[]{8,14,13,0,1},   new[]{9,15,14,7,1,2},   new[]{10,16,15,8,2,3},
        new[]{11,17,16,9,3,4},new[]{12,18,17,10,4,5}, new[]{19,18,11,5,6},
        new[]{14,20,7},       new[]{15,21,20,13,7,8},  new[]{16,22,21,14,8,9},
        new[]{17,23,22,15,9,10},new[]{18,24,23,16,10,11},new[]{19,25,24,17,11,12},new[]{25,18,12},
        new[]{21,27,26,13,14},new[]{22,28,27,20,14,15},new[]{23,29,28,21,15,16},
        new[]{24,30,29,22,16,17},new[]{25,31,30,23,17,18},new[]{32,31,24,18,19},
        new[]{27,33,20},      new[]{28,34,33,26,20,21},new[]{29,35,34,27,21,22},
        new[]{30,36,35,28,22,23},new[]{31,37,36,29,23,24},new[]{32,38,37,30,24,25},new[]{38,31,25},
        new[]{34,40,39,26,27},new[]{35,41,40,33,27,28},new[]{36,42,41,34,28,29},
        new[]{37,43,42,35,29,30},new[]{38,44,43,36,30,31},new[]{45,44,37,31,32},
        new[]{40,46,33},      new[]{41,47,46,39,33,34},new[]{42,48,47,40,34,35},
        new[]{43,49,48,41,35,36},new[]{44,50,49,42,36,37},new[]{45,51,50,43,37,38},new[]{51,44,38},
        new[]{47,53,52,39,40},new[]{48,54,53,46,40,41},new[]{49,55,54,47,41,42},
        new[]{50,56,55,48,42,43},new[]{51,57,56,49,43,44},new[]{58,57,50,44,45},
        new[]{53,46},         new[]{54,52,46,47},       new[]{55,53,47,48},
        new[]{56,54,48,49},   new[]{57,55,49,50},       new[]{58,56,50,51},     new[]{57,51},
    };

    public static int[][] IMoves => LShortMoves;
    public static int[][] DMoves => LShortMoves;
    public static int[][] Near => LShortMoves;

    public static readonly int[][] PMoves_white = new int[][]
    {
        /*a1*/new int[]{},    /*a2*/new[]{2,8},     /*a3*/new[]{3,9},
        /*a4*/new[]{4,10},    /*a5*/new[]{5,11},    /*a6*/new[]{6,12},    /*a7*/new int[]{},
        /*b1*/new[]{1,8,14},  /*b2*/new[]{2,9,15},  /*b3*/new[]{3,10,16},
        /*b4*/new[]{4,11,17}, /*b5*/new[]{5,12,18}, /*b6*/new[]{6,19},
        /*c1*/new int[]{},    /*c2*/new[]{8,15,21}, /*c3*/new[]{9,16,22},
        /*c4*/new[]{10,17,23},/*c5*/new[]{11,18,24},/*c6*/new[]{12,19,25},/*c7*/new int[]{},
        /*d1*/new[]{14,21,27},/*d2*/new[]{15,22,28},/*d3*/new[]{16,23,29},
        /*d4*/new[]{17,24,30},/*d5*/new[]{18,25,31},/*d6*/new[]{19,32},
        /*e1*/new int[]{},    /*e2*/new[]{21,28,34},/*e3*/new[]{22,29,35},
        /*e4*/new[]{23,30,36},/*e5*/new[]{24,31,37},/*e6*/new[]{25,32,38},/*e7*/new int[]{},
        /*f1*/new[]{27,34,40},/*f2*/new[]{28,35,41},/*f3*/new[]{29,36,42},
        /*f4*/new[]{30,37,43},/*f5*/new[]{31,38,44},/*f6*/new[]{32,45},
        /*g1*/new int[]{},    /*g2*/new[]{34,41,47},/*g3*/new[]{35,42,48},
        /*g4*/new[]{36,43,49},/*g5*/new[]{37,44,50},/*g6*/new[]{38,45,51},/*g7*/new int[]{},
        /*h1*/new[]{40,47,53},/*h2*/new[]{41,48,54},/*h3*/new[]{42,49,55},
        /*h4*/new[]{43,50,56},/*h5*/new[]{44,51,57},/*h6*/new[]{45,58},
        /*i1*/new int[]{},    /*i2*/new[]{47,54},   /*i3*/new[]{48,55},
        /*i4*/new[]{49,56},   /*i5*/new[]{50,57},   /*i6*/new[]{51,58},   /*i7*/new int[]{},
    };

    public static readonly int[][] PMoves_black = new int[][]
    {
        /*a1*/new int[]{},    /*a2*/new[]{0,7},     /*a3*/new[]{1,8},
        /*a4*/new[]{2,9},     /*a5*/new[]{3,10},    /*a6*/new[]{4,11},    /*a7*/new int[]{},
        /*b1*/new[]{0,13},    /*b2*/new[]{1,7,14},  /*b3*/new[]{2,8,15},
        /*b4*/new[]{3,9,16},  /*b5*/new[]{4,10,17}, /*b6*/new[]{5,11,18},
        /*c1*/new int[]{},    /*c2*/new[]{7,13,20}, /*c3*/new[]{8,14,21},
        /*c4*/new[]{9,15,22}, /*c5*/new[]{10,16,23},/*c6*/new[]{11,17,24},/*c7*/new int[]{},
        /*d1*/new[]{13,26},   /*d2*/new[]{14,20,27},/*d3*/new[]{15,21,28},
        /*d4*/new[]{16,22,29},/*d5*/new[]{17,23,30},/*d6*/new[]{18,24,31},
        /*e1*/new int[]{},    /*e2*/new[]{20,26,33},/*e3*/new[]{21,27,34},
        /*e4*/new[]{22,28,35},/*e5*/new[]{23,29,36},/*e6*/new[]{24,30,37},/*e7*/new int[]{},
        /*f1*/new[]{26,39},   /*f2*/new[]{27,33,40},/*f3*/new[]{28,34,41},
        /*f4*/new[]{29,35,42},/*f5*/new[]{30,36,43},/*f6*/new[]{31,37,44},
        /*g1*/new int[]{},    /*g2*/new[]{33,39,46},/*g3*/new[]{34,40,47},
        /*g4*/new[]{35,41,48},/*g5*/new[]{36,42,49},/*g6*/new[]{37,43,50},/*g7*/new int[]{},
        /*h1*/new[]{39,52},   /*h2*/new[]{40,46,53},/*h3*/new[]{41,47,54},
        /*h4*/new[]{42,48,55},/*h5*/new[]{43,49,56},/*h6*/new[]{44,50,57},
        /*i1*/new int[]{},    /*i2*/new[]{46,52},   /*i3*/new[]{47,53},
        /*i4*/new[]{48,54},   /*i5*/new[]{49,55},   /*i6*/new[]{50,56},   /*i7*/new int[]{},
    };

    public static readonly int[] DMovesCount = {
        2,4,4,4,4,4,2, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5,
        3,6,6,6,6,6,3, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5, 2,4,4,4,4,4,2};
    public static readonly int[] IMovesCount = {
        2,4,4,4,4,4,2, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5,
        3,6,6,6,6,6,3, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5, 2,4,4,4,4,4,2};
    public static readonly int[] LShortMovesCount = {
        2,4,4,4,4,4,2, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5,
        3,6,6,6,6,6,3, 5,6,6,6,6,5, 3,6,6,6,6,6,3, 5,6,6,6,6,5, 2,4,4,4,4,4,2};
    public static readonly int[] LLongMovesCount = {
        2,3,4,4,4,3,2, 2,3,4,4,3,2, 3,5,6,6,6,5,3, 3,5,6,6,5,3,
        3,5,6,6,6,5,3, 3,5,6,6,5,3, 3,5,6,6,6,5,3, 2,3,4,4,3,2, 2,3,4,4,4,3,2};
    public static readonly int[] AMovesCount = {
        8,0,0,8,0,0,8, 0,9,0,0,9,0, 10,0,0,12,0,0,10, 0,11,0,0,11,0,
        12,0,0,12,0,0,12, 0,11,0,0,11,0, 10,0,0,12,0,0,10, 0,9,0,0,9,0, 8,0,0,8,0,0,8};
    public static readonly int[] PMovesCount_w = {
        0,2,2,2,2,2,0, 3,3,3,3,3,2, 0,3,3,3,3,3,0, 3,3,3,3,3,2,
        0,3,3,3,3,3,0, 3,3,3,3,3,2, 0,3,3,3,3,3,0, 3,3,3,3,3,2, 0,2,2,2,2,2,0};
    public static readonly int[] PMovesCount_b = {
        0,2,2,2,2,2,0, 2,3,3,3,3,3, 0,3,3,3,3,3,0, 2,3,3,3,3,3,
        0,3,3,3,3,3,0, 2,3,3,3,3,3, 0,3,3,3,3,3,0, 2,3,3,3,3,3, 0,2,2,2,2,2,0};
    public static readonly int[] MMovesCount = {
        14,16,18,18,18,16,14, 15,17,19,19,17,15, 14,18,20,22,20,18,14,
        15,19,21,21,19,15, 14,18,22,22,22,18,14, 15,19,21,21,19,15,
        14,18,20,22,20,18,14, 15,17,19,19,17,15, 14,16,18,18,18,16,14};
    public static readonly int[] DistanceToCenter = {
        5,4,4,4,4,4,5, 4,3,3,3,3,4, 4,3,2,2,2,3,4, 3,2,1,1,2,3,
        3,2,1,0,1,2,3, 3,2,1,1,2,3, 4,3,2,2,2,3,4, 4,3,3,3,3,4, 5,4,4,4,4,4,5};
    public static readonly int[] PPromotion_w = {
        0,0,20,50,100,150,0, 0,0,20,50,100,150, 0,0,20,50,100,150,0,
        0,0,20,50,100,150, 0,0,20,50,100,150,0, 0,0,20,50,100,150,
        0,0,20,50,100,150,0, 0,0,20,50,100,150, 0,0,20,50,100,150,0};
    public static readonly int[] PPromotion_b = {
        0,-150,-100,-50,-20,0,0, -150,-100,-50,-20,0,0, 0,-150,-100,-50,-20,0,0,
        -150,-100,-50,-20,0,0, 0,-150,-100,-50,-20,0,0, -150,-100,-50,-20,0,0,
        0,-150,-100,-50,-20,0,0, -150,-100,-50,-20,0,0, 0,-150,-100,-50,-20,0,0};
    public static readonly int[] IPromotion_w = {
        0,5,10,20,50,100,100000, 0,5,10,20,50,100, 0,5,10,20,50,100,100000,
        0,5,10,20,50,100, 0,5,10,20,50,100,100000, 0,5,10,20,50,100,
        0,5,10,20,50,100,100000, 0,5,10,20,50,100, 0,5,10,20,50,100,100000};
    public static readonly int[] IPromotion_b = {
        -100000,-100,-50,-20,-10,-5,0, -100,-50,-20,-10,-5,0,
        -100000,-100,-50,-20,-10,-5,0, -100,-50,-20,-10,-5,0,
        -100000,-100,-50,-20,-10,-5,0, -100,-50,-20,-10,-5,0,
        -100000,-100,-50,-20,-10,-5,0, -100,-50,-20,-10,-5,0,
        -100000,-100,-50,-20,-10,-5,0};

    public static readonly double[][] Price;

    static EngineTables()
    {
        const int kMovesCount = 5;
        const int kCenter = 5;

        Price = new double[13][];
        for (int fig = 0; fig <= (int)EngineFigure.Empty; fig++) Price[fig] = new double[59];

        for (int i = 0; i <= 58; i++)
        {
            Price[(int)EngineFigure.WhiteProgressor][i] = MarkOf(EngineFigure.WhiteProgressor) + PMovesCount_w[i] * kMovesCount + PPromotion_w[i];
            Price[(int)EngineFigure.BlackProgressor][i] = MarkOf(EngineFigure.BlackProgressor) - PMovesCount_b[i] * kMovesCount + PPromotion_b[i];
            Price[(int)EngineFigure.WhiteDominator][i] = MarkOf(EngineFigure.WhiteDominator) + MMovesCount[i] * kMovesCount + DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.BlackDominator][i] = MarkOf(EngineFigure.BlackDominator) - MMovesCount[i] * kMovesCount - DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.WhiteLiberator][i] = MarkOf(EngineFigure.WhiteLiberator) + (LShortMovesCount[i] / 2.0 + LLongMovesCount[i]) * kMovesCount + DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.BlackLiberator][i] = MarkOf(EngineFigure.BlackLiberator) - (LShortMovesCount[i] / 2.0 + LLongMovesCount[i]) * kMovesCount - DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.WhiteAgressor][i] = MarkOf(EngineFigure.WhiteAgressor) + AMovesCount[i] * kMovesCount;
            Price[(int)EngineFigure.BlackAgressor][i] = MarkOf(EngineFigure.BlackAgressor) - AMovesCount[i] * kMovesCount;
            Price[(int)EngineFigure.WhiteDefensor][i] = MarkOf(EngineFigure.WhiteDefensor) + DMovesCount[i] * kMovesCount + DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.BlackDefensor][i] = MarkOf(EngineFigure.BlackDefensor) - DMovesCount[i] * kMovesCount - DistanceToCenter[i] * kCenter;
            Price[(int)EngineFigure.WhiteIntellector][i] = MarkOf(EngineFigure.WhiteIntellector) + IMovesCount[i] * kMovesCount + IPromotion_w[i];
            Price[(int)EngineFigure.BlackIntellector][i] = MarkOf(EngineFigure.BlackIntellector) - IMovesCount[i] * kMovesCount + IPromotion_b[i];
            Price[(int)EngineFigure.Empty][i] = 0;
        }
    }
}
