using System.Collections.Generic;

public class GameRecord
{
    public const string UnfinishedResult = "*";

    public string Event;
    public string Site;
    public string Date;
    public string UTCTime;
    public string White;
    public string Black;
    public string Result = UnfinishedResult;
    public string TimeControl;
    public string GameMode;
    public string AppVersion;
    public string Termination;
    public string SetUp;
    public string Ifen;
    public List<RecordedMove> Moves = new();
    public List<PositionAnalysis> Analysis;

    public bool IsFinished => Result != UnfinishedResult;
}

public class PositionAnalysis
{
    public double Mark;
    public int Depth;
    public RecordedMove BestMove;
    public bool Known;
}
