using System;
using System.Collections.Generic;
using System.Diagnostics;

using T = EngineTables;
using U = EngineUtils;

public class Engine
{
    private int? whiteIntellectorSquare;
    private int? blackIntellectorSquare;
    private EngineFigure[] fields;
    private EngineColor sideToMove;

    private List<MoveHistoryEntry> moveHistory;
    private Dictionary<int, List<HashRecord>> hash;
    private double[][][] history;
    private PruningEntry[][][] pruningHistory;
    private static int[][] hashKeys;
    private static int blackMoveKey;
    private static bool hashReady;
    private Dictionary<int, int> positionHistory;
    private static readonly Dictionary<int, int> playedOccurrences = new Dictionary<int, int>();
    private const int MaxPositionRepeats = 3;
    private const double DrawByRepetitionMark = 0;

    private double variability = 0;
    private double[] variabilityArray = new double[0];
    private int currentLine = 0;
    private long finishMs = 0;
    private int countLimit = 0;
    private int count = 0;
    private MoveResult bestMoveInfo;

    private static readonly Random random = new Random();
    private const double HashWindowMargin = 1.0;
    private const double WinMarkThreshold = 0.9;
    private const double HistorySortScale = 1_000_000.0;
    private const int AspirationNarrow = 10;
    private const int AspirationWide = 20;

    // Информация о лучшем ходе, найденном на текущей глубине. Вызывается после каждой итерации IDS.
    public event Action<MoveResult> OnProgress;

    public Engine(string position = null)
    {
        positionHistory = new Dictionary<int, int>();
        moveHistory = new List<MoveHistoryEntry>();
        fields = new EngineFigure[59];

        InitializeHashKeys();

        if (position != null && TrySetPosition(position))
            return;

        // начальная расстановка
        for (int i = 0; i <= 58; i++) fields[i] = EngineFigure.Empty;

        fields[1] = fields[14] = fields[27] = fields[40] = fields[53] = EngineFigure.WhiteProgressor;
        fields[0] = fields[52] = EngineFigure.WhiteDominator;
        fields[7] = fields[46] = EngineFigure.WhiteLiberator;
        fields[13] = fields[39] = EngineFigure.WhiteAgressor;
        fields[20] = fields[33] = EngineFigure.WhiteDefensor;
        fields[26] = EngineFigure.WhiteIntellector;

        fields[5] = fields[18] = fields[31] = fields[44] = fields[57] = EngineFigure.BlackProgressor;
        fields[6] = fields[58] = EngineFigure.BlackDominator;
        fields[12] = fields[51] = EngineFigure.BlackLiberator;
        fields[19] = fields[45] = EngineFigure.BlackAgressor;
        fields[25] = fields[38] = EngineFigure.BlackDefensor;
        fields[32] = EngineFigure.BlackIntellector;

        sideToMove = EngineColor.White;
        whiteIntellectorSquare = 26;
        blackIntellectorSquare = 32;

        int h = Hash();
        positionHistory[h] = 1;
    }

    public void Load(EngineFigure[] board, EngineColor sideToMove)
    {
        if (board == null || board.Length != 59)
            throw new ArgumentException("Ожидается массив из 59 клеток.", nameof(board));

        Array.Copy(board, fields, 59);
        this.sideToMove = sideToMove;

        int white = Array.IndexOf(fields, EngineFigure.WhiteIntellector);
        whiteIntellectorSquare = white >= 0 ? white : (int?)null;
        int black = Array.IndexOf(fields, EngineFigure.BlackIntellector);
        blackIntellectorSquare = black >= 0 ? black : (int?)null;

        moveHistory.Clear();
        positionHistory = new Dictionary<int, int> { { Hash(), 1 } };
    }

