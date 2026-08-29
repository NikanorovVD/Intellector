using System.Collections.Generic;
using UnityEngine;

public class Liberator : Piece
{
    public override PieceType Type => PieceType.liberator;
    public override List<Vector2Int> GetAvailableMooves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        //ближний круг
        for (int i = X - 1; i <= X + 1; i++)
        {
            if (i < 0) continue;                                                                        //левая граница
            if (i > 8) continue;                                                                        //правая граница

            for (int j = Y - 1; j <= Y + 1; j++)
            {
                if (j < 0) continue;                                                                    //нижняя граница
                if (j >= Board[i].Length) continue;                                              //верхняя граница

                if (X == i && Y == j) continue;                                                         //клетка с фигурой
                if (X % 2 == 0 && Y + 1 == j && X != i) continue;                                       //две лишние клетки сверху
                if (X % 2 == 1 && Y - 1 == j && X != i) continue;                                       //две лишние клетки снизу

                if (Board[i][j] != null) continue;                                               //есть фигура

                result.Add(new Vector2Int(i, j));
            }
        }

        //дальний круг
        if(Y + 2 < Board[X].Length)                                                              //вверх
            if (Board[X][Y + 2] == null || Board[X][Y + 2].Team != Team)
                result.Add(new Vector2Int(X, Y + 2));
        if (Y - 2 >= 0)                                                                                 //вниз
            if (Board[X][Y - 2] == null || Board[X][Y - 2].Team != Team)
                result.Add(new Vector2Int(X, Y - 2));

        if (Y + 1 < Board[X].Length && X + 2 <= 8)                                               //вверх вправо
            if (Board[X + 2][Y + 1] == null || Board[X + 2][Y + 1].Team != Team)
                result.Add(new Vector2Int(X + 2, Y + 1));
        if (Y - 1 >= 0 && X + 2 <= 8)                                                                   //вниз вправо
            if (Board[X + 2][Y - 1] == null || Board[X + 2][Y - 1].Team != Team)
                result.Add(new Vector2Int(X + 2, Y - 1));

        if (Y + 1 < Board[X].Length && X - 2 >= 0)                                               //вверх влево
            if (Board[X - 2][Y + 1] == null || Board[X - 2][Y + 1].Team != Team)
                result.Add(new Vector2Int(X - 2, Y + 1));
        if (Y - 1 >= 0 && X - 2 >= 0)                                                                   //вниз влево
            if (Board[X - 2][Y - 1] == null || Board[X - 2][Y - 1].Team != Team)
                result.Add(new Vector2Int(X - 2, Y - 1));

        return result;
    }
}
