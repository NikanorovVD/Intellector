using System.Collections.Generic;

using UnityEngine;

public enum PieceMaterials
{
    Standard = 0,
    Mramor = 1,
    New = 2
}

public class MaterialSelector : MonoBehaviour
{
    [SerializeField] private Material[] whiteMaterials;
    [SerializeField] private Material[] blackMaterials;

    private static readonly Dictionary<PieceMaterials, string> materialNames = new Dictionary<PieceMaterials, string> {
        { PieceMaterials.Standard, "Стандарт" } ,
        { PieceMaterials.Mramor, "Мрамор" },
        { PieceMaterials.New, "Новый" }
    };

    public static string MaterialName(PieceMaterials material) => materialNames[material];

    public (Material, Material) GetCurrentMaterials(PieceMaterials materials)
    {
        return (whiteMaterials[(int)materials], blackMaterials[(int)materials]);
    }
}
