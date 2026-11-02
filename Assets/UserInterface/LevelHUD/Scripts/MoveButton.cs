using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The in-level MOVE button: a round gold button in the bottom-right corner (same family as the pause button).
// Tap = pick a placed Keeper up and drag it to another tile; tap again (before grabbing anyone) to cancel.
//   ready      -> gold disc, white four-way arrows, "MOVE" under it
//   armed      -> lifted, gold dashed ring turning round it, "Drag a Keeper" hint slides out to the left
//   recharging -> greyed, a dark radial sweep winds back clockwise, pops when ready again
// Pops in after the tower cards have dealt. Wires its own click, so the prefab needs no scene refs;
// RelocationManager still drives cooldownSlider / button.interactable as before.
[RequireComponent(typeof(Button))]
public class MoveButton : MonoBehaviour
{
    [Tooltip("Scales/lifts as one piece (disc, icon, label, sweep); the root stays put as the hit area.")]
    public RectTransform body;
    public Image disc;
    public Image icon;
    public TextMeshProUGUI label;
    [Tooltip("Radial-filled dark disc over the button; covers the part still recharging.")]
    public Image shade;
    [Tooltip("Gold dashed ring that turns round the button while waiting for the player to drag a Keeper.")]
    public Image ring;
    [Tooltip("'Drag a Keeper' pill to the left of the button while armed.")]
    public CanvasGroup hint;
    [Tooltip("RelocationManager writes the cooldown (1 = just used, 0 = ready) here.")]
    public Slider cooldownSlider;

    [Header("Pop in (after the 4 tower cards)")]
    public float popInDelay = 0.25f + 4 * 0.12f;
    public float popInDuration = 0.45f;

    [Header("Look")]
    public Color dimmed = new Color(0.6f, 0.6f, 0.66f, 1f);
    public float armedLift = 8f;
    public float ringSpin = 40f;
    public float hintSlide = 30f;

    /// <summary>Set by the tutorial while its dog guide stands in the bottom-right (he does the explaining).</summary>
    public static bool SuppressHint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => SuppressHint = false;

    Button button;
    RectTransform hintRt;
    Vector2 hintHome;
    float popInTime;
    float pressTime = -1f;
    float popTime = -1f;
    float armed;     // 0..1 eased
    bool wasReady = true;

    const float MaxStep = 1f / 30f;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnTap);
        if (hint)
        {
            hintRt = (RectTransform)hint.transform;
            hintHome = hintRt.anchoredPosition;
            hint.alpha = 0f;
            hint.blocksRaycasts = false;
        }
        if (ring)
        {
            ring.color = new Color(ring.color.r, ring.color.g, ring.color.b, 0f);
        }
        if (body)
        {
            body.localScale = Vector3.zero;
        }
    }

    void OnTap()
    {
        pressTime = 0f;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ButtonClick");
        }
        RelocationManager rm = RelocationManager.instance;
        if (rm == null)
        {
            return;
        }
        // Second tap while nobody is picked up = cancel
        if (rm.canRelocate && rm.relocatedObject == null)
        {
            rm.canRelocate = false;
        }
        else
        {
            rm.EnableRelocate();
        }
    }

    void Update()
    {
        RelocationManager rm = RelocationManager.instance;
        bool isArmed = rm != null && (rm.canRelocate || rm.relocatedObject != null);
        bool ready = button.interactable;
        float charge = !ready && cooldownSlider ? Mathf.Clamp01(cooldownSlider.value) : 0f;

        if (ready && !wasReady)
        {
            popTime = 0f;
        }
        wasReady = ready;

        float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
        armed = Mathf.MoveTowards(armed, isArmed ? 1f : 0f, dt / 0.15f);

        if (shade)
        {
            shade.fillAmount = charge;
        }
        Color tint = ready ? Color.white : dimmed;
        if (disc)
        {
            disc.color = tint;
        }
        if (icon)
        {
            icon.color = tint;
        }
        if (label)
        {
            label.color = tint;
        }
        if (ring)
        {
            Color c = ring.color;
            c.a = armed;
            ring.color = c;
            ring.rectTransform.Rotate(0f, 0f, -ringSpin * dt);
        }
        if (hint)
        {
            float a = SuppressHint ? 0f : armed;
            float e = 1f - (1f - a) * (1f - a);
            hint.alpha = e;
            hintRt.anchoredPosition = hintHome + new Vector2((1f - e) * hintSlide, 0f);
        }

        Animate(dt);
    }

    void Animate(float dt)
    {
        if (!body)
        {
            return;
        }
        float scale = 1f + 0.05f * armed;

        if (popInTime >= 0f)
        {
            popInTime += dt;
            float t = Mathf.Clamp01((popInTime - popInDelay) / popInDuration);
            scale *= EaseOutBack(t);
            if (t >= 1f)
            {
                popInTime = -1f;
            }
        }

        if (pressTime >= 0f)
        {
            pressTime += dt;
            float t = Mathf.Clamp01(pressTime / 0.2f);
            scale *= 1f - 0.1f * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f)
            {
                pressTime = -1f;
            }
        }

        if (popTime >= 0f)
        {
            popTime += dt;
            float t = Mathf.Clamp01(popTime / 0.3f);
            scale *= 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f)
            {
                popTime = -1f;
            }
        }

        body.anchoredPosition = new Vector2(0f, armed * armedLift);
        body.localScale = new Vector3(scale, scale, 1f);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
