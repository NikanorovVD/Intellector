using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialPiece : MonoBehaviour, IPiece
{
    private readonly Piece _piece;
    public MaterialPiece(Piece piece)
    {
        _piece = piece;
    }
    public PieceType Type { get => _piece.Type; }
    public int X
    {
        get => _piece.X;
        set => _piece.X = value;
    }
    public int Y
    {
        get => _piece.Y;
        set => _piece.Y = value;
    }
    public bool Team
    {
        get => _piece.Team;
        set => _piece.Team = value;
    }
    public IPiece[][] Board
    {
        get => _piece.Board;
        set => _piece.Board = value;
    }
    public bool HasIntellectorNearby()
    {
        return _piece.HasIntellectorNearby();
    }
    public List<Vector2Int> GetAvailableMooves()
    {
        return _piece.GetAvailableMooves();
    }
}

