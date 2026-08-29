using System;
using System.Collections.Generic;
using UnityEngine;

public class Move
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

    public Move(IPiece piece, Vector2Int coor)
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

    public static List<Move> MoveWithIntellector(IPiece piece, Vector2Int coor)
    {
        List<Move> moves = new();
        Move standard = new Move(piece, coor);
        moves.Add(standard);

        if (standard.taking && piece.Type != PieceType.progressor)
        {
            moves.Add(new Move(standard, piece.Board[coor.x][coor.y].Type));
        }

        return moves;
    }

    public static List<Move> ProgressorMoveWithTransformation(IPiece piece, Vector2Int coor)
    {
        List<Move> moves = new();

        if (piece.Type != PieceType.progressor)
        {
            throw new InvalidOperationException("Not a progressor");
        }

        moves.Add(new Move(piece, coor, PieceType.defensor));
        moves.Add(new Move(piece, coor, PieceType.agressor));
        moves.Add(new Move(piece, coor, PieceType.dominator));
        moves.Add(new Move(piece, coor, PieceType.liberator));

        return moves;
    }

    private Move(Move standard, PieceType end_type)
    {
        start_x = standard.start_x;
        start_y = standard.start_y;
        start_type = standard.start_type;
        end_x = standard.end_x;
        end_y = standard.end_y;
        this.end_type = end_type;
        castling = standard.castling;
        taking = standard.taking;
        previous_piece = standard.previous_piece;
    }

    private Move(IPiece piece, Vector2Int coor, PieceType end_type) : this(new Move(piece, coor), end_type)
    { }
}
