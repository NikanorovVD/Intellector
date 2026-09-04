public class RecordedPosition
{
    public TileState?[][] Pieces;
    public bool BlackToMove;
    public int HalfmoveClock;
    public int FullmoveNumber = 1;

    public RecordedPosition()
    {
        Pieces = new TileState?[9][];
        for (int x = 0; x < 9; x++)
            Pieces[x] = new TileState?[7 - (x % 2)];
    }
}