    private static void InitializeHashKeys()
    {
        if (hashReady) return;

        var bytes = new byte[4];
        int NextKey()
        {
            random.NextBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        hashKeys = new int[59][];
        for (int i = 0; i <= 58; i++)
        {
            hashKeys[i] = new int[13];
            for (int j = 0; j <= 11; j++)
                hashKeys[i][j] = NextKey();
            hashKeys[i][(int)EngineFigure.Empty] = 0;
        }
        blackMoveKey = NextKey();
        hashReady = true;
    }

    private int Hash()
    {
        int h = 0;
        if (sideToMove == EngineColor.Black) h ^= blackMoveKey;
        for (int i = 0; i <= 58; i++)
        {
            EngineFigure fig = fields[i];
            if (fig != EngineFigure.Empty) h ^= hashKeys[i][(int)fig];
        }
        return h;
    }

    private int RecalculateHash(int parentHash, EngineMove move)
    {
        parentHash ^= blackMoveKey;

        EngineFigure fromFig = fields[move.From];
        EngineFigure toFig = fields[move.To];

        EngineFigure newFromFig = ((fromFig == EngineFigure.WhiteIntellector && toFig == EngineFigure.WhiteDefensor) || (fromFig == EngineFigure.BlackIntellector && toFig == EngineFigure.BlackDefensor))
                         ? toFig : EngineFigure.Empty;
        EngineFigure newToFig = move.Figure;

        if (fromFig != EngineFigure.Empty) parentHash ^= hashKeys[move.From][(int)fromFig];
        if (toFig != EngineFigure.Empty) parentHash ^= hashKeys[move.To][(int)toFig];
        if (newFromFig != EngineFigure.Empty) parentHash ^= hashKeys[move.From][(int)newFromFig];
        if (newToFig != EngineFigure.Empty) parentHash ^= hashKeys[move.To][(int)newToFig];

        return parentHash;
    }

    // запомнить, что позиция встречалась и вернуть количество повторений, нужно для учета троекратного повторения
    public int RememberPlayed(bool progressive)
    {
        if (progressive) playedOccurrences.Clear();
        int h = Hash();
        if (playedOccurrences.TryGetValue(h, out int n)) n++;
        else n = 1;
        playedOccurrences[h] = n;
        return n;
    }

    public static void ClearPlayedHistory() => playedOccurrences.Clear();

    private void AdoptPlayedHistoryForSearch()
    {
        positionHistory = new Dictionary<int, int>(playedOccurrences);
        int h = Hash();
        if (!positionHistory.ContainsKey(h))
            positionHistory[h] = 1;
    }

    private bool IsDrawByRepetition(int posVal)
    {
        return positionHistory.TryGetValue(posVal, out int rep) && rep >= MaxPositionRepeats;
    }

    private bool TrySetPosition(string pos)
    {
        if (pos.Length != 60) return false;
        int whiteIntellectorCount = 0, blackIntellectorCount = 0;

        for (int i = 0; i <= 58; i++)
        {
            fields[i] = U.CharToFigure(pos[i]);
            if (fields[i] == EngineFigure.WhiteIntellector) { whiteIntellectorSquare = i; whiteIntellectorCount++; }
            else if (fields[i] == EngineFigure.BlackIntellector) { blackIntellectorSquare = i; blackIntellectorCount++; }
        }

        if (whiteIntellectorCount == 0) whiteIntellectorSquare = null;
        if (blackIntellectorCount == 0) blackIntellectorSquare = null;
        if (whiteIntellectorCount > 1 || blackIntellectorCount > 1) return false;
        if (pos[59] != EngineChars.WhiteToMove && pos[59] != EngineChars.BlackToMove) return false;

        sideToMove = U.CharToColor(pos[59]);
        positionHistory = new Dictionary<int, int>();
        positionHistory[Hash()] = 1;
        return true;
    }

    private void MoveByRules(EngineMove move, int? hashVal)
    {
        EngineFigure fromFig = fields[move.From];
        EngineFigure toFig = fields[move.To];

        moveHistory.Add(new MoveHistoryEntry
        {
            From = move.From,
            To = move.To,
            FromFigure = fromFig,
            ToFigure = toFig,
            NewFigure = move.Figure
        });

        if (move.Figure == EngineFigure.WhiteIntellector) whiteIntellectorSquare = move.To;
        else if (move.Figure == EngineFigure.BlackIntellector) blackIntellectorSquare = move.To;

        if (toFig == EngineFigure.WhiteIntellector) whiteIntellectorSquare = null;
        else if (toFig == EngineFigure.BlackIntellector) blackIntellectorSquare = null;

        if ((fromFig == EngineFigure.WhiteIntellector && toFig == EngineFigure.WhiteDefensor) || (fromFig == EngineFigure.BlackIntellector && toFig == EngineFigure.BlackDefensor))
        {
            // рокировка
            fields[move.From] = toFig;
            fields[move.To] = fromFig;
        }
        else
        {
            fields[move.From] = EngineFigure.Empty;
            fields[move.To] = move.Figure;
        }

        sideToMove = U.Opposite(sideToMove);

        if (hashVal.HasValue)
        {
            int hv = hashVal.Value;
            int last = moveHistory.Count - 1;
            if (U.IsProgressiveMove(fromFig, toFig))
            {
                var entry = moveHistory[last];
                entry.SavedPositionHistory = positionHistory;
                moveHistory[last] = entry;
                positionHistory = new Dictionary<int, int> { { hv, 1 } };
            }
            else if (positionHistory.TryGetValue(hv, out int val))
                positionHistory[hv] = val + 1;
            else
                positionHistory[hv] = 1;
        }

    }

    private void UnmoveByRules(int? hashVal = null)
    {
        var m = moveHistory[moveHistory.Count - 1];
        moveHistory.RemoveAt(moveHistory.Count - 1);

        fields[m.From] = m.FromFigure;
        fields[m.To] = m.ToFigure;

        if (m.FromFigure == EngineFigure.WhiteIntellector) whiteIntellectorSquare = m.From;
        else if (m.FromFigure == EngineFigure.BlackIntellector) blackIntellectorSquare = m.From;

        if (m.ToFigure == EngineFigure.WhiteIntellector) whiteIntellectorSquare = m.To;
        else if (m.ToFigure == EngineFigure.BlackIntellector) blackIntellectorSquare = m.To;

        sideToMove = U.Opposite(sideToMove);

        if (hashVal.HasValue)
        {
            if (m.SavedPositionHistory != null)
                positionHistory = m.SavedPositionHistory;
            else
            {
                int hv = hashVal.Value;
                if (positionHistory.TryGetValue(hv, out int val))
                {
                    if (val == 1) positionHistory.Remove(hv);
                    else positionHistory[hv] = val - 1;
                }
            }
        }

    }

    private List<EngineMove> GenerateMoves(MoveGenMode mode)
    {
        var moves = new List<EngineMove>();
        bool all = mode == MoveGenMode.All;
        EngineColor color = sideToMove;

        void AddDominatorMoves(int coord)
        {
            EngineFigure figure = fields[coord];
            var ray = T.MMoves[coord];

            for (int dir = 0; dir < 6; dir++)
            {
                int[] line = ray[dir];
                int len = line.Length;
                if (len == 0) continue;

                int n = 0;
                if (all)
                {
                    while (n < len && fields[line[n]] == EngineFigure.Empty)
                        moves.Add(new EngineMove(coord, line[n++], figure));
                }
                else
                {
                    while (n < len && fields[line[n]] == EngineFigure.Empty) n++;
                }

                if (n < len)
                {
                    EngineFigure beatFig = fields[line[n]];
                    if (U.GetColor(beatFig) != color)
                    {
                        moves.Add(new EngineMove(coord, line[n], figure));
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, line[n], U.OppositeFigure(beatFig)));
                        }
                    }
                }
            }
        }

        void AddAgressorMoves(int coord)
        {
            EngineFigure figure = fields[coord];
            var ray = T.AMoves[coord];

            for (int dir = 0; dir < 6; dir++)
            {
                int[] line = ray[dir];
                int len = line.Length;
                if (len == 0) continue;

                int n = 0;
                if (all)
                {
                    while (n < len && fields[line[n]] == EngineFigure.Empty)
                        moves.Add(new EngineMove(coord, line[n++], figure));
                }
                else
                {
                    while (n < len && fields[line[n]] == EngineFigure.Empty) n++;
                }

                if (n < len)
                {
                    EngineFigure beatFig = fields[line[n]];
                    if (U.GetColor(beatFig) != color)
                    {
                        moves.Add(new EngineMove(coord, line[n], figure));
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, line[n], U.OppositeFigure(beatFig)));
                        }
                    }
                }
            }
        }

        void AddDefensorMoves(int coord)
        {
            EngineFigure figure = fields[coord];
            int[] targets = T.DMoves[coord];
            int len = targets.Length;

            if (all)
            {
                foreach (int field in targets)
                {
                    if (fields[field] == EngineFigure.Empty || U.GetColor(fields[field]) != color)
                        moves.Add(new EngineMove(coord, field, figure));

                    EngineFigure beatFig = fields[field];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, field, U.OppositeFigure(beatFig)));
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    EngineFigure beatFig = fields[targets[j]];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        moves.Add(new EngineMove(coord, targets[j], figure));
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, targets[j], U.OppositeFigure(beatFig)));
                        }
                    }
                }
            }
        }

        void AddWhiteProgressorMoves(int coord)
        {
            int[] targets = T.PMoves_white[coord];
            int len = targets.Length;

            if (all)
            {
                foreach (int field in targets)
                {
                    if (fields[field] == EngineFigure.Empty || U.GetColor(fields[field]) == EngineColor.Black)
                    {
                        if (field != 6 && field != 19 && field != 32 && field != 45 && field != 58)
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteProgressor));
                        else
                        {
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteDominator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteLiberator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteAgressor));
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteDefensor));
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    int field = targets[j];
                    EngineFigure beatFig = fields[field];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        if (field != 6 && field != 19 && field != 32 && field != 45 && field != 58)
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteProgressor));
                        else
                        {
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteDominator)); moves.Add(new EngineMove(coord, field, EngineFigure.WhiteLiberator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.WhiteAgressor)); moves.Add(new EngineMove(coord, field, EngineFigure.WhiteDefensor));
                        }
                    }
                }
            }
        }

        void AddBlackProgressorMoves(int coord)
        {
            int[] targets = T.PMoves_black[coord];
            int len = targets.Length;

            if (all)
            {
                foreach (int field in targets)
                {
                    if (fields[field] == EngineFigure.Empty || U.GetColor(fields[field]) == EngineColor.White)
                    {
                        if (field != 0 && field != 13 && field != 26 && field != 39 && field != 52)
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackProgressor));
                        else
                        {
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackDominator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackLiberator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackAgressor));
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackDefensor));
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    int field = targets[j];
                    EngineFigure beatFig = fields[field];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        if (field != 0 && field != 13 && field != 26 && field != 39 && field != 52)
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackProgressor));
                        else
                        {
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackDominator)); moves.Add(new EngineMove(coord, field, EngineFigure.BlackLiberator));
                            moves.Add(new EngineMove(coord, field, EngineFigure.BlackAgressor)); moves.Add(new EngineMove(coord, field, EngineFigure.BlackDefensor));
                        }
                    }
                }
            }
        }

        void AddLiberatorMoves(int coord)
        {
            EngineFigure figure = fields[coord];
            int[] targets = T.LLongMoves[coord];
            int len = targets.Length;

            if (all)
            {
                foreach (int field in targets)
                {
                    if (fields[field] == EngineFigure.Empty || U.GetColor(fields[field]) != color)
                        moves.Add(new EngineMove(coord, field, figure));

                    EngineFigure beatFig = fields[field];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, field, U.OppositeFigure(beatFig)));
                        }
                    }
                }

                foreach (int field in T.LShortMoves[coord])
                {
                    if (fields[field] == EngineFigure.Empty)
                        moves.Add(new EngineMove(coord, field, figure));
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    EngineFigure beatFig = fields[targets[j]];
                    if (beatFig != EngineFigure.Empty && U.GetColor(beatFig) != color)
                    {
                        moves.Add(new EngineMove(coord, targets[j], figure));
                        if (beatFig != U.OppositeFigure(figure) && !U.IsIntellector(beatFig))
                        {
                            int? intellectorSquare = (color == EngineColor.White) ? whiteIntellectorSquare : blackIntellectorSquare;
                            if (intellectorSquare.HasValue && Array.IndexOf(T.Near[coord], intellectorSquare.Value) >= 0)
                                moves.Add(new EngineMove(coord, targets[j], U.OppositeFigure(beatFig)));
                        }
                    }
                }
            }
        }

        void AddIntellectorMoves(int coord)
        {
            if (!all) return;

            EngineFigure figure = fields[coord];
            EngineColor figColor = U.GetColor(figure);

            foreach (int field in T.IMoves[coord])
            {
                if (fields[field] == EngineFigure.Empty)
                    moves.Add(new EngineMove(coord, field, figure));

                if ((figColor == EngineColor.White && fields[field] == EngineFigure.WhiteDefensor) ||
                    (figColor == EngineColor.Black && fields[field] == EngineFigure.BlackDefensor))
                    moves.Add(new EngineMove(coord, field, figure));
            }
        }

        for (int i = 0; i <= 58; i++)
        {
            EngineFigure fig = fields[i];
            if (fig == EngineFigure.Empty || U.GetColor(fig) != sideToMove) continue;

            switch (fig)
            {
                case EngineFigure.WhiteProgressor: AddWhiteProgressorMoves(i); break;
                case EngineFigure.BlackProgressor: AddBlackProgressorMoves(i); break;
                case EngineFigure.WhiteDominator:
                case EngineFigure.BlackDominator: AddDominatorMoves(i); break;
                case EngineFigure.WhiteAgressor:
                case EngineFigure.BlackAgressor: AddAgressorMoves(i); break;
                case EngineFigure.WhiteDefensor:
                case EngineFigure.BlackDefensor: AddDefensorMoves(i); break;
                case EngineFigure.WhiteLiberator:
                case EngineFigure.BlackLiberator: AddLiberatorMoves(i); break;
                case EngineFigure.WhiteIntellector:
                case EngineFigure.BlackIntellector:
                    if (all) AddIntellectorMoves(i);
                    break;
            }
        }

        return moves;
    }

    private List<EngineMove> GetMoves() => GenerateMoves(MoveGenMode.All);
    private List<EngineMove> GetBeatMoves() => GenerateMoves(MoveGenMode.CapturesOnly);

    private void SortBeatMoves(List<EngineMove> moves)
    {
        int len = moves.Count;
        double[] scores = new double[len];
        for (int i = 0; i < len; i++)
            scores[i] = -T.MarkOf(fields[moves[i].To]) - T.MarkOf(fields[moves[i].From]);
        SortMovesByScore(moves, scores);
    }

    private double Quiesce(double alpha, double beta)
    {
        const int fullDepth = 2;
        var beatfields = new List<int>();

        double Quiesce(int depth, double a, double b, double? mark)
        {
            double standPat = mark ?? FastMark();
            if (Math.Abs(standPat) > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector)) return standPat;

            if (sideToMove == EngineColor.White)
            {
                if (standPat >= b) return standPat;
                if (a < standPat) a = standPat;

                var moves = GetBeatMoves();
                SortBeatMoves(moves);
                int len = moves.Count;
                for (int i = 0; i < len; i++)
                {
                    var mv = moves[i];
                    if (depth >= fullDepth && !beatfields.Contains(mv.To) &&
                        !U.IsIntellector(fields[mv.To])) continue;

                    double newMark = RecalculateMark(standPat, mv);
                    MoveByRules(mv, null);
                    beatfields.Add(mv.To);
                    double res = Quiesce(depth + 1, a, b, newMark);
                    beatfields.RemoveAt(beatfields.Count - 1);
                    UnmoveByRules();

                    if (res >= b) return res;
                    if (res > a) a = res;
                }
                return a;
            }
            else
            {
                if (standPat <= a) return standPat;
                if (b > standPat) b = standPat;

                var moves = GetBeatMoves();
                SortBeatMoves(moves);
                int len = moves.Count;
                for (int i = 0; i < len; i++)
                {
                    var mv = moves[i];
                    if (depth >= fullDepth && !beatfields.Contains(mv.To) &&
                        !U.IsIntellector(fields[mv.To])) continue;

                    double newMark = RecalculateMark(standPat, mv);
                    MoveByRules(mv, null);
                    beatfields.Add(mv.To);
                    double res = Quiesce(depth + 1, a, b, newMark);
                    beatfields.RemoveAt(beatfields.Count - 1);
                    UnmoveByRules();

                    if (res <= a) return res;
                    if (res < b) b = res;
                }
                return b;
            }
        }

        return Quiesce(0, alpha, beta, null);
    }

    private double FastMark()
    {
        double mark = 0;
        for (int i = 0; i <= 58; i++)
        {
            EngineFigure fig = fields[i];
            if (fig != EngineFigure.Empty) mark += T.PriceOf(fig, i);
        }
        if (mark > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector)) mark = T.MarkOf(EngineFigure.WhiteIntellector);
        else if (mark < WinMarkThreshold * T.MarkOf(EngineFigure.BlackIntellector)) mark = T.MarkOf(EngineFigure.BlackIntellector);

        if (mark < T.MarkOf(EngineFigure.WhiteIntellector) * WinMarkThreshold && mark > T.MarkOf(EngineFigure.BlackIntellector) * WinMarkThreshold)
            mark += variabilityArray.Length > currentLine ? variabilityArray[currentLine] : 0;
        return mark;
    }

    private double RecalculateMark(double mark, EngineMove move)
    {
        mark -= T.PriceOf(fields[move.From], move.From);
        mark -= T.PriceOf(fields[move.To], move.To);
        mark += T.PriceOf(move.Figure, move.To);

        if (mark > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector)) mark = T.MarkOf(EngineFigure.WhiteIntellector);
        else if (mark < WinMarkThreshold * T.MarkOf(EngineFigure.BlackIntellector)) mark = T.MarkOf(EngineFigure.BlackIntellector);
        return mark;
    }

    private bool IsCheck()
    {
        if (whiteIntellectorSquare == null || blackIntellectorSquare == null) return false;
        int field = (sideToMove == EngineColor.White) ? whiteIntellectorSquare.Value : blackIntellectorSquare.Value;
        EngineColor opp = U.Opposite(sideToMove);

        foreach (int f in T.DMoves[field])
            if (fields[f] == U.WithColor(EngineFigure.WhiteDefensor, opp)) return true;

        foreach (int f in T.LLongMoves[field])
            if (fields[f] == U.WithColor(EngineFigure.WhiteLiberator, opp)) return true;

        if (opp == EngineColor.White)
            foreach (int f in T.PMoves_black[field])
                if (fields[f] == EngineFigure.WhiteProgressor) return true;

        if (opp == EngineColor.Black)
            foreach (int f in T.PMoves_white[field])
                if (fields[f] == EngineFigure.BlackProgressor) return true;

        for (int dir = 0; dir < 6; dir++)
        {
            int[] line = T.AMoves[field][dir];
            for (int i = 0; i < line.Length; i++)
            {
                EngineFigure fig = fields[line[i]];
                if (fig == U.WithColor(EngineFigure.WhiteAgressor, opp)) return true;
                if (fig != EngineFigure.Empty) break;
            }
        }

        for (int dir = 0; dir < 6; dir++)
        {
            int[] line = T.MMoves[field][dir];
            for (int i = 0; i < line.Length; i++)
            {
                EngineFigure fig = fields[line[i]];
                if (fig == U.WithColor(EngineFigure.WhiteDominator, opp)) return true;
                if (fig != EngineFigure.Empty) break;
            }
        }

        return false;
    }

    private bool IsBeat(EngineMove move)
    {
        return fields[move.To] != EngineFigure.Empty &&
               !U.IsIntellector(fields[move.From]);
    }

    private double? WinMark()
    {
        if (whiteIntellectorSquare == null) return T.MarkOf(EngineFigure.BlackIntellector);
        if (blackIntellectorSquare == null) return T.MarkOf(EngineFigure.WhiteIntellector);
        if (whiteIntellectorSquare == 6 || whiteIntellectorSquare == 19 || whiteIntellectorSquare == 32 || whiteIntellectorSquare == 45 || whiteIntellectorSquare == 58) return T.MarkOf(EngineFigure.WhiteIntellector);
        if (blackIntellectorSquare == 0 || blackIntellectorSquare == 13 || blackIntellectorSquare == 26 || blackIntellectorSquare == 39 || blackIntellectorSquare == 52) return T.MarkOf(EngineFigure.BlackIntellector);
        return null;
    }

    private static bool IsImmediateWin(double mark)
    {
        return mark >= T.MarkOf(EngineFigure.WhiteIntellector) - 1
            || mark <= T.MarkOf(EngineFigure.BlackIntellector) + 1;
    }

    private (bool hasResult, double mark, EngineMove? move, double newAlpha, double newBeta)
        SeeHash(int pos, double alpha, double beta, int depth)
    {
        if (!hash.TryGetValue(pos, out var records))
            return (false, 0, null, alpha, beta);

        EngineMove? bestMove = null;
        double hashA = double.NegativeInfinity;
        double hashB = double.PositiveInfinity;

        foreach (var rec in records)
        {
            if (rec.Depth < depth) continue;
            if (rec.Mark > rec.Alpha && rec.Mark < rec.Beta)
                return (true, rec.Mark, rec.Move, alpha, beta);
            if (rec.Mark >= rec.Beta && hashA < rec.Beta) { hashA = rec.Beta; bestMove = rec.Move; }
            if (rec.Mark <= rec.Alpha && hashB > rec.Alpha) { hashB = rec.Alpha; }
        }

        if (alpha >= hashB) return (true, alpha, null, alpha, beta);
        if (beta <= hashA) return (true, beta, null, alpha, beta);

        if (hashA <= hashB)
        {
            alpha = Math.Max(alpha, hashA - HashWindowMargin);
            beta = Math.Min(beta, hashB + HashWindowMargin);
        }

        return (false, 0, null, alpha, beta);
    }

    private void AddToHash(int pos, double alpha, double beta, EngineMove? move, double mark, int depth)
    {
        double trustA, trustB;
        if (mark <= alpha) { trustA = mark; trustB = double.PositiveInfinity; }
        else if (mark >= beta) { trustA = double.NegativeInfinity; trustB = mark; }
        else { trustA = double.NegativeInfinity; trustB = double.PositiveInfinity; }

        if (((trustA > T.MarkOf(EngineFigure.BlackIntellector)) || (trustB < T.MarkOf(EngineFigure.WhiteIntellector))) && (Math.Abs(mark) > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector)))
            return;

        if (!hash.TryGetValue(pos, out var records))
        {
            records = new List<HashRecord>();
            hash[pos] = records;
        }

        for (int i = records.Count - 1; i >= 0; i--)
        {
            var r = records[i];
            if (r.Depth <= depth && r.Alpha >= trustA && r.Beta <= trustB)
                records.RemoveAt(i);
        }

        records.Add(new HashRecord { Move = move, Depth = depth, Mark = mark, Alpha = trustA, Beta = trustB });
    }

    private void SortMoves(List<EngineMove> moves)
    {
        int parentHash = Hash();
        EngineColor side = this.sideToMove;
        int len = moves.Count;
        double[] scores = new double[len];

        for (int i = 0; i < len; i++)
        {
            var mv = moves[i];
            int depth2 = -1;
            int childHash = RecalculateHash(parentHash, mv);

            MoveByRules(mv, null);
            double? win = WinMark();
            if (win.HasValue)
            {
                scores[i] = win.Value;
            }
            else
            {
                if (hash.TryGetValue(childHash, out var recs))
                {
                    int rlen = recs.Count;
                    for (int j = 0; j < rlen; j++)
                    {
                        var rec = recs[j];
                        if (rec.Depth > depth2)
                        {
                            if (rec.Mark > rec.Alpha && rec.Mark < rec.Beta)
                                scores[i] = rec.Mark + rec.Depth * T.MarkOf(side == EngineColor.White ? EngineFigure.WhiteProgressor : EngineFigure.BlackProgressor);
                            else if (side == EngineColor.White && rec.Mark >= rec.Beta)
                                scores[i] = rec.Beta + rec.Depth * T.MarkOf(EngineFigure.WhiteProgressor);
                            else if (side == EngineColor.Black && rec.Mark <= rec.Alpha)
                                scores[i] = rec.Alpha + rec.Depth * T.MarkOf(EngineFigure.BlackProgressor);
                            depth2 = rec.Depth;
                        }
                    }
                }
            }
            UnmoveByRules();

            if (scores[i] == 0 && fields[mv.To] == EngineFigure.Empty)
            {
                double rec2 = history[mv.From][mv.To][(int)mv.Figure];
                scores[i] = (side == EngineColor.White) ? T.MarkOf(EngineFigure.BlackIntellector) : T.MarkOf(EngineFigure.WhiteIntellector);
                if (side == EngineColor.White) scores[i] += rec2 / HistorySortScale;
                else scores[i] -= rec2 / HistorySortScale;
            }

            if (scores[i] == 0)
            {
                double d = -T.MarkOf(fields[mv.To]) - T.MarkOf(fields[mv.From]);
                scores[i] = (side == EngineColor.White) ? T.MarkOf(EngineFigure.BlackIntellector) + d : T.MarkOf(EngineFigure.WhiteIntellector) + d;
            }
        }

        SortMovesByScore(moves, scores);
    }

    private void SortMovesByScore(List<EngineMove> moves, double[] scores)
    {
        int len = moves.Count;
        int[] order = new int[len];
        for (int i = 0; i < len; i++) order[i] = i;

        bool whiteMaximizes = sideToMove == EngineColor.White;
        Array.Sort(order, (a, b) =>
        {
            int cmp = scores[a].CompareTo(scores[b]);
            if (cmp != 0) return whiteMaximizes ? -cmp : cmp;
            return a.CompareTo(b);
        });

        var sorted = new EngineMove[len];
        for (int i = 0; i < len; i++) sorted[i] = moves[order[i]];
        moves.Clear();
        moves.AddRange(sorted);
    }

    private void InitializeHistory()
    {
        history = new double[59][][];
        for (int i = 0; i <= 58; i++)
        {
            history[i] = new double[59][];
            for (int j = 0; j <= 58; j++)
            {
                history[i][j] = new double[14];
            }
        }
    }

    private void InitializePruningHistory()
    {
        pruningHistory = new PruningEntry[59][][];
        for (int i = 0; i <= 58; i++)
        {
            pruningHistory[i] = new PruningEntry[59][];
            for (int j = 0; j <= 58; j++)
                pruningHistory[i][j] = new PruningEntry[14];
        }
    }

    private void InitializeVariability(int rootMoveCount)
    {
        variabilityArray = new double[rootMoveCount];
        for (int i = 0; i < rootMoveCount; i++)
            variabilityArray[i] = Math.Round((random.NextDouble() - 0.5) * 2.0 * variability);
    }

    private int GetMoveNumber(EngineMove move)
    {
        var moves = GetMoves();
        for (int i = 0; i < moves.Count; i++)
        {
            var m = moves[i];
            if (m.From == move.From && m.To == move.To && m.Figure == move.Figure)
                return i;
        }
        return -1;
    }

    private enum SearchAbortMode { None, Time, Count }

    private struct SearchContext
    {
        public SearchAbortMode AbortMode { get; set; }
        public bool CountNodes { get; set; }
        public bool TrackLine { get; set; }
        public bool UpdateHashBoundsAtRoot { get; set; }
        public bool ReportRootProgress { get; set; }
        public bool UseMoveIndexForVariability { get; set; }
    }

    private struct AlphaBetaResult
    {
        public EngineMove? Move;
        public double Mark;
        public List<EngineMove> Line;
    }

    private static readonly SearchContext DepthSearch = new()
    {
        AbortMode = SearchAbortMode.None, CountNodes = false, TrackLine = false,
        UpdateHashBoundsAtRoot = true, ReportRootProgress = false, UseMoveIndexForVariability = true
    };

    private static readonly SearchContext TimeSearch = new()
    {
        AbortMode = SearchAbortMode.Time, CountNodes = true, TrackLine = true,
        UpdateHashBoundsAtRoot = true, ReportRootProgress = true, UseMoveIndexForVariability = false
    };

    private static readonly SearchContext CountSearch = new()
    {
        AbortMode = SearchAbortMode.Count, CountNodes = true, TrackLine = false,
        UpdateHashBoundsAtRoot = false, ReportRootProgress = true, UseMoveIndexForVariability = false
    };

    private AlphaBetaResult AlphaBeta(double alpha, double beta,
        int depth, double extension, int maxDepth, int? pos, SearchContext ctx, bool firstDepth = false)
    {
        if (depth > 0 && maxDepth > 1)
        {
            if (ctx.AbortMode == SearchAbortMode.Time &&
                Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency > finishMs)
            {
                double abort = (sideToMove == EngineColor.White) ? double.PositiveInfinity : double.NegativeInfinity;
                return new AlphaBetaResult { Move = null, Mark = abort, Line = ctx.TrackLine ? new List<EngineMove>() : null };
            }
            if (ctx.AbortMode == SearchAbortMode.Count && countLimit - count <= 0)
            {
                double abort = (sideToMove == EngineColor.White) ? double.PositiveInfinity : double.NegativeInfinity;
                return new AlphaBetaResult { Move = null, Mark = abort, Line = null };
            }
        }

        const int sortDepth = 2;
        const int hashDepth = 2;
        const double drawMark = DrawByRepetitionMark;

        int posVal = pos ?? Hash();
        if (ctx.CountNodes) count++;

        double CorrectWinMark(double v)
        {
            if (v > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector)) return v - 1;
            if (v < WinMarkThreshold * T.MarkOf(EngineFigure.BlackIntellector)) return v + 1;
            return v;
        }

        double result = 0;
        EngineMove? bestMove = null;
        bool isActive = false;
        List<EngineMove> bestLine = ctx.TrackLine ? new List<EngineMove>() : null;
        double oldA = alpha, oldB = beta;

        while (extension >= 1) { depth++; extension--; }
        while (extension <= -1) { depth = Math.Max(0, depth - 1); extension++; }

        if (depth != maxDepth && IsDrawByRepetition(posVal))
            return new AlphaBetaResult { Move = null, Mark = drawMark, Line = ctx.TrackLine ? new List<EngineMove>() : null };

        if (depth >= hashDepth)
        {
            var h = SeeHash(posVal, alpha, beta, depth);
            if (h.hasResult)
                return new AlphaBetaResult { Move = h.move, Mark = h.mark, Line = ctx.TrackLine ? new List<EngineMove>() : null };
            if (ctx.UpdateHashBoundsAtRoot || !firstDepth)
            {
                alpha = h.newAlpha;
                beta = h.newBeta;
            }
        }

        if (alpha < WinMarkThreshold * T.MarkOf(EngineFigure.BlackIntellector))
            alpha = T.MarkOf(EngineFigure.BlackIntellector) + 1;
        if (beta > WinMarkThreshold * T.MarkOf(EngineFigure.WhiteIntellector))
            beta = T.MarkOf(EngineFigure.WhiteIntellector) - 1;

        if (depth == 1)
        {
            sideToMove = U.Opposite(sideToMove);
            bool isNull = false;
            double nullMark = 0;
            if (sideToMove == EngineColor.Black && beta < double.PositiveInfinity)
            {
                nullMark = AlphaBeta(beta - 1, beta, depth - 1, extension, maxDepth, null, ctx).Mark;
                if (nullMark >= beta) isNull = true;
            }
            else if (sideToMove == EngineColor.White && alpha > double.NegativeInfinity)
            {
                nullMark = AlphaBeta(alpha, alpha + 1, depth - 1, extension, maxDepth, null, ctx).Mark;
                if (nullMark <= alpha) isNull = true;
            }
            sideToMove = U.Opposite(sideToMove);
            if (isNull)
                return new AlphaBetaResult { Move = null, Mark = nullMark, Line = ctx.TrackLine ? new List<EngineMove>() : null };
        }

        double? win = WinMark();
        if (win.HasValue)
            return new AlphaBetaResult { Move = null, Mark = win.Value, Line = ctx.TrackLine ? new List<EngineMove>() : null };

        if (depth < 1)
        {
            result = Quiesce(alpha, beta);
        }
        else if (sideToMove == EngineColor.White)
        {
            double value = T.MarkOf(EngineFigure.BlackIntellector);
            var moves = GetMoves();
            if (depth >= sortDepth) SortMoves(moves);
            int len = moves.Count;

            for (int i = 0; i < len; i++)
            {
                if (firstDepth)
                    currentLine = ctx.UseMoveIndexForVariability ? i : GetMoveNumber(moves[i]);
                var mv = moves[i];
                double beat = IsBeat(mv) ? 0.5 : 0;

                int p = RecalculateHash(posVal, mv);
                int? argP = p;

                MoveByRules(mv, argP);
                double check = IsCheck() ? 0.5 : 0;
                double ext = Math.Max(beat, check);

                if (ext == 0)
                {
                    var ph = pruningHistory[mv.From][mv.To][(int)mv.Figure];
                    if (ph.All >= maxDepth * maxDepth * 2)
                    {
                        double pp = ph.Best / (double)ph.All;
                        ext = -1.0 / (100 * pp + 0.5);
                    }
                    if (depth >= sortDepth && hash.TryGetValue(p, out var recs))
                    {
                        int d2 = 0;
                        foreach (var r in recs) if (r.Depth > d2) d2 = r.Depth;
                        if (d2 >= 3) ext -= (double)i / len;
                    }
                }

                double res;
                if (ctx.TrackLine)
                {
                    AlphaBetaResult r;
                    List<EngineMove> line;
                    if (i >= 1 && beta - alpha > 1)
                    {
                        r = AlphaBeta(alpha, alpha + 1, depth - 1, extension + ext, maxDepth, p, ctx);
                        line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                        if (r.Mark >= alpha + 1 && r.Mark < beta)
                        {
                            r = AlphaBeta(r.Mark - 1, beta, depth - 1, extension + ext, maxDepth, p, ctx);
                            line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                        }
                    }
                    else
                    {
                        r = AlphaBeta(alpha, beta, depth - 1, extension + ext, maxDepth, p, ctx);
                        line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                    }
                    res = CorrectWinMark(r.Mark);
                    UnmoveByRules(argP);

                    if (value < Math.Max(value, res)) { value = Math.Max(value, res); bestLine = new List<EngineMove>(line); }
                }
                else
                {
                    if (i >= 1 && beta - alpha > 1)
                    {
                        res = AlphaBeta(alpha, alpha + 1, depth - 1, extension + ext, maxDepth, p, ctx).Mark;
                        if (res >= alpha + 1 && res < beta)
                            res = AlphaBeta(res - 1, beta, depth - 1, extension + ext, maxDepth, p, ctx).Mark;
                    }
                    else
                        res = AlphaBeta(alpha, beta, depth - 1, extension + ext, maxDepth, p, ctx).Mark;

                    res = CorrectWinMark(res);
                    UnmoveByRules(argP);

                    value = Math.Max(value, res);
                }

                if (beat == 0 && check == 0) pruningHistory[mv.From][mv.To][(int)mv.Figure].All++;

                if (value > alpha)
                {
                    alpha = value; bestMove = mv;
                    isActive = beat > 0 || check > 0;
                }
                if (value >= beta)
                {
                    if (beat == 0) history[mv.From][mv.To][(int)mv.Figure] += depth * depth;
                    break;
                }

                if (ctx.ReportRootProgress && bestMove.HasValue)
                {
                    if (ctx.AbortMode == SearchAbortMode.Time && depth == maxDepth)
                    {
                        long nowMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
                        if (nowMs < finishMs || maxDepth <= 1)
                        {
                            for (int j = 0; j <= i; j++)
                            {
                                if (!bestMoveInfo.Move.HasValue ||
                                    (moves[j].From == bestMoveInfo.Move.Value.From &&
                                     moves[j].To == bestMoveInfo.Move.Value.To &&
                                     moves[j].Figure == bestMoveInfo.Move.Value.Figure))
                                {
                                    bestMoveInfo.Move = bestMove;
                                    bestMoveInfo.Mark = value;
                                    bestMoveInfo.Depth = maxDepth;
                                    bestMoveInfo.Progress = i + 1;
                                    bestMoveInfo.BestLine = new List<EngineMove>(bestLine);
                                }
                            }
                        }
                    }
                    else if (ctx.AbortMode == SearchAbortMode.Count && firstDepth &&
                             (countLimit - count > 0 || maxDepth <= 1))
                    {
                        for (int j = 0; j <= i; j++)
                        {
                            if (!bestMoveInfo.Move.HasValue ||
                                (moves[j].From == bestMoveInfo.Move.Value.From &&
                                 moves[j].To == bestMoveInfo.Move.Value.To &&
                                 moves[j].Figure == bestMoveInfo.Move.Value.Figure))
                            {
                                bestMoveInfo.Move = bestMove;
                                bestMoveInfo.Mark = value;
                                bestMoveInfo.Depth = maxDepth;
                                bestMoveInfo.Progress = i + 1;
                            }
                        }
                    }
                }
            }
            result = value;
        }
        else
        {
            double value = T.MarkOf(EngineFigure.WhiteIntellector);
            var moves = GetMoves();
            if (depth >= sortDepth) SortMoves(moves);
            int len = moves.Count;

            for (int i = 0; i < len; i++)
            {
                if (firstDepth)
                    currentLine = ctx.UseMoveIndexForVariability ? i : GetMoveNumber(moves[i]);
                var mv = moves[i];
                double beat = IsBeat(mv) ? 0.5 : 0;

                int p = RecalculateHash(posVal, mv);
                int? argP = p;

                MoveByRules(mv, argP);
                double check = IsCheck() ? 0.5 : 0;
                double ext = Math.Max(beat, check);

                if (ext == 0)
                {
                    var ph = pruningHistory[mv.From][mv.To][(int)mv.Figure];
                    if (ph.All >= maxDepth * maxDepth * 2)
                    {
                        double pp = ph.Best / (double)ph.All;
                        ext = -1.0 / (100 * pp + 0.5);
                    }
                    if (depth >= sortDepth && hash.TryGetValue(p, out var recs))
                    {
                        int d2 = 0;
                        foreach (var r in recs) if (r.Depth > d2) d2 = r.Depth;
                        if (d2 >= 3) ext -= (double)i / len;
                    }
                }

                double res;
                if (ctx.TrackLine)
                {
                    AlphaBetaResult r;
                    List<EngineMove> line;
                    if (i >= 1 && beta - alpha > 1)
                    {
                        r = AlphaBeta(beta - 1, beta, depth - 1, extension + ext, maxDepth, p, ctx);
                        line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                        if (r.Mark > alpha && r.Mark <= beta - 1)
                        {
                            r = AlphaBeta(alpha, r.Mark + 1, depth - 1, extension + ext, maxDepth, p, ctx);
                            line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                        }
                    }
                    else
                    {
                        r = AlphaBeta(alpha, beta, depth - 1, extension + ext, maxDepth, p, ctx);
                        line = r.Line ?? new List<EngineMove>(); line.Add(mv);
                    }
                    res = CorrectWinMark(r.Mark);
                    UnmoveByRules(argP);

                    if (value > Math.Min(value, res)) { value = Math.Min(value, res); bestLine = new List<EngineMove>(line); }
                }
                else
                {
                    if (i >= 1 && beta - alpha > 1)
                    {
                        res = AlphaBeta(beta - 1, beta, depth - 1, extension + ext, maxDepth, p, ctx).Mark;
                        if (res > alpha && res <= beta - 1)
                            res = AlphaBeta(alpha, res + 1, depth - 1, extension + ext, maxDepth, p, ctx).Mark;
                    }
                    else
                        res = AlphaBeta(alpha, beta, depth - 1, extension + ext, maxDepth, p, ctx).Mark;

                    res = CorrectWinMark(res);
                    UnmoveByRules(argP);

                    value = Math.Min(value, res);
                }

                if (beat == 0 && check == 0) pruningHistory[mv.From][mv.To][(int)mv.Figure].All++;

                if (value < beta)
                {
                    beta = value; bestMove = mv;
                    isActive = beat > 0 || check > 0;
                }
                if (value <= alpha)
                {
                    if (beat == 0) history[mv.From][mv.To][(int)mv.Figure] += depth * depth;
                    break;
                }

                if (ctx.ReportRootProgress && bestMove.HasValue)
                {
                    if (ctx.AbortMode == SearchAbortMode.Time && depth == maxDepth)
                    {
                        long nowMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
                        if (nowMs < finishMs || maxDepth <= 1)
                        {
                            for (int j = 0; j <= i; j++)
                            {
                                if (!bestMoveInfo.Move.HasValue ||
                                    (moves[j].From == bestMoveInfo.Move.Value.From &&
                                     moves[j].To == bestMoveInfo.Move.Value.To &&
                                     moves[j].Figure == bestMoveInfo.Move.Value.Figure))
                                {
                                    bestMoveInfo.Move = bestMove;
                                    bestMoveInfo.Mark = value;
                                    bestMoveInfo.Depth = maxDepth;
                                    bestMoveInfo.Progress = i + 1;
                                    bestMoveInfo.BestLine = new List<EngineMove>(bestLine);
                                }
                            }
                        }
                    }
                    else if (ctx.AbortMode == SearchAbortMode.Count && firstDepth &&
                             (countLimit - count > 0 || maxDepth <= 1))
                    {
                        for (int j = 0; j <= i; j++)
                        {
                            if (!bestMoveInfo.Move.HasValue ||
                                (moves[j].From == bestMoveInfo.Move.Value.From &&
                                 moves[j].To == bestMoveInfo.Move.Value.To &&
                                 moves[j].Figure == bestMoveInfo.Move.Value.Figure))
                            {
                                bestMoveInfo.Move = bestMove;
                                bestMoveInfo.Mark = value;
                                bestMoveInfo.Depth = maxDepth;
                                bestMoveInfo.Progress = i + 1;
                            }
                        }
                    }
                }
            }
            result = value;
        }

        if (bestMove.HasValue && !isActive)
            pruningHistory[bestMove.Value.From][bestMove.Value.To][(int)bestMove.Value.Figure].Best++;

        if (depth >= hashDepth && result != drawMark)
            AddToHash(posVal, oldA, oldB, bestMove, result, depth);

        return new AlphaBetaResult { Move = bestMove, Mark = result, Line = bestLine };
    }

    private (EngineMove? move, double mark) AB(double alpha, double beta,
        int depth, double extension, int maxDepth, int? pos, bool firstDepth = false)
    {
        var r = AlphaBeta(alpha, beta, depth, extension, maxDepth, pos, DepthSearch, firstDepth);
        return (r.Move, r.Mark);
    }

    private (EngineMove? move, double mark, List<EngineMove> line) ABByTime(
        double alpha, double beta,
        int depth, double extension, int maxDepth, int? pos, bool firstDepth = false)
    {
        var r = AlphaBeta(alpha, beta, depth, extension, maxDepth, pos, TimeSearch, firstDepth);
        return (r.Move, r.Mark, r.Line ?? new List<EngineMove>());
    }

    private (EngineMove? move, double mark) ABByCount(
        double alpha, double beta,
        int depth, double extension, int maxDepth, int? pos, bool firstDepth = false)
    {
        var r = AlphaBeta(alpha, beta, depth, extension, maxDepth, pos, CountSearch, firstDepth);
        return (r.Move, r.Mark);
    }

    private (double a, double b) AspirationWindow(int depth, double hypothesis, double point)
    {
        EngineColor lastSide = (depth % 2 == 0) ? U.Opposite(sideToMove) : sideToMove;
        if (lastSide == EngineColor.White)
            return (hypothesis - AspirationNarrow * point, hypothesis + AspirationWide * point);
        return (hypothesis - AspirationWide * point, hypothesis + AspirationNarrow * point);
    }

    private void RunAspirationSearch(
        int startDepth,
        Func<int, bool> shouldContinue,
        Func<int, double, double, int, bool, (EngineMove? move, double mark, List<EngineMove> line)> searchAtDepth,
        Action<int, EngineMove?, double, List<EngineMove>> onComplete,
        bool initialFirstDepth = true,
        bool fullSearchOnNullMove = true)
    {
        double point = T.MarkOf(EngineFigure.WhiteProgressor) / 100.0;
        double hypothesis = 0;
        int i = startDepth;

        while (shouldContinue(i))
        {
            var (a, b) = AspirationWindow(i, hypothesis, point);
            var res = searchAtDepth(i, a, b, 0, initialFirstDepth);

            if (res.mark <= a)
                res = searchAtDepth(i, double.NegativeInfinity, a + 1, 0, true);
            else if (res.mark >= b)
                res = searchAtDepth(i, b - 1, double.PositiveInfinity, 0, true);

            hypothesis = res.mark;

            if (fullSearchOnNullMove && !res.move.HasValue)
                res = searchAtDepth(i, double.NegativeInfinity, double.PositiveInfinity, 0, true);

            onComplete(i, res.move, res.mark, res.line);
            if (res.move.HasValue && IsImmediateWin(res.mark))
                break;
            i++;
        }
    }

    private (EngineMove? move, double mark) IDS(int depth)
    {
        int pos = Hash();
        (EngineMove? move, double mark) res = (null, 0);

        RunAspirationSearch(
            startDepth: 0,
            shouldContinue: i => i <= depth,
            searchAtDepth: (i, a, b, _, firstDepth) =>
            {
                var r = AB(a, b, i, 0, i, pos, firstDepth);
                return (r.move, r.mark, null);
            },
            onComplete: (i, move, mark, _) =>
            {
                res = (move, mark);
                bestMoveInfo.Move = move;
                bestMoveInfo.Mark = mark;
                bestMoveInfo.Depth = i;
                bestMoveInfo.Progress = GetMoves().Count;
                bestMoveInfo.BestLine = null;
                OnProgress?.Invoke(bestMoveInfo);
            },
            fullSearchOnNullMove: false,
            initialFirstDepth: false);

        if (!res.move.HasValue)
        {
            res = AB(double.NegativeInfinity, double.PositiveInfinity, depth, 0, depth, pos);
            bestMoveInfo.Move = res.move;
            bestMoveInfo.Mark = res.mark;
            bestMoveInfo.Depth = depth;
            bestMoveInfo.Progress = GetMoves().Count;
            bestMoveInfo.BestLine = null;
            OnProgress?.Invoke(bestMoveInfo);
        }

        return res;
    }

    // Поиск на заданную глубину.
    public MoveResult BestMoveByDepth(int depth)
    {
        AdoptPlayedHistoryForSearch();
        hash = new Dictionary<int, List<HashRecord>>();
        InitializeHistory();
        InitializePruningHistory();
        variability = 0;
        InitializeVariability(GetMoves().Count);

        IDS(depth);
        if (bestMoveInfo.Move.HasValue)
        {
            var mv = bestMoveInfo.Move.Value;
            UnityEngine.Debug.Log($"{U.FigureToChar(mv.Figure)} {U.IndexToTileName(mv.From)} {U.IndexToTileName(mv.To)} {bestMoveInfo.Mark}");
        }
        return bestMoveInfo;
    }

    // Поиск на заданное время (мс).
    public MoveResult BestMoveByTime(double timeMs)
    {
        AdoptPlayedHistoryForSearch();
        InitializeVariability(GetMoves().Count);
        hash = new Dictionary<int, List<HashRecord>>();
        InitializeHistory();
        InitializePruningHistory();
        int pos = Hash();

        finishMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency + (long)timeMs;

        RunAspirationSearch(
            startDepth: 1,
            shouldContinue: _ => Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency < finishMs,
            searchAtDepth: (i, a, b, _, firstDepth) => ABByTime(a, b, i, 0, i, pos, firstDepth),
            onComplete: (i, move, mark, line) =>
            {
                long nowMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
                if (nowMs < finishMs || i == 1)
                {
                    bestMoveInfo.Move = move;
                    bestMoveInfo.Mark = mark;
                    bestMoveInfo.Depth = i;
                    bestMoveInfo.Progress = GetMoves().Count;
                    bestMoveInfo.BestLine = line;
                }

                OnProgress?.Invoke(bestMoveInfo);

                if (bestMoveInfo.Move.HasValue)
                {
                    var mv = bestMoveInfo.Move.Value;
                    UnityEngine.Debug.Log(
                        $"{i}) {bestMoveInfo.Progress}/{GetMoves().Count} " +
                        $"{U.FigureToChar(mv.Figure)} {U.IndexToTileName(mv.From)} {U.IndexToTileName(mv.To)} {bestMoveInfo.Mark}");
                    UnityEngine.Debug.Log($"Позиций: {count}");
                }
            });

        return bestMoveInfo;
    }

    // Поиск до лимита позиций.
    private MoveResult BestMoveByCount(int limit)
    {
        AdoptPlayedHistoryForSearch();
        InitializeVariability(GetMoves().Count);
        hash = new Dictionary<int, List<HashRecord>>();
        InitializeHistory();
        InitializePruningHistory();
        int pos = Hash();

        countLimit = limit;
        count = 0;

        RunAspirationSearch(
            startDepth: 1,
            shouldContinue: _ => countLimit - count > 0,
            searchAtDepth: (i, a, b, _, firstDepth) =>
            {
                var r = ABByCount(a, b, i, 0, i, pos, firstDepth);
                return (r.move, r.mark, null);
            },
            onComplete: (i, move, mark, _) =>
            {
                if (countLimit - count > 0 || i == 1)
                {
                    bestMoveInfo.Move = move;
                    bestMoveInfo.Mark = mark;
                    bestMoveInfo.Depth = i;
                    bestMoveInfo.Progress = GetMoves().Count;
                }

                if (bestMoveInfo.Move.HasValue)
                {
                    var mv = bestMoveInfo.Move.Value;
                    UnityEngine.Debug.Log(
                        $"{i}) {bestMoveInfo.Progress}/{GetMoves().Count} " +
                        $"{U.FigureToChar(mv.Figure)} {U.IndexToTileName(mv.From)} {U.IndexToTileName(mv.To)} {bestMoveInfo.Mark}");
                }
            });

        return bestMoveInfo;
    }

    public MoveResult BestMoveByLevel(int level)
    {
        int limit;
        switch (level)
        {
            case 0: limit = 3_000_000; variability = 500; break;
            case 1: limit = 10_000; variability = 250; break;
            case 2: limit = 30_000; variability = 100; break;
            case 3: limit = 100_000; variability = 50; break;
            case 4: limit = 300_000; variability = 25; break;
            case 5: limit = 1_000_000; variability = 20; break;
            case 6: limit = 3_000_000; variability = 15; break;
            case 7: limit = 10_000_000; variability = 15; break;
            case 8: limit = 30_000_000; variability = 10; break;
            case 9: limit = 70_000_000; variability = 10; break;
            case 10: limit = 150_000_000; variability = 8; break;
            default: limit = 10_000; variability = 250; break;
        }
        return BestMoveByCount(limit);
    }
}
