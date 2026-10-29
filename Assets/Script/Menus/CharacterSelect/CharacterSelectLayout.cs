using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fits the character select to any screen shape. The header and the squad roster keep their size;
// the name block and the lineup keepers scale to fill the space left between them, so phones, 16:9
// screens and tablets all get a full screen with nothing cut off.
[ExecuteAlways]
public class CharacterSelectLayout : MonoBehaviour
{
    [Tooltip("The safe area everything is laid out in")]
    public RectTransform area;
    public RectTransform intelPanel;
    public RectTransform lineup;
    public LineupScroller scroller;
    public List<CharacterCard> cards = new List<CharacterCard>();

    public float headerHeight = 150;
    public float footerHeight = 270;
    public float margin = 24;
    public float gap = 12;
    [Tooltip("Unscaled size of the name block")]
    public Vector2 intelSize = new Vector2(560, 470);
    [Tooltip("Unscaled size of one lineup keeper (platform, art and name)")]
    public Vector2 cardSize = new Vector2(370, 560);
    public float minScale = 0.7f;
    public float maxScale = 1.4f;
    [Tooltip("Largest share of the screen width the name block may take")]
    public float maxIntelShare = 0.3f;
    [Tooltip("Keepers never grow so big that fewer than this many fit across the lineup")]
    public float minVisibleKeepers = 2.6f;

    Vector2 lastSize;

    private void OnEnable()
    {
        lastSize = Vector2.zero;
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        if (!area || !intelPanel || !lineup)
        {
            return;
        }
        Vector2 size = area.rect.size;
        if (size == lastSize || size.x <= 0 || size.y <= 0)
        {
            return;
        }
        lastSize = size;

        float middle = size.y - headerHeight - footerHeight;

        // Name block: as big as the middle band allows, without taking over narrow screens
        float intelScale = Mathf.Min(middle / intelSize.y, size.x * maxIntelShare / intelSize.x);
        intelScale = Mathf.Clamp(intelScale, minScale, maxScale);
        intelPanel.anchorMin = intelPanel.anchorMax = Vector2.zero;
        intelPanel.pivot = new Vector2(0f, 0.5f);
        intelPanel.sizeDelta = intelSize;
        intelPanel.anchoredPosition = new Vector2(margin, footerHeight + middle / 2);
        intelPanel.localScale = new Vector3(intelScale, intelScale, 1);

        // Lineup: everything to the right of the panel
        lineup.anchorMin = Vector2.zero;
        lineup.anchorMax = Vector2.one;
        float left = margin + intelSize.x * intelScale + gap;
        lineup.offsetMin = new Vector2(left, footerHeight);
        lineup.offsetMax = new Vector2(-10, -headerHeight);

        // Keepers grow with the band's height, but keep a few in view on narrow screens
        float lineupWidth = size.x - left - 10;
        float cardScale = Mathf.Min(middle / cardSize.y, lineupWidth / (cardSize.x * minVisibleKeepers));
        cardScale = Mathf.Clamp(cardScale, minScale, maxScale);
        foreach (CharacterCard card in cards)
        {
            if (!card || !card.fit)
            {
                continue;
            }
            card.fit.sizeDelta = cardSize;
            card.fit.localScale = new Vector3(cardScale, cardScale, 1);
            LayoutElement element = card.GetComponent<LayoutElement>();
            if (element)
            {
                element.preferredWidth = cardSize.x * cardScale;
            }
        }

        if (scroller && Application.isPlaying)
        {
            scroller.Relayout();
        }
        else if (lineup.childCount > 0)
        {
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)lineup.GetChild(0));
        }
    }
}
