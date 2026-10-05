using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialPiece : MonoBehaviour, IPiece
{
    private readonly Piece piece;
    public MaterialPiece(Piece piece)
    {
        this.piece = piece;
    }
    public PieceType Type { get => piece.Type; }
    public int X
    {
        get => piece.X;
        set => piece.X = value;
    }
    public int Y
    {
        get => piece.Y;
        set => piece.Y = value;
    }
    public bool Team
    {
        get => piece.Team;
        set => piece.Team = value;
    }
    public IPiece[][] Board
    {
        get => piece.Board;
        set => piece.Board = value;
    }
    public bool HasIntellectorNearby()
    {
        return piece.HasIntellectorNearby();
    }
    public List<Vector2Int> GetAvailableMoves()
    {
        return piece.GetAvailableMoves();
    }
}
