using System;

using UnityEngine;

public enum TileLayer
{
    Tile,
    HoverTile,
    SelectedTile,
    Available,
    HoverAvailable,
    HoverSelected,
    Hint,
    HoverHint
}

public static class TileLayers
{
    public static readonly int Mask;

    private static readonly int[] indices;

    static TileLayers()
    {
        string[] names = Enum.GetNames(typeof(TileLayer));
        indices = new int[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            indices[i] = LayerMask.NameToLayer(names[i]);
            if (indices[i] < 0)
                throw new InvalidOperationException($"Слой {names[i]} не найден");
        }
        Mask = LayerMask.GetMask(names);
    }

    public static int Index(TileLayer layer) => indices[(int)layer];
}
