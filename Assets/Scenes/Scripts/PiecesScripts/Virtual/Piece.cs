using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : IPiece
{
    public abstract PieceType Type { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Team { get; set; }
    public IPiece[][] Board { get; set; }

    public bool HasIntellectorNearby()
    {
        for (int i = X - 1; i <= X + 1; i++)
        {
            if (i < 0) continue;
            if (i > 8) continue;

            for (int j = Y - 1; j <= Y + 1; j++)
            {
                if (j < 0) continue;
                if (j >= Board[i].Length) continue;

                if (X == i && Y == j) continue;
                if (X % 2 == 0 && Y + 1 == j && X != i) continue;
                if (X % 2 == 1 && Y - 1 == j && X != i) continue;

                if (Board[i][j] != null && Board[i][j].Team == Team)
                    if (Board[i][j].Type == PieceType.intellector)
                        return true;
            }
        }

        return false;
    }

    abstract public List<Vector2Int> GetAvailableMooves();
}
