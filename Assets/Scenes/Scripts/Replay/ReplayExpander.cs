using System.Collections.Generic;

public static class ReplayExpander
{
    public static List<ReplayMove> Expand(GameRecord record)
    {
        var board = new NotationBoard();
        var moves = new List<ReplayMove>();
        if (record?.Moves == null)
            return moves;

        foreach (RecordedMove move in record.Moves)
        {
            var replayMove = new ReplayMove
            {
                From = move.From,
                To = move.To,
                FromBefore = board.Get(move.From),
                ToBefore = board.Get(move.To)
            };
            board.Apply(move);
            replayMove.FromAfter = board.Get(move.From);
            replayMove.ToAfter = board.Get(move.To);
            moves.Add(replayMove);
        }

        return moves;
    }
}
