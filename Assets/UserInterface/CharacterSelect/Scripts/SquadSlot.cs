using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One of the four round squad medallions. Empty: spinning dashed ring with a "+" (the next one to fill pulses gold).
// Filled: the keeper's portrait in a disc of their colour, a gold rim and their name; tapping it removes them.
public class SquadSlot : MonoBehaviour
{
    public Button button;
    public RectTransform content;
    public CanvasGroup emptyGroup;
    public Image emptyRing;
    public CanvasGroup filledGroup;
    [Tooltip("Round, masked backdrop the portrait sits in; flying keepers land here")]
    public Image disc;
    public Image rim;
    public Image portrait;
    public HeroIdle portraitIdle;
    public TextMeshProUGUI nameText;

    public bool IsFilled { get; private set; }

    const float SaluteTime = 0.28f;
    const float SaluteHeight = 26f;

    bool next;
    bool incoming;
    float fill;     // eased 0..1
    float pop;      // 0..1 once when a keeper lands
    float shake;
    float salute = -1; // DEPLOY send-off hop: seconds since it started (negative = waiting / off)
    bool saluting;
    Color accent = Color.white;

    public void Fill(CharacterProfile profile, bool animate)
    {
        IsFilled = true;
        incoming = false;
        portrait.sprite = profile.heroSprite;
        nameText.text = profile.tower.name;
        accent = profile.accent;
        disc.color = Color.Lerp(profile.accent, new Color(0.11f, 0.08f, 0.2f), 0.2f);
        if (animate)
        {
            pop = 1f;
            if (portraitIdle)
            {
                portraitIdle.Perk();
            }
        }
        else
        {
            fill = 1;
        }
    }

    public void Clear(bool animate)
    {
        IsFilled = false;
        incoming = false;
        if (!animate)
        {
            fill = 0;
        }
    }

    // A keeper is flying in: hold the spot
    public void Reserve()
    {
        incoming = true;
    }

    public void SetNext(bool value)
    {
        next = value;
    }

    public void Shake()
    {
        shake = 1f;
    }

    // DEPLOY: the keeper hops up and their rim flashes, after `delay` seconds (staggered across the roster)
    public void Salute(float delay)
    {
        saluting = true;
        salute = -delay;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fill = Mathf.Lerp(fill, IsFilled ? 1 : 0, 1 - Mathf.Exp(-dt * 14f));
        pop = Mathf.Max(0, pop - dt * 2.6f);
        shake = Mathf.Max(0, shake - dt * 2.8f);

        filledGroup.alpha = fill;
        filledGroup.blocksRaycasts = false;
        emptyGroup.alpha = 1 - fill;

        // The slot that will be filled next breathes gold; one with a keeper on the way glows steady
        float pulse = next || incoming ? (incoming ? 1 : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f)) : 0;
        emptyRing.color = Color.Lerp(new Color(1f, 1f, 1f, 0.5f), new Color(1f, 0.87f, 0.4f, 1f), pulse);
        emptyRing.rectTransform.localScale = Vector3.one * (1 + 0.05f * pulse);
        emptyRing.rectTransform.Rotate(0, 0, -dt * (10f + 30f * pulse));

        // Landing: a springy pop, rim flashing white before settling to gold
        float s = 1 + (pop > 0 ? Mathf.Sin((1 - pop) * Mathf.PI * 2f) * pop * 0.14f : 0);
        content.localScale = new Vector3(s, s, 1);
        // Send-off hop: quick rise, soft landing, a small squash as it lands
        float hop = 0;
        if (saluting)
        {
            salute += dt;
            float h = Mathf.Clamp01(salute / SaluteTime);
            if (salute >= 0 && h < 1)
            {
                hop = Mathf.Sin(h * Mathf.PI) * (1 - h * 0.35f);
                float squash = h > 0.8f ? Mathf.Sin((h - 0.8f) / 0.2f * Mathf.PI) * 0.06f : 0;
                s *= 1 + hop * 0.08f;
                content.localScale = new Vector3(s * (1 + squash), s * (1 - squash), 1);
                rim.color = Color.Lerp(rim.color, Color.white, Mathf.Sin(h * Mathf.PI) * 0.8f);
            }
        }
        content.anchoredPosition = new Vector2(Mathf.Sin(shake * 30f) * 10f * shake, hop * SaluteHeight);
        rim.color = Color.Lerp(new Color(1f, 0.84f, 0.32f), Color.white, pop);
        portrait.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, fill);
        nameText.color = Color.Lerp(Color.white, Color.Lerp(accent, Color.white, 0.4f), pop);
    }
}
