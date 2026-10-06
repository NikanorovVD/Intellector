using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GameOverWatcher : MonoBehaviour
{
    [SerializeField] private Board board;

    public const int MaxMovesWithoutProgress = 60;
    public const int MaxPositionRepeats = 3;

    private int currentMovesWithoutProgressCount;
    private Dictionary<int, int> positionCounts = new();
    private bool progressiveMove;

    private void Start()
    {
        if (Settings.GameMode == GameMode.Replay) return;

        currentMovesWithoutProgressCount = Settings.StartPosition?.HalfmoveClock ?? 0;

        board.MoveStartEvent += MoveStartHandler;
        board.MoveEndEvent += MoveEndHandler;
        board.RestartEvent += () =>
        {
            currentMovesWithoutProgressCount = Settings.StartPosition?.HalfmoveClock ?? 0;
            positionCounts.Clear();
        };
    }

    private void MoveStartHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        if (IsProgressiveMove(start, end))
        {
            currentMovesWithoutProgressCount = 0;
            progressiveMove = true;
        }
        else
        {
            currentMovesWithoutProgressCount++;
            progressiveMove = false;
            if (currentMovesWithoutProgressCount >= MaxMovesWithoutProgress)
            {
                board.GameOver(null, EndGameReason.DrawBy30MovesRule);
                return;
            }
        }
    }

    private void MoveEndHandler(Vector2Int start, Vector2Int end, int transformInfo)
    {
        if (progressiveMove) positionCounts.Clear();
        int positionHash = BoardToEngine.CreateEngine(board).Hash();
        if (positionCounts.TryGetValue(positionHash, out int count))
        {
            count++;
            if (count >= MaxPositionRepeats)
            {
                board.GameOver(null, EndGameReason.DrawByRepeatingPosition);
                return;
            }
            positionCounts[positionHash] = count;
        }
        else
        {
            positionCounts.Add(positionHash, 1);
        }

        if (IsAllPiecesBlocked())
        {
            board.GameOver(!board.Turn, EndGameReason.AllPiecesBlocked);
            return;
        }
    }

    private bool IsAllPiecesBlocked()
    {
        bool teamToMove = board.Turn;
        foreach (IPiece[] pieces in board.Pieces)
        {
            foreach (IPiece piece in pieces)
            {
                if (piece != null && piece.Team == teamToMove)
                {
                    if (piece.GetAvailableMoves().Any())
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private bool IsProgressiveMove(Vector2Int start, Vector2Int end)
    {
        if (board.Pieces[start.x][start.y].Type == PieceType.Progressor) return true;
        if (board.Pieces[end.x][end.y] != null) return true;
        return false;
    }
}
