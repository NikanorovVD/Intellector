public class TileState
{
    public PieceType Type;
    public bool Team;
}

public class ReplayMove
{
    public UnityEngine.Vector2Int From;
    public UnityEngine.Vector2Int To;
    public TileState? FromBefore;
    public TileState? ToBefore;
    public TileState? FromAfter;
    public TileState? ToAfter;
}
