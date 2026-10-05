using System.Collections.Generic;

using UnityEngine;

public interface IPiece
{
    PieceType Type { get;  }
    int X {  get; set; }
    int Y {  get; set; }
    bool Team {  get; set; }
    IPiece[][] Board {  get; set; }

    bool HasIntellectorNearby();
    // FIXME: методы интерфейса всегда абстрактные
    // FIXME: возврат List<Vector2Int> - неправильно, должен быть List<Move> с полной информацией о ходе
    List<Vector2Int> GetAvailableMoves();
}
