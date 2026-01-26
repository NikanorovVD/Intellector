using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DrawWatcher : MonoBehaviour
{
    [SerializeField] public Board Board;

    public const int MAX_MOVES_WITHOUT_PROGRESS = 60;
    public const int MAX_POSITION_REPEATS = 3;

    private int currentMovesWithoutProgressCount; 
    private Dictionary<string, int> positionCounts = new();
    private bool progressiveMove;

    void Start()
    {
        currentMovesWithoutProgressCount = 0;

        Board.MoveStartEvent += MoveStartHandler;
        Board.MoveEndEvent += MoveEndHandler;
        Board.RestartEvent += () =>
        {
            currentMovesWithoutProgressCount = 0;
            positionCounts.Clear();
        };
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        if (IsMoveProgressive(start, end))
        {
            currentMovesWithoutProgressCount = 0;
            progressiveMove = true;
        }
        else
        {
            currentMovesWithoutProgressCount++;
            progressiveMove = false;
            if (currentMovesWithoutProgressCount >= MAX_MOVES_WITHOUT_PROGRESS)
            {
                Board.GameOver(null, EndGameReason.DrawBy30MovesRule);
                return;
            }
        }
    }

    private void MoveEndHandler(Vector2Int start, Vector2Int end, int transform_info)
    {
        if (progressiveMove) positionCounts.Clear();
        string positionHash = GetPositionHash(Board.Turn, Board.pieces);
        if (positionCounts.TryGetValue(positionHash, out int count))
        {
            count++;
            if(count >= MAX_POSITION_REPEATS)
            {
                Board.GameOver(null, EndGameReason.DrawByRepeatingPosition);
                return;
            }
            positionCounts[positionHash] = count;
        }
        else
        {
            positionCounts.Add(positionHash, 1);
        }
    }

    private bool IsMoveProgressive(Vector2Int start, Vector2Int end)
    {
        if (Board.pieces[start.x][start.y].Type == PieceType.progressor) return true;   // прогрессор сделал ход
        if (Board.pieces[end.x][end.y] != null) return true;    // или была взята фигура
        return false;
    }

    private string GetPositionHash(bool turn, IPiece[][] pieces)
    {
        var hashBuilder = new StringBuilder();
        hashBuilder.Append(turn ? '0' : '1');
        for (int i = 0; i < pieces.Length; i++)
        {
            for (int j = 0; j < pieces[i].Length; j++)
            {
                var piece = pieces[i][j];
                char pieceHash = piece switch
                {
                    null => '_',
                    AgressorPiece => piece.Team ? 'a' : 'A',
                    DominatorPiece => piece.Team ? 'd' : 'D',
                    ProgressorPiece => piece.Team ? 'p' : 'P',
                    LiberatorPiece => piece.Team ? 'l' : 'L',
                    IntellectorPiece => piece.Team ? 'i' : 'I',
                    DefensorPiece => piece.Team ? 'd' : 'D',
                };
                hashBuilder.Append(pieceHash);
            }
        }
        return hashBuilder.ToString();
    }
}
