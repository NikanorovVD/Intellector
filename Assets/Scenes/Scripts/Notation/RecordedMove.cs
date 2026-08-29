using UnityEngine;

public class RecordedMove
{
    public PieceType Piece;
    public Vector2Int From;
    public Vector2Int To;
    public bool Capture;
    public bool Castling;
    public PieceType? Transformation;
}
