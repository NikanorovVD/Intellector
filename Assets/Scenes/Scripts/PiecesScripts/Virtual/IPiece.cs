using System.Collections.Generic;
using UnityEngine;

public interface IPiece
{
    public PieceType Type { get;  }
    public int X {  get; set; }
    public int Y {  get; set; }
    public bool Team {  get; set; }
    public IPiece[][] Board {  get; set; }

    public bool HasIntellectorNearby();
    // FIXME: методы интерфейса всегда абстрактные
    // FIXME: возврат List<Vector2Int> - неправильно, должен быть List<Move> с полной информацией о ходе
    abstract public List<Vector2Int> GetAvailableMooves();
}
