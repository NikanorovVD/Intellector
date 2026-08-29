using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum AnchorType
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Top,
    Bottom,
    Left,
    Right
}

public class ScaleableNonCenteredComponent : MonoBehaviour
{
    [SerializeField] private float DesktopScale = 1;
    [SerializeField] private float AndroidScale;
    [SerializeField] private AnchorType anchorType = AnchorType.TopLeft;

    private Vector2 initialPosition;
    private Vector3 initialScale;
    private RectTransform parentRectTransform;
    private bool hasRectTransformParent;

    void Awake()
    {
        // Запоминаем начальную позицию и масштаб
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        // Проверяем, есть ли у родителя RectTransform
        parentRectTransform = transform.parent?.GetComponent<RectTransform>();
        hasRectTransformParent = parentRectTransform != null;

        // Применяем масштаб
        Vector3 targetScale = GetTargetScale();
        ApplyScaleWithAnchor(targetScale);
    }

    private Vector3 GetTargetScale()
    {
#if UNITY_EDITOR
        return AndroidSimulationMenu.IsAndroidSimulationOn() ?
            new Vector3(AndroidScale, AndroidScale, 1) :
            new Vector3(DesktopScale, DesktopScale, 1);
#else
        return Application.platform == RuntimePlatform.Android ?
            new Vector3(AndroidScale, AndroidScale, 1) :
            new Vector3(DesktopScale, DesktopScale, 1);
#endif
    }

    private void ApplyScaleWithAnchor(Vector3 targetScale)
    {
        // Применяем новый масштаб
        transform.localScale = targetScale;

        // Вычисляем изменение размера
        Vector2 scaleChange = new Vector2(
            targetScale.x / initialScale.x,
            targetScale.y / initialScale.y
        );

        // Корректируем позицию в зависимости от типа привязки
        Vector2 newPosition = initialPosition;

        switch (anchorType)
        {
            case AnchorType.TopLeft:
                if (hasRectTransformParent)
                {
                    // Для RectTransform учитываем размеры
                    float parentWidth = parentRectTransform.rect.width;
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetX = (initialPosition.x + parentWidth / 2) * (scaleChange.x - 1);
                    float offsetY = (parentHeight / 2 - initialPosition.y) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x + offsetX,
                        initialPosition.y - offsetY
                    );
                }
                else
                {
                    // Без RectTransform - корректировка от центра
                    newPosition = initialPosition * scaleChange;
                }
                break;

            case AnchorType.TopRight:
                if (hasRectTransformParent)
                {
                    float parentWidth = parentRectTransform.rect.width;
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetX = (parentWidth / 2 - initialPosition.x) * (scaleChange.x - 1);
                    float offsetY = (parentHeight / 2 - initialPosition.y) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x - offsetX,
                        initialPosition.y - offsetY
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x * (2 - scaleChange.x),
                        initialPosition.y * scaleChange.y
                    );
                }
                break;

            case AnchorType.BottomLeft:
                if (hasRectTransformParent)
                {
                    float parentWidth = parentRectTransform.rect.width;
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetX = (initialPosition.x + parentWidth / 2) * (scaleChange.x - 1);
                    float offsetY = (initialPosition.y + parentHeight / 2) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x + offsetX,
                        initialPosition.y + offsetY
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x * scaleChange.x,
                        initialPosition.y * (2 - scaleChange.y)
                    );
                }
                break;

            case AnchorType.BottomRight:
                if (hasRectTransformParent)
                {
                    float parentWidth = parentRectTransform.rect.width;
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetX = (parentWidth / 2 - initialPosition.x) * (scaleChange.x - 1);
                    float offsetY = (initialPosition.y + parentHeight / 2) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x - offsetX,
                        initialPosition.y + offsetY
                    );
                }
                else
                {
                    newPosition = initialPosition * new Vector2(2 - scaleChange.x, 2 - scaleChange.y);
                }
                break;

            case AnchorType.Top:
                if (hasRectTransformParent)
                {
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetY = (parentHeight / 2 - initialPosition.y) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x,
                        initialPosition.y - offsetY
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x,
                        initialPosition.y * scaleChange.y
                    );
                }
                break;

            case AnchorType.Bottom:
                if (hasRectTransformParent)
                {
                    float parentHeight = parentRectTransform.rect.height;
                    float offsetY = (initialPosition.y + parentHeight / 2) * (scaleChange.y - 1);
                    newPosition = new Vector2(
                        initialPosition.x,
                        initialPosition.y + offsetY
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x,
                        initialPosition.y * (2 - scaleChange.y)
                    );
                }
                break;

            case AnchorType.Left:
                if (hasRectTransformParent)
                {
                    float parentWidth = parentRectTransform.rect.width;
                    float offsetX = (initialPosition.x + parentWidth / 2) * (scaleChange.x - 1);
                    newPosition = new Vector2(
                        initialPosition.x + offsetX,
                        initialPosition.y
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x * scaleChange.x,
                        initialPosition.y
                    );
                }
                break;

            case AnchorType.Right:
                if (hasRectTransformParent)
                {
                    float parentWidth = parentRectTransform.rect.width;
                    float offsetX = (parentWidth / 2 - initialPosition.x) * (scaleChange.x - 1);
                    newPosition = new Vector2(
                        initialPosition.x - offsetX,
                        initialPosition.y
                    );
                }
                else
                {
                    newPosition = new Vector2(
                        initialPosition.x * (2 - scaleChange.x),
                        initialPosition.y
                    );
                }
                break;
        }

        transform.localPosition = newPosition;
    }
}