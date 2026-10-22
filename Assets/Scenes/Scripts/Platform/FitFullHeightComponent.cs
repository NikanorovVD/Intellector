using UnityEngine;

public class FitFullHeightComponent : MonoBehaviour
{
    private bool fitted;

    private void Start()
    {
        fitted = Fit();
    }

    private void LateUpdate()
    {
        if (fitted)
            return;
        fitted = Fit();
    }

    private bool Fit()
    {
        var rect = transform as RectTransform;
        if (rect == null || !FillsParentHeight(rect))
            return true;
        float scale = rect.localScale.y;
        if (scale <= 0f || Mathf.Approximately(scale, 1f))
            return true;
        var parent = rect.parent as RectTransform;
        if (parent == null)
            return true;

        float parentHeight = parent.rect.height;
        if (parentHeight <= 0f)
        {
            Canvas.ForceUpdateCanvases();
            parentHeight = parent.rect.height;
        }
        if (parentHeight <= 0f)
            return false;

        // После масштаба низ должен совпасть с низом родителя, поэтому высота заранее меньше.
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, parentHeight * (1f / scale - 1f));
        return true;
    }

    private static bool FillsParentHeight(RectTransform rect)
    {
        return Mathf.Approximately(rect.anchorMin.y, 0f)
            && Mathf.Approximately(rect.anchorMax.y, 1f)
            && Mathf.Approximately(rect.sizeDelta.y, 0f);
    }
}
