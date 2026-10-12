using System.Collections.Generic;

using UnityEngine;

public class Agressor : Piece
{
    public override PieceType Type => PieceType.Agressor;
    public override List<Vector2Int> GetAvailableMoves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // Ходы вправо
        for (int i = X + 2; i <= 8; i += 2)
        {
            // Есть фигура, и она союзная
            if (Board[i][Y] != null && Board[i][Y].Team == Team) break;
            result.Add(new Vector2Int(i, Y));
            // Есть фигура, и она вражеская
            if (Board[i][Y] != null && Board[i][Y].Team != Team) break;
        }

        // Ходы влево
        for (int i = X - 2; i >= 0; i -= 2)
        {
            // Есть фигура, и она союзная
            if (Board[i][Y] != null && Board[i][Y].Team == Team) break;
            result.Add(new Vector2Int(i, Y));
            // Есть фигура, и она вражеская
            if (Board[i][Y] != null && Board[i][Y].Team != Team) break;
        }

        // Ходы по диагонали вверх вправо
        for (int i = X + 1, j = Y + 1; i <= 8; i++, j++)
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
        for (int i = X + 1, j = Y - 1; i <= 8; i++, j--)
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
        for (int i = X - 1, j = Y + 1; i >= 0; i--, j++)
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
        for (int i = X - 1, j = Y - 1; i >= 0; i--, j--)
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
