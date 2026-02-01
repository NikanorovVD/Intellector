using System.Collections.Generic;
using UnityEngine;

public class Dominator : Piece
{
    public override PieceType Type => PieceType.dominator;
    public override List<Vector2Int> GetAvailableMooves()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        //ходы вверх
        for (int j = Y + 1; j < Board[X].Length; j++)
        {
            if (Board[X][j] != null && Board[X][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(X, j));
            if (Board[X][j] != null && Board[X][j].Team != Team) break;      //есть фигура и она вражеская
        }

        //ходы вниз
        for (int j = Y - 1; j >= 0; j--)
        {
            if (Board[X][j] != null && Board[X][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(X, j));
            if (Board[X][j] != null && Board[X][j].Team != Team) break;      //есть фигура и она вражеская
        }

        //ходы по диагонали вверх вправо
        for (int i = X + 1, j = Y; i <= 8; i++)
        {
            if (i % 2 == 0) j++;
            if (j >= Board[i].Length) break;                                             //верхняя граница

            if (Board[i][j] != null && Board[i][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(i, j));
            if (Board[i][j] != null && Board[i][j].Team != Team) break;      //есть фигура и она вражеская
        }

        //ходы по диагонали вниз вправо
        for (int i = X + 1, j = Y; i <= 8; i++)
        {
            if (i % 2 == 1) j--;
            if (j < 0) break;                                                                   //нижняя граница

            if (Board[i][j] != null && Board[i][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(i, j));
            if (Board[i][j] != null && Board[i][j].Team != Team) break;      //есть фигура и она вражеская
        }

        //ходы по диагонали вверх влево
        for (int i = X - 1, j = Y; i >= 0; i--)
        {
            if (i % 2 == 0) j++;
            if (j >= Board[i].Length) break;                                             //верхняя граница

            if (Board[i][j] != null && Board[i][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(i, j));
            if (Board[i][j] != null && Board[i][j].Team != Team) break;      //есть фигура и она вражеская
        }

        //ходы по диагонали вниз влево
        for (int i = X - 1, j = Y; i >= 0; i--) 
        {
            if (i % 2 == 1) j--;
            if (j < 0) break;                                                                   //нижняя граница

            if (Board[i][j] != null && Board[i][j].Team == Team) break;      //есть фигура и она союзная
            result.Add(new Vector2Int(i, j));
            if (Board[i][j] != null && Board[i][j].Team != Team) break;      //есть фигура и она вражеская
        }

        return result;
    }
}
