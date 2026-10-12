using System.Collections.Generic;

using UnityEngine;

public class Dominator : Piece
{
    public override PieceType Type => PieceType.Dominator;
    public override List<Vector2Int> GetAvailableMoves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // Ходы вверх
        for (int j = Y + 1; j < Board[X].Length; j++)
        {
            // Есть фигура, и она союзная
            if (Board[X][j] != null && Board[X][j].Team == Team) break;
            result.Add(new Vector2Int(X, j));
            // Есть фигура, и она вражеская
            if (Board[X][j] != null && Board[X][j].Team != Team) break;
        }

        // Ходы вниз
        for (int j = Y - 1; j >= 0; j--)
        {
            // Есть фигура, и она союзная
            if (Board[X][j] != null && Board[X][j].Team == Team) break;
            result.Add(new Vector2Int(X, j));
            // Есть фигура, и она вражеская
            if (Board[X][j] != null && Board[X][j].Team != Team) break;
        }

        // Ходы по диагонали вверх вправо
        for (int i = X + 1, j = Y; i <= 8; i++)
        {
            if (i % 2 == 0) j++;
            // Верхняя граница
            if (j >= Board[i].Length) break;

            // Есть фигура, и она союзная
            if (Board[i][j] != null && Board[i][j].Team == Team) break;
            result.Add(new Vector2Int(i, j));
            // Есть фигура, и она вражеская
            if (Board[i][j] != null && Board[i][j].Team != Team) break;
        }

        // Ходы по диагонали вниз вправо
        for (int i = X + 1, j = Y; i <= 8; i++)
        {
            if (i % 2 == 1) j--;
            // Нижняя граница
            if (j < 0) break;

            // Есть фигура, и она союзная
            if (Board[i][j] != null && Board[i][j].Team == Team) break;
            result.Add(new Vector2Int(i, j));
            // Есть фигура, и она вражеская
            if (Board[i][j] != null && Board[i][j].Team != Team) break;
        }

        // Ходы по диагонали вверх влево
        for (int i = X - 1, j = Y; i >= 0; i--)
        {
            if (i % 2 == 0) j++;
            // Верхняя граница
            if (j >= Board[i].Length) break;

            // Есть фигура, и она союзная
            if (Board[i][j] != null && Board[i][j].Team == Team) break;
            result.Add(new Vector2Int(i, j));
            // Есть фигура, и она вражеская
            if (Board[i][j] != null && Board[i][j].Team != Team) break;
        }

        // Ходы по диагонали вниз влево
        for (int i = X - 1, j = Y; i >= 0; i--)
        {
            if (i % 2 == 1) j--;
            // Нижняя граница
            if (j < 0) break;

            // Есть фигура, и она союзная
            if (Board[i][j] != null && Board[i][j].Team == Team) break;
            result.Add(new Vector2Int(i, j));
            // Есть фигура, и она вражеская
            if (Board[i][j] != null && Board[i][j].Team != Team) break;
        }

        return result;
    }
}
