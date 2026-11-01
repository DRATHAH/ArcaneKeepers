using UnityEngine;
using UnityEngine.UI;

// Chunky Plants vs. Zombies style health bar on an in-level tower card:
// heart badge + wooden bar with a cel-shaded fill that goes green -> yellow -> red.
//  - pops up when the Keeper is placed, shrinks away when it falls
//  - hit: fill drops, the lost chunk lingers as a cream "ghost" then drains, white flash, bar jolts, heart squashes
//  - heal: fill grows smoothly, heart bumps, green-white glow
//  - low health: heart beats and the fill throbs red
// Art: AgentScripts/GenerateCardHealthArt.py (fillMin/fillMax match its FILL_X, offset past the heart).
public class CardHealthBar : MonoBehaviour
{
    [Tooltip("Moves/scales as one piece (frame, fills, heart).")]
    public RectTransform body;
    public Image fill;
    [Tooltip("Lost health that lingers for a moment before draining.")]
    public Image ghost;
    [Tooltip("White overlay on the fill, flashed on hits and heals.")]
    public Image flash;
    public RectTransform heart;

    [Header("Fill range (fillAmount at 0 and full health)")]
    public float fillMin = 0.19f;
    public float fillMax = 0.946f;

    [Header("Colours")]
    public Color full = new Color(0.47f, 0.86f, 0.25f);
    public Color mid = new Color(1f, 0.80f, 0.18f);
    public Color low = new Color(0.95f, 0.26f, 0.20f);
    public Color ghostColor = new Color(1f, 0.93f, 0.70f);
    [Range(0f, 1f)] public float lowThreshold = 0.3f;

    [Header("Timing")]
    public float ghostHold = 0.35f;
    public float ghostDrainSpeed = 1.4f;

    float target = 1f;     // health fraction from the tower
    float shown = 1f;      // drawn fill (drops instantly, grows smoothly)
    float ghostValue = 1f; // trailing chunk
    float ghostWait;
    bool visible;
    float showT = 1f;      // 0 -> 1 pop in
    float hideT = -1f;     // 0 -> 1 shrink out, -1 = not hiding
    float shake;           // decaying jolt strength
    float squash = -1f;    // heart squash timer
    float bump = -1f;      // heart heal bump timer
    float flashA;
    Color flashColor = Color.white;
    float beatClock;

    const float ShowDuration = 0.35f;
    const float HideDuration = 0.2f;

    void Awake()
    {
        if (ghost) ghost.color = ghostColor;
        if (flash) flash.color = new Color(1f, 1f, 1f, 0f);
        Apply(0f);
        // Hidden until a Keeper is placed (SetShown sets visible before activating us)
        if (!visible)
        {
            gameObject.SetActive(false);
        }
    }

