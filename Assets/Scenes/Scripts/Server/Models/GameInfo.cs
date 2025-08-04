public class GameInfoResponse
{
    public int GameId { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public string WhitePlayerId { get; set; }
    public string BlackPlayerId { get; set; }
    public TimeControlDto TimeControl { get; set; }
}
