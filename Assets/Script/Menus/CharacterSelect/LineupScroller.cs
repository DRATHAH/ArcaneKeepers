using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Swipeable character lineup: a released swipe glides straight to the keeper it would coast to and
// settles with them centred, in one motion. Keepers get bigger as they reach the middle, fade out
// softly near the lineup's edges, and the centred one is reported as the focus.
[RequireComponent(typeof(ScrollRect))]
public class LineupScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public CharacterCard[] cards;
    [Tooltip("Below this speed (canvas units/s) the lineup settles on the nearest keeper")]
    public float snapSpeed = 600f;
    public float snapTime = 0.22f;
    [Tooltip("How far past the left / right edge (in card widths) keepers fade out")]
    public float leftFade = 0.6f;
    public float rightFade = 0.7f;

    public event Action<int> FocusChanged;
    public int FocusIndex { get; private set; } = -1;

    ScrollRect scroll;
    RectTransform viewport;
    RectTransform content;
    HorizontalLayoutGroup layout;
    bool dragging;
    bool snapping;
    float snapTarget;
    float snapVelocity;
    float lastWidth = -1;

    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        viewport = scroll.viewport ? scroll.viewport : (RectTransform)transform;
        content = scroll.content;
        layout = content.GetComponent<HorizontalLayoutGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        snapping = false;
    }

    // Instead of coasting and then snapping (two separate motions), work out where the flick would
    // coast to and glide straight to the keeper nearest that spot, keeping the flick's speed
    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        float velocity = scroll.velocity.x;
        float coast = scroll.inertia ? velocity / -Mathf.Log(Mathf.Clamp(scroll.decelerationRate, 0.001f, 0.999f)) : 0;
        float landing = content.anchoredPosition.x + coast;

        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < cards.Length; i++)
        {
            float d = Mathf.Abs(content.anchoredPosition.x + OffsetFromCentre(i) - landing);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        if (best < 0)
        {
            return;
        }
        snapTarget = content.anchoredPosition.x + OffsetFromCentre(best);
        snapping = true;
        snapVelocity = velocity;
        scroll.velocity = Vector2.zero;
    }

    // Glide the given keeper to the middle
    public void ScrollTo(int index, bool instant = false)
    {
        if (index < 0 || index >= cards.Length)
        {
            return;
        }
        UpdatePadding();
        snapTarget = content.anchoredPosition.x + OffsetFromCentre(index);
        if (instant)
        {
            SetContentX(snapTarget);
            snapping = false;
        }
        else
        {
            snapping = true;
            snapVelocity = 0;
            scroll.velocity = Vector2.zero;
        }
    }

    // Card sizes changed (screen resize): recompute the padding and keep the same keeper centred
    public void Relayout()
    {
        lastWidth = -1;
        if (!scroll)
        {
            return; // not awake yet; LateUpdate pads on the first frame
        }
        if (FocusIndex >= 0)
        {
            ScrollTo(FocusIndex, true);
        }
        else
        {
            UpdatePadding();
        }
    }

    private void LateUpdate()
    {
        UpdatePadding();

        if (!dragging && !snapping && Mathf.Abs(scroll.velocity.x) < snapSpeed)
        {
            int nearest = NearestCard();
            if (nearest >= 0 && Mathf.Abs(OffsetFromCentre(nearest)) > 0.5f)
            {
                snapTarget = content.anchoredPosition.x + OffsetFromCentre(nearest);
                snapping = true;
                snapVelocity = scroll.velocity.x;
                scroll.velocity = Vector2.zero;
            }
        }

        if (snapping && !dragging)
        {
            float x = Mathf.SmoothDamp(content.anchoredPosition.x, snapTarget, ref snapVelocity, snapTime, Mathf.Infinity, Time.unscaledDeltaTime);
            SetContentX(x);
            scroll.velocity = Vector2.zero;
            if (Mathf.Abs(x - snapTarget) < 0.5f)
            {
                SetContentX(snapTarget);
                snapping = false;
            }
        }

        // Focus: 1 in the middle, fading out over one card width.
        // Edge fade: keepers dissolve as they leave the lineup instead of being cut off by a mask
        float spacing = CardSpacing();
        Rect view = viewport.rect;
        for (int i = 0; i < cards.Length; i++)
        {
            float offset = OffsetFromCentre(i);
            cards[i].focus = 1 - Mathf.Clamp01(Mathf.Abs(offset) / spacing);
            float x = view.center.x - offset;
            float fromLeft = Mathf.InverseLerp(view.xMin - spacing * leftFade * 0.5f, view.xMin + spacing * leftFade, x);
            // Gone by the time the card's middle reaches the screen edge, so nothing looks sliced off
            float fromRight = Mathf.InverseLerp(view.xMax + spacing * 0.1f, view.xMax - spacing * rightFade, x);
            cards[i].edgeFade = Mathf.SmoothStep(0, 1, Mathf.Min(fromLeft, fromRight));
        }

        int focus = NearestCard();
        if (focus != FocusIndex && focus >= 0)
        {
            FocusIndex = focus;
            FocusChanged?.Invoke(focus);
        }
    }

    // Padding lets the first and last keepers reach the middle too
    void UpdatePadding()
    {
        float width = viewport.rect.width;
        if (Mathf.Approximately(width, lastWidth) || cards.Length == 0)
        {
            return;
        }
        lastWidth = width;
        float cardWidth = LayoutUtility.GetPreferredWidth((RectTransform)cards[0].transform);
        int pad = Mathf.Max(0, Mathf.RoundToInt(width / 2 - cardWidth / 2));
        layout.padding = new RectOffset(pad, pad, layout.padding.top, layout.padding.bottom);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // How far the content must move for this card to sit in the viewport's centre
    float OffsetFromCentre(int index)
    {
        RectTransform card = (RectTransform)cards[index].transform;
        Vector3 world = card.TransformPoint(card.rect.center);
        float x = viewport.InverseTransformPoint(world).x;
        return viewport.rect.center.x - x;
    }

    int NearestCard()
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < cards.Length; i++)
        {
            float d = Mathf.Abs(OffsetFromCentre(i));
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    float CardSpacing()
    {
        if (cards.Length == 0)
        {
            return 1;
        }
        return Mathf.Max(1, LayoutUtility.GetPreferredWidth((RectTransform)cards[0].transform) + layout.spacing);
    }

    void SetContentX(float x)
    {
        Vector2 p = content.anchoredPosition;
        p.x = x;
        content.anchoredPosition = p;
    }
}