    // Shown while the card's Keeper is on the board
    public void SetShown(bool show)
    {
        if (show ? visible && hideT < 0f && gameObject.activeSelf : !visible)
        {
            return;
        }
        if (show)
        {
            visible = true;
            hideT = -1f;
            gameObject.SetActive(true);
            // Fresh Keeper: snap to its health, no hit/heal effects
            shown = ghostValue = target;
            showT = 0f;
            squash = bump = -1f;
            shake = flashA = 0f;
        }
        else
        {
            visible = false;
            if (gameObject.activeInHierarchy)
            {
                hideT = 0f;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void SetValue(float value)
    {
        value = Mathf.Clamp01(value);
        if (!visible || !gameObject.activeInHierarchy)
        {
            target = shown = ghostValue = value;
            return;
        }
        float delta = value - target;
        target = value;
        if (delta < -0.0001f)
        {
            // Hit: drop now, keep the lost chunk as a ghost
            if (ghostValue < shown) ghostValue = shown;
            shown = value;
            ghostWait = ghostHold;
            shake = Mathf.Min(1f, shake + 0.35f + -delta * 3f);
            squash = 0f;
            flashA = 0.85f;
            flashColor = Color.white;
        }
        else if (delta > 0.0001f)
        {
            bump = 0f;
            flashA = Mathf.Max(flashA, 0.6f);
            flashColor = new Color(0.85f, 1f, 0.75f);
        }
    }

    void Update()
    {
        // Capped so a load hitch doesn't skip the pop-in / hit animations
        Apply(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
    }

    void Apply(float dt)
    {
        // Heal grows smoothly; hits already snapped
        if (shown < target)
        {
            shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.6f, (target - shown) * 6f) * dt);
        }
        if (ghostWait > 0f)
        {
            ghostWait -= dt;
        }
        else if (ghostValue > shown)
        {
            ghostValue = Mathf.MoveTowards(ghostValue, shown, ghostDrainSpeed * dt * (0.4f + ghostValue - shown));
        }
        if (ghostValue < shown) ghostValue = shown;

        bool isLow = target <= lowThreshold && target > 0f;
        Color c = target > 0.6f ? Color.Lerp(mid, full, (target - 0.6f) / 0.4f)
                : target > lowThreshold ? Color.Lerp(low, mid, (target - lowThreshold) / (0.6f - lowThreshold))
                : low;

        // Low health heartbeat: double thump every 0.9s
        float beat = 0f;
        if (isLow)
        {
            beatClock += dt;
            float p = beatClock % 0.9f;
            beat = Pulse(p, 0f, 0.14f) + 0.6f * Pulse(p, 0.2f, 0.14f);
            c = Color.Lerp(c, Color.white, 0.25f * beat);
        }
        else
        {
            beatClock = 0f;
        }

        if (fill)
        {
            fill.fillAmount = Mathf.Lerp(fillMin, fillMax, shown);
            fill.color = c;
        }
        if (ghost)
        {
            ghost.fillAmount = Mathf.Lerp(fillMin, fillMax, ghostValue);
        }
        if (flash)
        {
            flashA = Mathf.MoveTowards(flashA, 0f, dt * 4f);
            flash.fillAmount = Mathf.Lerp(fillMin, fillMax, Mathf.Max(shown, ghostValue));
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashA);
        }

        // Body: pop in / shrink out / jolt on hit
        float scale = 1f;
        float angle = 0f;
        Vector2 pos = Vector2.zero;
        if (showT < 1f)
        {
            showT = Mathf.Min(1f, showT + dt / ShowDuration);
            scale = EaseOutBack(showT);
        }
        if (hideT >= 0f)
        {
            hideT += dt / HideDuration;
            float t = Mathf.Clamp01(hideT);
            scale = Mathf.Lerp(1f, 0f, t * t);
            angle = -8f * t;
            if (hideT >= 1f)
            {
                hideT = -1f;
                gameObject.SetActive(false);
                return;
            }
        }
        if (shake > 0.001f)
        {
            float time = Time.unscaledTime;
            pos = new Vector2(Mathf.Sin(time * 61f), Mathf.Sin(time * 47f + 1.3f) * 0.5f) * 7f * shake;
            angle += Mathf.Sin(time * 53f) * 3f * shake;
            shake = Mathf.MoveTowards(shake, 0f, dt * 3.5f);
        }
        if (body)
        {
            body.anchoredPosition = pos;
            body.localScale = new Vector3(scale, scale, 1f);
            body.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        // Heart: squash on hit, bump on heal, beat when low
        if (heart)
        {
            Vector2 hs = Vector2.one * (1f + 0.18f * beat);
            if (squash >= 0f)
            {
                squash += dt / 0.3f;
                float t = Mathf.Clamp01(squash);
                float k = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                hs = Vector2.Scale(hs, new Vector2(1f + 0.25f * k, 1f - 0.25f * k));
                if (squash >= 1f) squash = -1f;
            }
            if (bump >= 0f)
            {
                bump += dt / 0.35f;
                float t = Mathf.Clamp01(bump);
                hs *= 1f + 0.3f * Mathf.Sin(t * Mathf.PI) * (1f - 0.5f * t);
                if (bump >= 1f) bump = -1f;
            }
            float showScale = showT < 1f ? EaseOutBack(Mathf.Clamp01(showT * 1.4f - 0.4f)) : 1f;
            heart.localScale = new Vector3(hs.x * showScale, hs.y * showScale, 1f);
        }
    }

    static float Pulse(float t, float start, float length)
    {
        float x = (t - start) / length;
        return x < 0f || x > 1f ? 0f : Mathf.Sin(x * Mathf.PI);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 2.2f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
