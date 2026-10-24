using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Motion for one level tile: pops in when the screen opens, squashes while pressed, punches when played,
// wiggles its padlock when a locked level is tapped, and the medal floats gently while idle.
// The guide tile (the level to play next) also breathes over a soft gold glow.
// LevelSelectFx starts the entrance and adds the sparkles.
[RequireComponent(typeof(Button))]
public class LevelTile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    public RectTransform medal;
    public RectTransform padlock;
    [Tooltip("The level to play next: breathes and glows")]
    public bool guide;
    public float bobHeight = 6f;
    public float bobSpeed = 1.6f;

    public bool Playable => button && button.interactable;
    public LevelSelectFx Fx { get; set; }

    Button button;
    CanvasGroup group;
    Image glow;
    Vector2 medalRest;
    float appear = 1;   // entrance scale (overshoots), 1 at rest
    float press;        // eased 0..1 while a finger is down
    float punch;        // scale kick when played
    bool pressed;
    float phase;

    private void Awake()
    {
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
        if (!group)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        if (medal)
        {
            medalRest = medal.anchoredPosition;
        }
        phase = transform.GetSiblingIndex() * 0.9f;
        button.onClick.AddListener(Played);
    }

    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnPointerExit(PointerEventData eventData) { pressed = false; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Playable && appear >= 1)
        {
            StartCoroutine(Shake());
        }
    }

    void Played()
    {
        StartCoroutine(Punch());
        if (Fx && medal)
        {
            Fx.Burst(medal);
        }
    }

    // Hidden until the entrance plays
    public void Hide()
    {
        appear = 0;
        group.alpha = 0;
        if (medal)
        {
            medal.localScale = Vector3.zero;
        }
        if (padlock)
        {
            padlock.localScale = Vector3.zero;
        }
    }

    // Tile pops up with a little overshoot, then the medal (and padlock) pop on top of it
    public IEnumerator Enter()
    {
        yield return FxKit.Tween(0.38f, t =>
        {
            appear = BackOut(t, 0.5f);
            group.alpha = Mathf.Clamp01(t * 3);
        });
        appear = 1;
        if (medal)
        {
            StartCoroutine(Pop(medal, 0.32f));
        }
        if (padlock)
        {
            yield return FxKit.Wait(0.08f);
            StartCoroutine(Pop(padlock, 0.3f));
        }
    }

    IEnumerator Pop(RectTransform target, float duration)
    {
        yield return FxKit.Tween(duration, t => target.localScale = Vector3.one * BackOut(t, 0.6f));
    }

    IEnumerator Punch()
    {
        yield return FxKit.Tween(0.3f, t => punch = Mathf.Sin(t * Mathf.PI) * (1 - t) * 0.14f);
        punch = 0;
    }

    // Locked: the tile shakes its head and the padlock rattles
    IEnumerator Shake()
    {
        yield return FxKit.Tween(0.4f, t =>
        {
            float wave = Mathf.Sin(t * Mathf.PI * 6) * (1 - t);
            transform.localRotation = Quaternion.Euler(0, 0, wave * 3f);
            if (padlock)
            {
                padlock.localRotation = Quaternion.Euler(0, 0, wave * 18f);
            }
        });
    }

    // Soft glow that sits behind the tile (a layout-ignored sibling, so it draws underneath)
    public void SetGlow(Image image)
    {
        glow = image;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        press = Mathf.Lerp(press, pressed && appear >= 1 ? 1 : 0, 1 - Mathf.Exp(-dt * 25f));

        float breathe = guide && Playable ? Mathf.Sin(time * 3f) * 0.025f : 0;
        transform.localScale = Vector3.one * (appear * (1 + breathe + punch) * (1 - 0.05f * press));

        if (medal && Playable)
        {
            medal.anchoredPosition = medalRest + Vector2.up * Mathf.Sin(time * bobSpeed + phase) * bobHeight;
        }
        if (glow)
        {
            RectTransform rt = glow.rectTransform;
            rt.anchoredPosition = ((RectTransform)transform).anchoredPosition;
            rt.localScale = transform.localScale;
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f);
            glow.color = FxKit.WithAlpha(glow.color, guide && Playable ? group.alpha * Mathf.Lerp(0.35f, 0.6f, pulse) : 0);
        }
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    // Ease out that overshoots past 1 then settles
    static float BackOut(float t, float overshoot)
    {
        float s = 1.70158f * overshoot * 2;
        t -= 1;
        return t * t * ((s + 1) * t + s) + 1;
    }
}
