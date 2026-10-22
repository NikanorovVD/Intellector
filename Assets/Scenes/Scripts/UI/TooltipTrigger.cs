using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipView view;
    [SerializeField] private string text;

    private bool pointerInside;
    private bool pinned;

    public string Text
    {
        get => text;
        set
        {
            text = value ?? string.Empty;
            Refresh();
        }
    }

    public void Pin()
    {
        pinned = true;
        Refresh();
    }

    public void Unpin()
    {
        pinned = false;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Refresh();
    }

    private void OnDisable()
    {
        pointerInside = false;
        pinned = false;
        if (view != null)
            view.Hide(this);
    }

    private void Refresh()
    {
        if (view == null || !isActiveAndEnabled)
            return;

        if (pointerInside || pinned)
            view.Show(this, transform as RectTransform, text);
        else
            view.Hide(this);
    }
}
