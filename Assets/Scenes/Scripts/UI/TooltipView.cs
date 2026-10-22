using UnityEngine;
using UnityEngine.UI;

public class TooltipView : MonoBehaviour
{
    [SerializeField] private RectTransform bubble;
    [SerializeField] private Text label;

    private TooltipTrigger owner;

    public void Show(TooltipTrigger source, RectTransform target, string text)
    {
        if (bubble == null || label == null || target == null)
            return;

        owner = source;
        label.text = text ?? string.Empty;
        bubble.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
        Place(target);
    }

    public void Hide(TooltipTrigger source)
    {
        if (owner != source || bubble == null)
            return;

        owner = null;
        bubble.gameObject.SetActive(false);
    }

    private void Place(RectTransform target)
    {
        var parent = bubble.parent as RectTransform;
        if (parent == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 local))
            return;

        bubble.anchoredPosition = local + new Vector2(0f, -4f);
    }
}
