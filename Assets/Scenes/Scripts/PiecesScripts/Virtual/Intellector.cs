using System.Collections.Generic;

using UnityEngine;

public class Intellector : Piece
{
    public override PieceType Type => PieceType.Intellector;
    public override List<Vector2Int> GetAvailableMoves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

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
                if (Board[i][j] != null)
                {
                    // Не дефенсор своей команды
                    if (Board[i][j].Team != Team || Board[i][j].Type != PieceType.Defensor)
                        continue;
                }

                result.Add(new Vector2Int(i, j));
            }
        }

        return result;
    }
}
