using System.Collections.Generic;
using UnityEngine;

public class Defensor : Piece
{
    public override PieceType Type => PieceType.defensor;
    public override List<Vector2Int> GetAvailableMooves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = X - 1; i <= X + 1; i++)
        {
            if (i < 0) continue;                                                                        //левая граница
            if (i > 8) continue;                                                                        //правая граница

            for (int j = Y - 1; j <= Y + 1; j++)
            {
                if (j < 0) continue;                                                                    //нижняя граница
                if (j >= Board[i].Length) continue;                                              //верхняя граница

                if (X == i && Y == j) continue;                                               //клетка с фигурой
                if (X % 2 == 0 && Y + 1 == j && X != i) continue;                        //две лишние клетки сверху
                if (X % 2 == 1 && Y - 1 == j && X != i) continue;                        //две лишние клетки снизу

                if (Board[i][j] != null && Board[i][j].Team == Team) continue;       //есть фигура и она союзная

                result.Add(new Vector2Int(i, j));
            }
        }

        return result;
    }
}
