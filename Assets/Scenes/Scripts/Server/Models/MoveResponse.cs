public class MoveResponse
{
    public Move Move { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public Time Time { get; set; }
    public MoveResponse()
    { }
    public MoveResponse(Move move, PlayerColor playerColor, Time time)
    {
        Move = move;
        PlayerColor = playerColor;
        Time = time;
    }

    public MoveResponse(Move move, PlayerColor playerColor) :
        this(move, playerColor, null)
    { }
}

public enum PlayerColor
{
    White = 0,
    Black = 1
}

public class Time
{
    public int White { get; set; }
    public int Black { get; set; }
}

