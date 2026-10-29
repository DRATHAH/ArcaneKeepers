using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One keeper in the lineup: full-body art on a platform with their name underneath, and the "in your squad" highlight.
// All visual changes ease toward a target so every tap gets a smooth response.
public class CharacterCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public TowerEntry tower;
    public Button button;
    [Tooltip("Fixed-size box holding the visual, scaled to fit the screen by CharacterSelectLayout")]
    public RectTransform fit;
    [Tooltip("Scaled for focus / press feedback")]
    public RectTransform visual;
    public Image hero;
    public HeroIdle idle;
    public Image platform;
    [Tooltip("Rune circle on the pedestal (inside a squashed parent so spinning it looks flat)")]
    public Image ring;
    public Image glow;
    public TextMeshProUGUI nameText;
    public RectTransform badge;
    public TextMeshProUGUI badgeNumber;
    public CanvasGroup group;

    [HideInInspector] public float focus = 1f; // 1 = centred in the lineup, set by LineupScroller
    [HideInInspector] public float edgeFade = 1f; // 0 = scrolled off the lineup's edge, set by LineupScroller

    static readonly Color Gold = new Color(1f, 0.86f, 0.35f);

    bool selected;
    bool teamFull;
    float selectAmount;  // eased 0..1
    float dimAmount;     // eased 0..1
    float badgePop;      // 0..1, plays once when picked
    float hop;           // 0..1, a little jump when picked
    float shake;         // 0..1, plays once when the squad is full
    float appear = 1f;   // 0..1 entrance
    float appearDelay;
    float press;         // eased 0..1 while a finger is down
    Color accent = Color.white;
    Vector2 heroHome;    // where the hero's feet rest on the platform
    bool pressed;
    Graphic hitArea;

    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnPointerExit(PointerEventData eventData) { pressed = false; }

    private void Awake()
    {
        heroHome = hero.rectTransform.anchoredPosition;
        hitArea = GetComponent<Graphic>();
    }

    public void AddTower(TowerEntry newTower)
    {
        tower = newTower;
        CharacterProfile profile = newTower.tower.profile;
    }

    public void Setup(float idleOffset)
    {
        accent = tower.tower.profile.accent;
        hero.sprite = tower.tower.profile.heroSprite;
        nameText.text = tower.tower.name;
        if (idle)
        {
            idle.timeOffset = idleOffset;
        }
        Color g = tower.tower.profile.accent;
        g.a = 0;
        glow.color = g;
        ApplyVisuals();
    }

    public void SetSelected(bool value, int slotNumber, bool animate)
    {
        if (value && !selected && animate)
        {
            badgePop = 1f;
            hop = 1f;
            if (idle)
            {
                idle.Perk();
            }
        }
        selected = value;
        if (value)
        {
            badgeNumber.text = slotNumber.ToString();
        }
        if (!animate)
        {
            selectAmount = value ? 1 : 0;
            badgePop = 0;
        }
    }

    public void SetTeamFull(bool value)
    {
        teamFull = value;
    }

    // Squad is full: a short sideways "no"
    public void Reject()
    {
        shake = 1f;
    }

    public void Appear(float delay)
    {
        appear = 0;
        appearDelay = delay;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float k = 1 - Mathf.Exp(-dt * 12f);
        selectAmount = Mathf.Lerp(selectAmount, selected ? 1 : 0, k);
        dimAmount = Mathf.Lerp(dimAmount, teamFull && !selected ? 1 : 0, k);
        press = Mathf.Lerp(press, pressed ? 1 : 0, 1 - Mathf.Exp(-dt * 25f));
        badgePop = Mathf.Max(0, badgePop - dt * 3f);
        hop = Mathf.Max(0, hop - dt * 3.2f);
        shake = Mathf.Max(0, shake - dt * 2.8f);
        if (appearDelay > 0)
        {
            appearDelay -= dt;
        }
        else if (appear < 1)
        {
            appear = Mathf.Min(1, appear + dt * 2.6f);
        }
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        float f = Mathf.SmoothStep(0, 1, focus);
        float scale = Mathf.Lerp(0.8f, 1f, f) * (1 - 0.04f * press);
        float enter = 1 - Mathf.Pow(1 - appear, 3);

        visual.localScale = new Vector3(scale, scale, 1);
        float shakeX = Mathf.Sin(shake * 28f) * 14f * shake;
        visual.anchoredPosition = new Vector2(shakeX, -40f * (1 - enter));
        group.alpha = enter * edgeFade;
        // Faded-out keepers (e.g. passing behind the name) can't be tapped by accident
        if (hitArea)
        {
            hitArea.raycastTarget = edgeFade > 0.4f;
        }

        // Picked keepers do a small hop on their platform
        hero.rectTransform.anchoredPosition = heroHome + new Vector2(0, Mathf.Sin(hop * Mathf.PI) * 34f);

        // Keepers away from the middle sit a little in shadow; while the squad is full,
        // the ones not picked are pushed back further so the picked ones stand out
        float shade = Mathf.Lerp(0.72f, 1f, f) * Mathf.Lerp(1f, 0.55f, dimAmount);
        hero.color = new Color(shade, shade, shade, 1);
        platform.color = new Color(shade, shade, shade, 1);

        // Rune circle: idles faintly in the keeper's colour, flares gold and spins up once recruited
        Color rune = Color.Lerp(accent, Gold, selectAmount);
        rune.a = Mathf.Lerp(0.35f + 0.25f * f, 1f, selectAmount) * Mathf.Lerp(1f, 0.5f, dimAmount);
        ring.color = rune;
        ring.rectTransform.localScale = Vector3.one * (1 + 0.12f * selectAmount);
        ring.rectTransform.Rotate(0, 0, -Time.unscaledDeltaTime * Mathf.Lerp(8f, 40f, selectAmount));

        Color g = Color.Lerp(accent, Gold, selectAmount);
        g.a = Mathf.Lerp(0.12f, 0.45f, f) * (1 - dimAmount * 0.6f) + selectAmount * 0.2f;
        glow.color = g;

        // Name turns gold while the keeper is in the squad
        Color name = Color.Lerp(Color.white, Gold, selectAmount);
        name.a = Mathf.Lerp(0.55f, 1f, f) * Mathf.Lerp(1f, 0.6f, dimAmount);
        nameText.color = name;

        badge.gameObject.SetActive(selectAmount > 0.02f);
        float pop = badgePop > 0 ? 1 + Mathf.Sin((1 - badgePop) * Mathf.PI) * 0.3f : 1;
        badge.localScale = Vector3.one * selectAmount * pop;
    }
}
