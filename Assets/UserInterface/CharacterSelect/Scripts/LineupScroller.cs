using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Swipeable character lineup. The focused keeper stands on a spot near the lineup's left edge, right next
// to the name block. The row scrolls until its last keeper reaches the right edge and then stops, so the
// end of the lineup never leaves an empty gap; from there the focus spot itself walks right onto the last
// keepers. Scrolling is tracked as one "position" (0 = first keeper focused, one card width per keeper):
// the row moves for the first part and the spot moves for the rest.
// A released swipe glides straight to the keeper it would coast to, in one motion, and a clear swipe
// always moves at least one keeper. Keepers grow as they reach the spot and fade out near the edges.
[RequireComponent(typeof(ScrollRect))]
public class LineupScroller : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public List<CharacterCard> cards = new List<CharacterCard>();
    public float snapTime = 0.22f;
    [Tooltip("How far a released swipe coasts, in seconds of its speed")]
    public float coastTime = 0.12f;
    [Tooltip("A swipe longer than this (in card widths) always moves to the next keeper")]
    public float minSwipe = 0.12f;
    [Tooltip("How far past the left / right edge (in card widths) keepers fade out")]
    public float leftFade = 0.6f;
    public float rightFade = 0.7f;
    [Tooltip("Where the focused keeper stands: card widths from the lineup's left edge to the keeper's middle (capped at the lineup's centre)")]
    public float focusInset = 0.62f;

    public event Action<int> FocusChanged;
    public int FocusIndex { get; private set; } = -1;

    ScrollRect scroll;
    RectTransform viewport;
    RectTransform content;
    HorizontalLayoutGroup layout;
    bool dragging;
    bool snapping;
    float position;      // 0 .. (count - 1) * spacing
    float velocity;      // of position, canvas units / s
    float snapTarget;
    float dragStart;
    float lastDragTime;
    float lastWidth = -1;
    float maxRowScroll;  // how far the row itself can move before its last keeper reaches the right edge

    private void Awake()
    {
        if (scroll)
        {
            return;
        }
        scroll = GetComponent<ScrollRect>();
        viewport = scroll.viewport ? scroll.viewport : (RectTransform)transform;
        content = scroll.content;
        layout = content.GetComponent<HorizontalLayoutGroup>();
        // The ScrollRect only provides the viewport / content; this script moves the row itself
        scroll.enabled = false;
    }

    public void UpdateCards(List<CharacterCard> cardsToAdd)
    {
        if (cardsToAdd == cards || SameCards(cardsToAdd))
        {
            return;
        }
        cards.Clear();
        foreach (CharacterCard card in cardsToAdd)
        {
            cards.Add(card);
        }
        // New keepers: size them for this screen and lay the row out now, so positions are right
        // this frame instead of after the next layout pass
        CharacterSelectLayout fitter = FindAnyObjectByType<CharacterSelectLayout>();
        if (fitter)
        {
            fitter.SetCards(cards);
        }
        Awake();
        snapping = false;
        dragging = false;
        velocity = 0;
        FocusIndex = -1;
        lastWidth = -1;
        UpdatePadding();
        position = Mathf.Clamp(position, 0, MaxPosition());
        Apply();
    }

    bool SameCards(List<CharacterCard> other)
    {
        if (other == null || other.Count != cards.Count)
        {
            return false;
        }
        for (int i = 0; i < cards.Count; i++)
        {
            if (other[i] != cards[i])
            {
                return false;
            }
        }
        return true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        snapping = false;
        velocity = 0;
        dragStart = position;
        lastDragTime = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }
        // Finger movement in the lineup's own units (the canvas may be scaled)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 now);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 before);
        float dx = now.x - before.x;
        // Rubber band past either end
        if (position < 0 || position > MaxPosition())
        {
            dx *= 0.35f;
        }
        position -= dx;
        // Speed from the time between drag events (not the frame time, which can be shorter)
        float dt = Mathf.Max(Time.unscaledTime - lastDragTime, Time.unscaledDeltaTime, 0.004f);
        lastDragTime = Time.unscaledTime;
        velocity = Mathf.Lerp(velocity, -dx / dt, 0.4f);
        Apply();
    }

    // Work out where the flick would coast to and glide straight to the keeper nearest that spot
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }
        dragging = false;
        if (cards.Count == 0)
        {
            return;
        }
        float spacing = CardSpacing();
        float landing = position + velocity * coastTime;
        int target = Mathf.Clamp(Mathf.RoundToInt(landing / spacing), 0, cards.Count - 1);
        int from = Mathf.Clamp(Mathf.RoundToInt(dragStart / spacing), 0, cards.Count - 1);
        float swiped = position - dragStart;
        if (target == from && Mathf.Abs(swiped) > spacing * minSwipe)
        {
            target = Mathf.Clamp(from + (swiped > 0 ? 1 : -1), 0, cards.Count - 1);
        }
        snapTarget = target * spacing;
        snapping = true;
    }

    // Bring the given keeper onto the focus spot
    public void ScrollTo(int index, bool instant = false)
    {
        if (index < 0 || index >= cards.Count)
        {
            return;
        }
        Awake();
        UpdatePadding();
        snapTarget = index * CardSpacing();
        if (instant)
        {
            position = snapTarget;
            velocity = 0;
            snapping = false;
            Apply();
        }
        else
        {
            snapping = true;
            velocity = 0;
        }
    }

    // Card sizes changed (screen resize): recompute the padding and keep the same keeper focused
    public void Relayout()
    {
        lastWidth = -1;
        if (!scroll)
        {
            return; // not awake yet; LateUpdate pads on the first frame
        }
        if (cards.Count > 0)
        {
            ScrollTo(Mathf.Max(0, FocusIndex), true);
        }
    }

    private void LateUpdate()
    {
        if (cards.Count == 0)
        {
            return;
        }
        UpdatePadding();

        if (!dragging)
        {
            float spacing = CardSpacing();
            if (!snapping && Mathf.Abs(position - Mathf.Round(position / spacing) * spacing) > 0.5f)
            {
                // Resting between keepers (e.g. after a resize): settle on the nearest one
                snapTarget = Mathf.Clamp(Mathf.Round(position / spacing), 0, cards.Count - 1) * spacing;
                snapping = true;
            }
            if (snapping)
            {
                position = Mathf.SmoothDamp(position, snapTarget, ref velocity, snapTime, Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(position - snapTarget) < 0.5f)
                {
                    position = snapTarget;
                    velocity = 0;
                    snapping = false;
                }
            }
        }
        Apply();
    }

    // Move the row (and, past the end of the row, the focus spot) for the current position,
    // then update each keeper's focus and edge fade
    void Apply()
    {
        if (!content || cards.Count == 0)
        {
            return;
        }
        // The row moves first; once its last keeper reaches the right edge the spot moves instead.
        // Before the first keeper (rubber band) only the row moves.
        float rowScroll = position < 0 ? position : Mathf.Min(position, maxRowScroll);
        float spotShift = position - rowScroll;
        Vector2 p = content.anchoredPosition;
        p.x = -rowScroll;
        content.anchoredPosition = p;

        // Focus: 1 on the spot, fading out over one card width.
        // Edge fade: keepers dissolve as they leave the lineup instead of being cut off by a mask
        float spacing = CardSpacing();
        Rect view = viewport.rect;
        float spot = FocusX() + spotShift;
        for (int i = 0; i < cards.Count; i++)
        {
            if (!cards[i])
            {
                continue;
            }
            float x = CardX(i);
            cards[i].focus = 1 - Mathf.Clamp01(Mathf.Abs(x - spot) / spacing);
            float fromLeft = Mathf.InverseLerp(view.xMin - spacing * leftFade * 0.5f, view.xMin + spacing * leftFade, x);
            // Gone by the time the card's middle reaches the screen edge, so nothing looks sliced off
            float fromRight = Mathf.InverseLerp(view.xMax + spacing * 0.1f, view.xMax - spacing * rightFade, x);
            cards[i].edgeFade = Mathf.SmoothStep(0, 1, Mathf.Min(fromLeft, fromRight));
        }

        int focus = Mathf.Clamp(Mathf.RoundToInt(position / spacing), 0, cards.Count - 1);
        if (focus != FocusIndex)
        {
            FocusIndex = focus;
            FocusChanged?.Invoke(focus);
        }
    }

    float MaxPosition()
    {
        return Mathf.Max(0, cards.Count - 1) * CardSpacing();
    }

    // Left padding puts the first keeper on the focus spot; right padding lets the last keeper stop just
    // inside the right edge (fully visible), so the row never scrolls into empty space
    void UpdatePadding()
    {
        if (cards.Count == 0 || !cards[0])
        {
            return;
        }
        float width = viewport.rect.width;
        if (Mathf.Approximately(width, lastWidth) || width <= 0)
        {
            return;
        }
        lastWidth = width;
        float cardWidth = LayoutUtility.GetPreferredWidth((RectTransform)cards[0].transform);
        float spacing = CardSpacing();
        float spot = FocusX() - viewport.rect.xMin;
        int left = Mathf.Max(0, Mathf.RoundToInt(spot - cardWidth / 2));
        int right = Mathf.Max(0, Mathf.RoundToInt(spacing * rightFade - cardWidth / 2));
        layout.padding = new RectOffset(left, right, layout.padding.top, layout.padding.bottom);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        maxRowScroll = Mathf.Max(0, content.rect.width - width);
    }

    // The focus spot, in the viewport's space: a little in from the left edge, never past the centre
    float FocusX()
    {
        Rect view = viewport.rect;
        return view.xMin + Mathf.Min(CardSpacing() * focusInset, view.width / 2);
    }

    // A keeper's middle, in the viewport's space
    float CardX(int index)
    {
        RectTransform card = (RectTransform)cards[index].transform;
        Vector3 world = card.TransformPoint(card.rect.center);
        return viewport.InverseTransformPoint(world).x;
    }

    float CardSpacing()
    {
        if (cards.Count == 0 || !cards[0])
        {
            return 1;
        }
        return Mathf.Max(1, LayoutUtility.GetPreferredWidth((RectTransform)cards[0].transform) + layout.spacing);
    }
}
