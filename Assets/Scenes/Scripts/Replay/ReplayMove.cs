public class TileState
{
    public PieceType Type;
    public Side Team;
}

public class ReplayMove
{
    public UnityEngine.Vector2Int From;
    public UnityEngine.Vector2Int To;
    public TileState? FromBefore;
    public TileState? ToBefore;
    public TileState? FromAfter;
    public TileState? ToAfter;
    public string Notation;
}
