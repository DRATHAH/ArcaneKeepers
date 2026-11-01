using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Card-count bar for one tower: how many copies the player owns out of what the next level costs.
// The fill glides to its new value and reads MAX at the top level. Once an upgrade is affordable the bar
// itself becomes the level-up control: it turns green, reads LEVEL UP!, a shine sweeps across it, little
// arrows rise out of it and the card at its end turns into a still gold arrow-up gem (which is tappable too).
// Tapping squashes the bar and pushes the gem in, the fill drains into the level badge while a wave of arrows
// shoots up behind it, and the gem pops (or shrinks away if no upgrade is left).
public class TowerProgressBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform fill;
    public Image fillImage;
    public Sprite fillSprite;
    [Tooltip("Fill used while an upgrade is affordable")]
    public Sprite readySprite;
    public TextMeshProUGUI countText;
    [Tooltip("Optional: soft glow behind the bar that breathes while an upgrade is affordable")]
    public Image readyGlow;
    [Tooltip("Optional: level number on a badge at the bar's left end")]
    public RectTransform levelBadge;
    public TextMeshProUGUI levelText;

    [Header("Level up (optional)")]
    [Tooltip("Shown on the bar instead of the count while an upgrade is affordable")]
    public TextMeshProUGUI readyText;
    [Tooltip("Card at the bar's right end; swaps for the gem while ready")]
    public GameObject cardIcon;
    public RectTransform upGem;
    [Tooltip("Soft streak inside the well (which masks it) that sweeps across while ready")]
    public RectTransform shine;
    [Tooltip("Rising arrow sprite")]
    public Sprite arrowSprite;
    public Color arrowColor = new Color32(200, 255, 150, 255);

    const float DrainTime = 0.3f;
    const float CelebrateTime = 0.75f;

    float shown;        // eased fill 0..1
    float target;
    bool ready;
    float badgePop;     // 1 right after a level up, eases to 0
    float readyIn;      // 0..1 as the ready look pops in
    bool pressed;
    float press;        // eased 0..1 while held
    float nextArrow;
    float drain;        // seconds left of the fill draining into the badge
    float celebrate;    // 1 right after a level up, eases to 0 (gem hop + flip, label punch)

    class Arrow { public Image image; public float age, life, speed, drift, delay; public bool burst; }
    readonly List<Arrow> arrows = new List<Arrow>();

    public bool Ready => ready;

    private void Awake()
    {
        // The gem reads as the button, so it has to catch taps too (it hangs off the bar's end);
        // its clicks bubble up to the Button on the bar.
        if (upGem && upGem.TryGetComponent(out Image gemImage))
        {
            gemImage.raycastTarget = true;
            // The keeper lineup (drawn later) overlaps the gem's right edge and would swallow taps there,
            // so the gem gets its own sort order one above its canvas, for drawing and for taps.
            // (Applied in Apply() each time the gem shows: Unity drops overrideSorting set while it's inactive.)
            if (!upGem.TryGetComponent(out gemCanvas))
            {
                gemCanvas = upGem.gameObject.AddComponent<Canvas>();
                upGem.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    Canvas gemCanvas;

    void LiftGem()
    {
        if (!gemCanvas || gemCanvas.overrideSorting)
        {
            return;
        }
        Canvas root = gemCanvas.rootCanvas;
        gemCanvas.overrideSorting = true;
        gemCanvas.sortingLayerID = root.sortingLayerID;
        gemCanvas.sortingOrder = root.sortingOrder + 1;
    }

    public void Set(TowerEntry entry, bool animate)
    {
        if (Inventory.IsEmpty(entry))
        {
            return;
        }
        bool max = RarityCalculation.IsMaxLevel(entry);
        int cost = max ? 0 : RarityCalculation.GetCardReqs(entry);
        target = max ? 1 : Mathf.Clamp01((float)entry.amount / cost);
        bool wasReady = ready;
        ready = !max && entry.amount >= cost;
        if (ready && (!wasReady || !animate))
        {
            readyIn = animate ? 0 : 1;
        }
        countText.text = max ? "MAX" : entry.amount + "/" + cost;
        if (levelText)
        {
            levelText.text = entry.level.ToString();
        }
        if (!animate)
        {
            shown = target;
            drain = 0;
            celebrate = 0;
        }
        Apply();
    }

    public void LevelUp()
    {
        badgePop = 1;
        celebrate = 1;
        drain = DrainTime;   // the spent cards drain into the badge, then the bar refills to what's left
        pressed = false;
        // A wave of arrows shoots up behind the draining edge, right to left
        const int count = 8;
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(0.12f, 0.88f, i / (count - 1f));
            Launch(x, true, (1 - x) * DrainTime * 0.9f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = ready;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressed = false;
    }

    private void OnDisable()
    {
        pressed = false;
        press = 0;
        drain = 0;
        celebrate = 0;
        foreach (Arrow a in arrows)
        {
            a.image.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        drain = Mathf.Max(0, drain - dt);
        shown = drain > 0
            ? Mathf.Lerp(shown, 0, 1 - Mathf.Exp(-dt * 16f))
            : Mathf.Lerp(shown, target, 1 - Mathf.Exp(-dt * 9f));
        badgePop = Mathf.Max(0, badgePop - dt * 2.5f);
        celebrate = Mathf.Max(0, celebrate - dt / CelebrateTime);
        readyIn = ready ? Mathf.Min(1, readyIn + dt * 3f) : 0;
        press = Mathf.Lerp(press, pressed ? 1 : 0, 1 - Mathf.Exp(-dt * 20f));

        if (ready && drain <= 0 && arrowSprite && Time.unscaledTime >= nextArrow)
        {
            nextArrow = Time.unscaledTime + Random.Range(0.35f, 0.55f);
            Launch(Random.Range(0.15f, 0.85f), false, 0);
        }
        TickArrows(dt);
        Apply();
    }

    void Apply()
    {
        // Keep the green "ready" look while the spent fill drains, even if no further upgrade is affordable
        bool green = ready || drain > 0;
        fill.anchorMax = new Vector2(shown, 1);
        fillImage.enabled = shown > 0.03f;
        fillImage.sprite = green ? readySprite : fillSprite;
        float t = Time.unscaledTime;
        if (readyGlow)
        {
            // Steady glow while ready (user: slider still, no pulsing); flares on level up
            float glow = ready ? 0.55f : 0;
            readyGlow.color = FxKit.WithAlpha(readyGlow.color, Mathf.Max(glow, celebrate));
        }

        // The bar holds still while ready and only squashes while held. Round children (badge, gem, label)
        // are counter-scaled so they keep their shape instead of turning into ovals.
        float sx = 1 + press * 0.03f;
        float sy = 1 - press * 0.08f;
        transform.localScale = new Vector3(sx, sy, 1);

        if (levelBadge)
        {
            float s = badgePop > 0 ? 1 + Mathf.Sin((1 - badgePop) * Mathf.PI) * 0.35f : 1;
            levelBadge.localScale = new Vector3(s / sx, s / sy, 1);
        }

        // Ready: the label swaps in with a pop and then holds still; it punches again on level up
        bool readyLook = green && readyText;
        countText.gameObject.SetActive(!readyLook);
        if (readyText)
        {
            readyText.gameObject.SetActive(readyLook);
            if (readyLook)
            {
                float pop = 1 + Mathf.Sin(readyIn * Mathf.PI) * 0.3f + Mathf.Sin(celebrate * Mathf.PI) * 0.2f;
                float s = Mathf.Lerp(0.5f, 1, FxKit.EaseOut(ready ? readyIn : 1)) * pop;
                readyText.rectTransform.localScale = new Vector3(s / sx, s / sy, 1);
            }
        }

        bool gemOn = upGem && (ready || celebrate > 0);
        if (cardIcon)
        {
            cardIcon.SetActive(!gemOn);
        }
        if (upGem)
        {
            upGem.gameObject.SetActive(gemOn);
            if (gemOn)
            {
                LiftGem();
                float k = 1 - celebrate;   // progress through the level-up celebration (1 when idle)
                float grow = ready ? FxKit.EaseOut(readyIn) : 1;
                // No upgrade left: shrink away over the last part of the celebration
                float leave = ready ? 1 : Mathf.Clamp01(celebrate / 0.35f);
                // Holds still while waiting (user asked); only reacts to the press and the level up
                float s = Mathf.Lerp(0.2f, 1, grow) * leave;
                s *= 1 - press * 0.15f;                                  // pushed in while held
                s *= 1 + Mathf.Sin(k * Mathf.PI) * 0.3f;                 // pop on level up (0 when idle)
                upGem.localScale = new Vector3(s / sx, s / sy, 1);
                upGem.anchoredPosition = new Vector2(upGem.anchoredPosition.x, 4);
                upGem.localRotation = Quaternion.identity;
            }
        }
        if (shine)
        {
            shine.gameObject.SetActive(ready);
            if (ready)
            {
                // Sweeps left to right, then rests off the bar until the next one
                float w = ((RectTransform)shine.parent).rect.width;
                float k = Mathf.Repeat(t, 1.8f) / 1.1f;
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.6f, w * 0.6f, Mathf.Clamp01(k)), 0);
            }
        }
    }

    // An arrow rising off the bar's top edge at fraction x of its width; burst arrows are bigger and faster
    void Launch(float x, bool burst, float delay)
    {
        if (!arrowSprite)
        {
            return;
        }
        Arrow a = arrows.Find(o => !o.image.gameObject.activeSelf);
        if (a == null)
        {
            GameObject go = new GameObject("RisingArrow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            a = new Arrow { image = go.GetComponent<Image>() };
            a.image.sprite = arrowSprite;
            a.image.raycastTarget = false;
            a.image.preserveAspect = true;
            arrows.Add(a);
        }
        RectTransform rt = a.image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(x, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = Vector2.one * (burst ? Random.Range(38f, 44f) : Random.Range(24f, 30f));
        rt.localScale = Vector3.zero;
        a.age = 0;
        a.delay = delay;
        a.burst = burst;
        a.life = burst ? Random.Range(0.55f, 0.7f) : Random.Range(0.8f, 1f);
        a.speed = burst ? Random.Range(170f, 210f) : Random.Range(60f, 80f);
        a.drift = burst ? 0 : Random.Range(-6f, 6f);
        a.image.color = FxKit.WithAlpha(arrowColor, 0);
        a.image.gameObject.SetActive(true);
        // Above the fill, under the label, badge and gem
        rt.SetSiblingIndex(fill.parent.GetSiblingIndex() + 1);
    }

    void TickArrows(float dt)
    {
        foreach (Arrow a in arrows)
        {
            if (!a.image.gameObject.activeSelf)
            {
                continue;
            }
            if (a.delay > 0)
            {
                a.delay -= dt;
                continue;
            }
            a.age += dt;
            float k = a.age / a.life;
            if (k >= 1)
            {
                a.image.gameObject.SetActive(false);
                continue;
            }
            RectTransform rt = a.image.rectTransform;
            // Burst arrows shoot up fast and ease off; ambient ones float at a steady pace
            float speed = a.burst ? a.speed * (1 - k * 0.7f) : a.speed;
            rt.anchoredPosition += new Vector2(a.drift, speed) * dt;
            // Burst arrows flash white as they appear, then settle to the arrow colour
            Color c = a.burst ? Color.Lerp(Color.white, arrowColor, Mathf.Clamp01(k * 4f)) : arrowColor;
            a.image.color = FxKit.WithAlpha(c, Mathf.Min(1, k * 6f) * (1 - k * k));
            float s = a.burst ? 1 + Mathf.Sin(Mathf.Min(1, k * 3f) * Mathf.PI) * 0.25f : Mathf.Lerp(0.6f, 1, Mathf.Min(1, k * 4f));
            s *= Mathf.Min(1, k * 8f);
            rt.localScale = new Vector3(s, s, 1);
        }
    }
}
