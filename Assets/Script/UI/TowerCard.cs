using UnityEngine;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Plants vs. Zombies style seed-packet animation for a TowerButton card:
// cards deal in from the left one after another, darken while held / placed,
// a dark shade drains from the bottom up while recharging, and the card pops + glows when ready again.
[RequireComponent(typeof(TowerButton))]
public class TowerCard : MonoBehaviour
{
    [Tooltip("Moves as one piece (art, shade, health bar); the root stays put for the layout group.")]
    public RectTransform card;
    [Tooltip("The card art; keeps the sprite's shape inside the cell.")]
    public AspectRatioFitter artFitter;
    [Tooltip("Filled image (card sprite, black) covering the part still recharging.")]
    public Image shade;
    [Tooltip("Soft glow behind the card that flashes when it's ready again.")]
    public Image readyGlow;

    [Header("Deal in")]
    public float dealDelay = 0.25f;
    public float dealStagger = 0.12f;
    public float dealDuration = 0.4f;
    public float dealDistance = 420f;

    [Header("Look")]
    public Color dimmed = new Color(0.42f, 0.42f, 0.46f, 1f);
    public Color held = new Color(0.62f, 0.62f, 0.66f, 1f);

    TowerButton button;
    Image art;
    float dealTime = -1f;
    bool holding;
    bool wasPlaced;
    bool wasCharging;
    float popTime = -1f;
    float shakeTime = -1f;

    const float PopDuration = 0.35f;
    const float ShakeDuration = 0.3f;

    void Awake()
    {
        button = GetComponent<TowerButton>();
        art = button.icon;
        if (readyGlow)
        {
            readyGlow.color = new Color(readyGlow.color.r, readyGlow.color.g, readyGlow.color.b, 0f);
        }
        // Start off screen so the first frame doesn't flash the card in place
        card.anchoredPosition = new Vector2(-dealDistance, 0f);
    }

    void Start()
    {
        dealTime = 0f;
    }

    public void SetArt(Sprite sprite)
    {
        // Can be called before Awake (e.g. on an inactive card)
        if (art == null)
        {
            art = GetComponent<TowerButton>().icon;
        }
        art.sprite = sprite;
        if (shade)
        {
            shade.sprite = sprite;
        }
        if (sprite && artFitter)
        {
            artFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }
    }

    // Called when the player picks the card up to drag a tower out
    public void OnPicked()
    {
        holding = true;
    }

    void Update()
    {
        if (holding && Touch.activeTouches.Count == 0)
        {
            holding = false;
        }

        bool placed = button.createdObject != null;
        float charge = button.CooldownFraction;
        bool charging = charge > 0f;

        // Placed tower fell -> little shake as the card starts recharging
        if (wasPlaced && !placed && charging)
        {
            shakeTime = 0f;
        }
        // Finished recharging -> ready pop
        if (wasCharging && !charging && !placed)
        {
            popTime = 0f;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound("ButtonPop");
            }
        }
        wasPlaced = placed;
        wasCharging = charging;

        if (shade)
        {
            shade.fillAmount = placed ? 0f : charge;
        }
        art.color = placed || charging ? dimmed : holding ? held : Color.white;

        Animate();
    }

    void Animate()
    {
        float dt = Time.unscaledDeltaTime;
        Vector2 pos = Vector2.zero;
        float scale = holding ? 0.94f : 1f;
        float angle = 0f;

        if (dealTime >= 0f)
        {
            dealTime += dt;
            float t = Mathf.Clamp01((dealTime - dealDelay - dealStagger * transform.GetSiblingIndex()) / dealDuration);
            pos.x = Mathf.LerpUnclamped(-dealDistance, 0f, EaseOutBack(t));
            angle = Mathf.Lerp(-10f, 0f, EaseOutBack(t));
            if (t >= 1f)
            {
                dealTime = -1f;
            }
        }

        if (popTime >= 0f)
        {
            popTime += dt;
            float t = Mathf.Clamp01(popTime / PopDuration);
            scale *= 1f + 0.12f * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
            if (readyGlow)
            {
                Color c = readyGlow.color;
                c.a = Mathf.Sin(t * Mathf.PI);
                readyGlow.color = c;
            }
            if (t >= 1f)
            {
                popTime = -1f;
            }
        }

        if (shakeTime >= 0f)
        {
            shakeTime += dt;
            float t = Mathf.Clamp01(shakeTime / ShakeDuration);
            pos.x += Mathf.Sin(t * Mathf.PI * 6f) * 10f * (1f - t);
            if (t >= 1f)
            {
                shakeTime = -1f;
            }
        }

        // Ease toward the held/normal scale so picking up and releasing feel soft
        float current = card.localScale.x;
        if (popTime < 0f)
        {
            scale = Mathf.Lerp(current, scale, 1f - Mathf.Exp(-18f * dt));
        }

        card.anchoredPosition = pos;
        card.localScale = new Vector3(scale, scale, 1f);
        card.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
