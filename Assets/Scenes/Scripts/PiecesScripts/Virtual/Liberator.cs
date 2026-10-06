using System.Collections.Generic;

using UnityEngine;

public class Liberator : Piece
{
    public override PieceType Type => PieceType.Liberator;
    public override List<Vector2Int> GetAvailableMoves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // Ближний круг
        for (int i = X - 1; i <= X + 1; i++)
        {
            // Левая граница
            if (i < 0) continue;
            // Правая граница
            if (i > 8) continue;

            for (int j = Y - 1; j <= Y + 1; j++)
            {
                // Нижняя граница
                if (j < 0) continue;
                // Верхняя граница
                if (j >= Board[i].Length) continue;

                // Клетка с фигурой
                if (X == i && Y == j) continue;
                // Две лишние клетки сверху
                if (X % 2 == 0 && Y + 1 == j && X != i) continue;
                // Две лишние клетки снизу
                if (X % 2 == 1 && Y - 1 == j && X != i) continue;

                // Есть фигура
                if (Board[i][j] != null) continue;

                result.Add(new Vector2Int(i, j));
            }
        }

        // Дальний круг
        // Вверх
        if(Y + 2 < Board[X].Length)
            if (Board[X][Y + 2] == null || Board[X][Y + 2].Team != Team)
                result.Add(new Vector2Int(X, Y + 2));
        // Вниз
        if (Y - 2 >= 0)
            if (Board[X][Y - 2] == null || Board[X][Y - 2].Team != Team)
                result.Add(new Vector2Int(X, Y - 2));

        // Вверх вправо
        if (Y + 1 < Board[X].Length && X + 2 <= 8)
            if (Board[X + 2][Y + 1] == null || Board[X + 2][Y + 1].Team != Team)
                result.Add(new Vector2Int(X + 2, Y + 1));
        // Вниз вправо
        if (Y - 1 >= 0 && X + 2 <= 8)
            if (Board[X + 2][Y - 1] == null || Board[X + 2][Y - 1].Team != Team)
                result.Add(new Vector2Int(X + 2, Y - 1));

        // Вверх влево
        if (Y + 1 < Board[X].Length && X - 2 >= 0)
            if (Board[X - 2][Y + 1] == null || Board[X - 2][Y + 1].Team != Team)
                result.Add(new Vector2Int(X - 2, Y + 1));
        // Вниз влево
        if (Y - 1 >= 0 && X - 2 >= 0)
            if (Board[X - 2][Y - 1] == null || Board[X - 2][Y - 1].Team != Team)
                result.Add(new Vector2Int(X - 2, Y - 1));

        return result;
    }
}
