using System;
using UnityEngine;

[Serializable()]
public enum ColorChoice
{
    white = 0,
    black = 1,
    random = 2
}

public class ColorSelector : MonoBehaviour
{
    [SerializeField] GameObject WhiteSelection;
    [SerializeField] GameObject BlackSelection;
    [SerializeField] GameObject RandomSelection;

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
            case ColorChoice.white:
                WhiteClick();
                break;
            case ColorChoice.black:
                BlackClick();
                break;
            case ColorChoice.random:
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
            ColorChoice.white => false,
            ColorChoice.black => true,
            ColorChoice.random => UnityEngine.Random.Range(0, 2) == 1,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public void WhiteClick()
    {
        colorSet = true;
        Color = ColorChoice.white;
        WhiteSelection.SetActive(true);
        BlackSelection.SetActive(false);
        RandomSelection.SetActive(false);
    }

    public void BlackClick()
    {
        colorSet = true;
        Color = ColorChoice.black;
        BlackSelection.SetActive(true);
        RandomSelection.SetActive(false);
        WhiteSelection.SetActive(false);
    }

    public void RandomClick()
    {
        colorSet = true;
        Color = ColorChoice.random;
        RandomSelection.SetActive(true);
        WhiteSelection.SetActive(false);
        BlackSelection.SetActive(false);
    }
}
