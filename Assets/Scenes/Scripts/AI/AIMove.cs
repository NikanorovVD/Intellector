using System;
using System.Collections.Generic;
using UnityEngine;

public class AIMove 
{
    public int start_x;
    public int start_y;
    public PieceType start_type;

    public int end_x;
    public int end_y;
    public PieceType end_type;

    public IPiece previous_piece;//опасно!
    public bool castling;
    public bool taking;

    public AIMove(IPiece piece, Vector2Int coor)
    {
        start_x = piece.X;
        start_y = piece.Y;
        start_type = piece.Type;
        end_x = coor.x;
        end_y = coor.y;
        end_type = piece.Type;

        IPiece start_piece = piece.Board[start_x][start_y];
        IPiece end_piece = piece.Board[end_x][end_y];
        castling = (start_piece != null && start_piece.Type == PieceType.intellector && end_piece != null && end_piece.Type == PieceType.defensor && start_piece.Team == end_piece.Team);

        taking = (end_piece != null) && (!castling);

        if (taking) previous_piece = end_piece;
        else previous_piece = null;
    }

    public static List<AIMove> MoveWithIntellector(IPiece piece, Vector2Int coor)
    {
        List<AIMove> moves = new();
        AIMove standard = new AIMove(piece, coor);
        moves.Add(standard);

        if (standard.taking)
        {
            moves.Add(new AIMove(standard, piece.Board[coor.x][coor.y].Type));
        }

        return moves;
    }

    public List<AIMove> ProgressorMoveWithTransform()
    {
        throw new NotImplementedException();
    }

    private AIMove(AIMove standard, PieceType taken_type)
    {
        start_x = standard.start_x;
        start_y = standard.start_y;
        start_type = standard.start_type;
        end_x = standard.end_x;
        end_y = standard.end_y;
        end_type = taken_type;
        castling = standard.castling;
        taking = standard.taking;
        previous_piece = standard.previous_piece;
    }
}
