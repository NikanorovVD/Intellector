using System;
using System.Collections.Generic;
using System.Globalization;

public enum MoveJudgement
{
    None,
    Inaccuracy,
    Mistake,
    Blunder
}

public struct MoveQuality
{
    public double Accuracy;
    public MoveJudgement Judgement;
}

public struct GameMoveQuality
{
    public MoveQuality[] Moves;
    public double? WhiteAccuracy;
    public double? BlackAccuracy;
}

public static class MoveAccuracy
{
    public const double LogisticMultiplier = 0.00368208;
    public const double CentiprogressorCeiling = 1000.0;
    public const double AccuracyScale = 103.1668100711649;
    public const double AccuracyDecay = 0.04354415386753951;
    public const double AccuracyOffset = -3.166924740191411;
    public const double UncertaintyBonus = 1.0;
    public const double InaccuracyThreshold = 0.10;
    public const double MistakeThreshold = 0.20;
    public const double BlunderThreshold = 0.30;

    public static double ToCentiprogressors(double mark)
    {
        double mate = EngineTables.MarkOf(EngineFigure.WhiteIntellector);
        if (mark >= mate * 0.9) return CentiprogressorCeiling;
        if (mark <= -mate * 0.9) return -CentiprogressorCeiling;
        if (mark > CentiprogressorCeiling) return CentiprogressorCeiling;
        if (mark < -CentiprogressorCeiling) return -CentiprogressorCeiling;
        return mark;
    }

    public static double WinningChances(double mark)
    {
        double cp = ToCentiprogressors(mark);
        double value = 2.0 / (1.0 + Math.Exp(-LogisticMultiplier * cp)) - 1.0;
        if (value < -1.0) return -1.0;
        if (value > 1.0) return 1.0;
        return value;
    }

    public static double WinPercent(double mark) =>
        50.0 + 50.0 * WinningChances(mark);

    public static double AccuracyFromWinPercents(double before, double after)
    {
        if (after >= before) return 100.0;
        double winDiff = before - after;
        double raw = AccuracyScale * Math.Exp(-AccuracyDecay * winDiff) + AccuracyOffset;
        double value = raw + UncertaintyBonus;
        if (value > 100.0) return 100.0;
        if (value < 0.0) return 0.0;
        return value;
    }

    public static MoveJudgement JudgementFromDrop(double winningChancesDrop)
    {
        if (winningChancesDrop >= BlunderThreshold) return MoveJudgement.Blunder;
        if (winningChancesDrop >= MistakeThreshold) return MoveJudgement.Mistake;
        if (winningChancesDrop >= InaccuracyThreshold) return MoveJudgement.Inaccuracy;
        return MoveJudgement.None;
    }

    public static MoveQuality FromMarks(double beforeMark, double afterMark, bool blackMoved)
    {
        double beforeWin = WinPercent(beforeMark);
        double afterWin = WinPercent(afterMark);
        double beforeWc = WinningChances(beforeMark);
        double afterWc = WinningChances(afterMark);
        if (blackMoved)
        {
            beforeWin = 100.0 - beforeWin;
            afterWin = 100.0 - afterWin;
        }

        return new MoveQuality
        {
            Accuracy = AccuracyFromWinPercents(beforeWin, afterWin),
            Judgement = JudgementFromDrop(blackMoved ? afterWc - beforeWc : beforeWc - afterWc)
        };
    }

    public static double? GameAccuracy(IReadOnlyList<double> moveAccuracies)
    {
        if (moveAccuracies == null || moveAccuracies.Count == 0)
            return null;

        double sum = 0;
        double inv = 0;
        for (int i = 0; i < moveAccuracies.Count; i++)
        {
            double a = moveAccuracies[i];
            sum += a;
            if (a <= 1e-12)
                return 0;
            inv += 1.0 / a;
        }

        double arithmetic = sum / moveAccuracies.Count;
        double harmonic = moveAccuracies.Count / inv;
        return (arithmetic + harmonic) / 2.0;
    }

    public static GameMoveQuality FromPositionMarks(IReadOnlyList<double> marks, int firstPly)
    {
        if (marks == null || marks.Count < 2)
        {
            return new GameMoveQuality
            {
                Moves = Array.Empty<MoveQuality>()
            };
        }

        int moveCount = marks.Count - 1;
        var moves = new MoveQuality[moveCount];
        var white = new List<double>();
        var black = new List<double>();
        for (int i = 0; i < moveCount; i++)
        {
            bool blackMoved = (firstPly + i) % 2 == 1;
            MoveQuality quality = FromMarks(marks[i], marks[i + 1], blackMoved);
            moves[i] = quality;
            if (blackMoved)
                black.Add(quality.Accuracy);
            else
                white.Add(quality.Accuracy);
        }

        return new GameMoveQuality
        {
            Moves = moves,
            WhiteAccuracy = GameAccuracy(white),
            BlackAccuracy = GameAccuracy(black)
        };
    }

    public static string Glyph(MoveJudgement judgement)
    {
        return judgement switch
        {
            MoveJudgement.Inaccuracy => "?!",
            MoveJudgement.Mistake => "?",
            MoveJudgement.Blunder => "??",
            _ => string.Empty
        };
    }

    public static string FormatPercent(double value)
    {
        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded < 0) rounded = 0;
        if (rounded > 100) rounded = 100;
        return rounded.ToString(CultureInfo.InvariantCulture) + "%";
    }
}
