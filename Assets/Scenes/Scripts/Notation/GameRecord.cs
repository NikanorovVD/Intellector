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
    public List<RecordedMove> Moves = new();

    public bool IsFinished => Result != UnfinishedResult;
}
