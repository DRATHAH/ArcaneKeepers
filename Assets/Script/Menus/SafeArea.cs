using UnityEngine;

// Fits this RectTransform to the screen's safe area, so notches and rounded corners never cover the UI
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rectTransform;
    Rect appliedArea;
    Vector2Int appliedScreen;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        // Re-fit after rotation or a resolution change
        if (Screen.safeArea != appliedArea || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        appliedArea = Screen.safeArea;
        appliedScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 min = appliedArea.position;
        Vector2 max = appliedArea.position + appliedArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        // Ignore a safe area that doesn't fit the screen (the editor's simulator can report one briefly)
        if (min.x < 0 || min.y < 0 || max.x > 1.001f || max.y > 1.001f || max.x <= min.x || max.y <= min.y)
        {
            min = Vector2.zero;
            max = Vector2.one;
        }

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
