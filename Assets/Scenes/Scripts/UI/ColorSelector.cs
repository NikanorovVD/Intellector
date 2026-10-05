using System;

using UnityEngine;

[Serializable()]
public enum ColorChoice
{
    White = 0,
    Black = 1,
    Random = 2
}

public class ColorSelector : MonoBehaviour
{
    [SerializeField] private GameObject whiteSelection;
    [SerializeField] private GameObject blackSelection;
    [SerializeField] private GameObject randomSelection;

    public ColorChoice Color { get; private set; }

    private bool colorSet;

    private void Start()
    {
        if (!colorSet)
            RandomClick();
    }

    public void SetColor(ColorChoice color)
    {
        switch (color)
        {
            case ColorChoice.White:
                WhiteClick();
                break;
            case ColorChoice.Black:
                BlackClick();
                break;
            case ColorChoice.Random:
                RandomClick();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }
    }

    public static bool ResolveTeam(ColorChoice color)
    {
        return color switch
        {
            ColorChoice.White => false,
            ColorChoice.Black => true,
            ColorChoice.Random => UnityEngine.Random.Range(0, 2) == 1,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public void WhiteClick()
    {
        colorSet = true;
        Color = ColorChoice.White;
        whiteSelection.SetActive(true);
        blackSelection.SetActive(false);
        randomSelection.SetActive(false);
    }

    public void BlackClick()
    {
        colorSet = true;
        Color = ColorChoice.Black;
        blackSelection.SetActive(true);
        randomSelection.SetActive(false);
        whiteSelection.SetActive(false);
    }

    public void RandomClick()
    {
        colorSet = true;
        Color = ColorChoice.Random;
        randomSelection.SetActive(true);
        whiteSelection.SetActive(false);
        blackSelection.SetActive(false);
    }
}
