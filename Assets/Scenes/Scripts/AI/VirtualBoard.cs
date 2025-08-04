using System.Collections.Generic;
using UnityEngine;

public class VirtualBoard
{
    private bool turn;

    private readonly IPiece[][] pieces;

    public int Valuation {  get; private set; }

    public VirtualBoard(IPiece[][] board, int valuation)
    {
        pieces = new IPiece[9][];
        for (int i = 0; i < 9; i++)
        {
            pieces[i] = new IPiece[7 - (i % 2)];
            for(int j = 0; j < pieces[i].Length; j++)
            {
                pieces[i][j] = MakeVirtualCopy(board[i][j]);
            }
        }

        Valuation = valuation;
        turn = AI.AI_team;
    }

    public void MakeMove(AIMove move)
    {
        IPiece start_piece = pieces[move.start_x][move.start_y];
        IPiece end_piece = pieces[move.end_x][move.end_y];

        if (start_piece == null)
            throw new System.NullReferenceException("��������� ������ NULL");

        DecreaseValuation(move);

        //���������
        if(move.castling)
        {
            (pieces[move.start_x][move.start_y], pieces[move.end_x][move.end_y]) = (pieces[move.end_x][move.end_y], pieces[move.start_x][move.start_y]);
        }
        //����� ������ ���
        else
        {
            pieces[move.end_x][move.end_y] = start_piece;
            if (move.end_type != start_piece.Type)
                ChangeType(ref pieces[move.end_x][move.end_y], move.end_type);

            pieces[move.start_x][move.start_y] = null;
        }

        SynchronizeCoordinates(move);
        IncreaseValuation(move);
        turn = !turn;
    }

    public void CancelMove(AIMove move)
    {
        IPiece start_piece = pieces[move.start_x][move.start_y];
        IPiece end_piece = pieces[move.end_x][move.end_y];

        if (end_piece == null)
            throw new System.NullReferenceException("�������� ������ NULL");

        DecreaseValuation(move);

        //���������
        if (move.castling)
        {
            (pieces[move.start_x][move.start_y], pieces[move.end_x][move.end_y]) = (pieces[move.end_x][move.end_y], pieces[move.start_x][move.start_y]);
        }
        //����� ������ ���
        else
        {
            pieces[move.start_x][move.start_y] = end_piece;
            if (move.start_type != end_piece.Type)
                ChangeType(ref pieces[move.start_x][move.start_y], move.start_type);

            pieces[move.end_x][move.end_y] = move.previous_piece;
        }
              
        SynchronizeCoordinates(move);
        IncreaseValuation(move);
        turn = !turn;
    }

    public List<AIMove> GetAllMoves()
    {
        List<AIMove> moves = new();
        foreach (IPiece[] row in pieces)
            foreach (IPiece piece in row)
            {
                if (piece != null && piece.Team == turn)
                {
                    foreach (Vector2Int coor in piece.GetAvaibleMooves())
                    {
                        if (piece.HasIntellectorNearby())
                        {
                            moves.AddRange(AIMove.MoveWithIntellector(piece, coor));
                        }
                        else
                        {
                            moves.Add(new AIMove(piece, coor));
                        }
                    }
                }
            }
        return moves;
    }

    private void SynchronizeCoordinates(AIMove move)
    {
        if (pieces[move.start_x][move.start_y] != null)
        {
            pieces[move.start_x][move.start_y].X = move.start_x;
            pieces[move.start_x][move.start_y].Y = move.start_y;
        }
        if(pieces[move.end_x][move.end_y] != null)
        {
            pieces[move.end_x][move.end_y].X = move.end_x; 
            pieces[move.end_x][move.end_y].Y = move.end_y;
        }       
    }

    private void DecreaseValuation(AIMove move)
    {
        Valuation -= Evaluator.GetValue(pieces[move.start_x][move.start_y]);
        Valuation -= Evaluator.GetValue(pieces[move.end_x][move.end_y]);
    }

    private void IncreaseValuation(AIMove move)
    {
        Valuation += Evaluator.GetValue(pieces[move.start_x][move.start_y]);
        Valuation += Evaluator.GetValue(pieces[move.end_x][move.end_y]);
    }

    private IPiece MakePiece(PieceType type)
    {
        switch (type)
        {
            case PieceType.intellector: return new Intellector();
            case PieceType.progressor: return new Progressor();
            case PieceType.liberator: return new Liberator();
            case PieceType.dominator: return new Dominator();
            case PieceType.agressor: return new Agressor();
            case PieceType.defensor: return new Defensor();
            default: throw new System.Exception("����������� ��� ������");
        }
    }

    private void ChangeType(ref IPiece piece, PieceType new_type)
    {
        IPiece new_piece = MakePiece(new_type);
        new_piece.X = piece.X;
        new_piece.Y = piece.Y;
        new_piece.Team = piece.Team;
        new_piece.Board = this.pieces;

        piece = new_piece;
    }

    private IPiece MakeVirtualCopy(IPiece piece)
    {
        if(piece == null ) return null;
        IPiece pieceCopy = MakePiece(piece.Type);
        pieceCopy.X = piece.X;
        pieceCopy.Y = piece.Y;
        pieceCopy.Team = piece.Team;
        pieceCopy.Board = this.pieces;

        return pieceCopy;
    }
}
