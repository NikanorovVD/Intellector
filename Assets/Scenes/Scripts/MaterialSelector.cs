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
    [SerializeField] private Material[] WhiteMaterials;
    [SerializeField] private Material[] BlackMaterials;

    private static readonly Dictionary<PieceMaterials, string> _materialNames = new Dictionary<PieceMaterials, string> {
        { PieceMaterials.Standard, "Стандарт" } ,
        { PieceMaterials.Mramor, "Мрамор" },
        { PieceMaterials.New, "Новый" }
    };

    public static string MaterialName(PieceMaterials material) => _materialNames[material];

    public (Material, Material) GetCurrentMaterials(PieceMaterials materials)
    {
        return (WhiteMaterials[(int)materials], BlackMaterials[(int)materials]);
    }
}
