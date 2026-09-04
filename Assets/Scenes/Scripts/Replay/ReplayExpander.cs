using System.Collections.Generic;

public static class ReplayExpander
{
    public static List<ReplayMove> Expand(GameRecord record)
    {
        var board = record != null && record.SetUp == "1" && !string.IsNullOrEmpty(record.Ifen)
            ? new NotationBoard(IfenParser.Parse(record.Ifen))
            : new NotationBoard();
        var moves = new List<ReplayMove>();
        if (record?.Moves == null)
            return moves;

        for (int i = 0; i < record.Moves.Count; i++)
        {
            RecordedMove move = record.Moves[i];
            var replayMove = new ReplayMove
            {
                From = move.From,
                To = move.To,
                FromBefore = board.Get(move.From),
                ToBefore = board.Get(move.To),
                Notation = IpgnFormatter.FormatMove(move)
            };
            board.Apply(move);
            replayMove.FromAfter = board.Get(move.From);
            replayMove.ToAfter = board.Get(move.To);
            moves.Add(replayMove);
        }

        return moves;
    }
}
