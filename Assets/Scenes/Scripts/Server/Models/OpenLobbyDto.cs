public class OpenLobbyDto
{
    public int LobbyId { get; set; }
    public string OwnerName { get; set; }
    public TimeControlDto TimeControl { get; set; }
    public ColorChoice ColorChoice { get; set; }
    public bool Rating { get; set; }
}
