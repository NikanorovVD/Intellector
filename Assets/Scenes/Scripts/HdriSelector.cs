using UnityEngine;

public enum HdriMap
{
    Cinema = 0,
    Church = 1
}

public class HdriSelector : MonoBehaviour
{
    [SerializeField] private Material cinema;
    [SerializeField] private Material church;

    private void Awake()
    {
        Material material = MaterialFor(Settings.Hdri);
        if (material == null)
            return;

        RenderSettings.skybox = material;
        DynamicGI.UpdateEnvironment();
    }

    public static string DisplayName(HdriMap map)
    {
        return map switch
        {
            HdriMap.Church => "Церковь",
            _ => "Кинотеатр"
        };
    }

    private Material MaterialFor(HdriMap map)
    {
        return map switch
        {
            HdriMap.Church => church,
            _ => cinema
        };
    }
}
