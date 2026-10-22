using UnityEngine;

// Keeps a fixed-size layout centred in its parent and scales it to fit, so it never
// overflows small or notched screens (and can grow a little on roomy ones)
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ScaleToFit : MonoBehaviour
{
    [Tooltip("Unscaled size of the content")]
    public Vector2 size = new Vector2(1800, 430);
    public float minScale = 0.5f;
    public float maxScale = 1.2f;
    [Tooltip("Fit inside this rect instead of the parent (e.g. the canvas, when the parent overflows the screen)")]
    public RectTransform area;

    RectTransform rectTransform;
    Vector2 lastArea;

    private void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        lastArea = Vector2.zero;
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        RectTransform fitTo = area ? area : rectTransform.parent as RectTransform;
        if (!fitTo)
        {
            return;
        }
        Vector2 available = fitTo.rect.size;
        if (available == lastArea || available.x <= 0 || available.y <= 0)
        {
            return;
        }
        lastArea = available;

        float scale = Mathf.Clamp(Mathf.Min(available.x / size.x, available.y / size.y), minScale, maxScale);
        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = new Vector3(scale, scale, 1);
    }
}
