public enum Side
{
    White = 0,
    Black = 1
}

public static class Sides
{
    public static Side Opposite(Side side)
    {
        return side == Side.White ? Side.Black : Side.White;
    }

    public static Side ToMove(int ply)
    {
        return ply % 2 == 0 ? Side.White : Side.Black;
    }
}
