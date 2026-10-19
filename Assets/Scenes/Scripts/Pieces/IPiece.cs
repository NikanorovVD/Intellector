using System.Collections.Generic;

using UnityEngine;

public interface IPiece
{
    PieceType Type { get;  }
    int X {  get; set; }
    int Y {  get; set; }
    Side Team {  get; set; }
    IPiece[][] Board {  get; set; }

    bool HasIntellectorNearby();
    List<Vector2Int> GetAvailableMoves();
}
