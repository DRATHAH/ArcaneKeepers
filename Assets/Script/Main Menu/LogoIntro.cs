using System.Collections;
using UnityEngine;

/// <summary>
/// Cartoony main-menu logo intro:
/// 1. The "ARCANE KEEPERS" title pops in with an overshoot and a little squash & stretch.
/// 2. The menu buttons pop in one after another.
/// 3. The logo then idles with a gentle float.
/// </summary>
public class LogoIntro : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform shakeRoot;    // usually this logo object
    [SerializeField] private RectTransform[] revealAfterLanding; // e.g. Start & Quit buttons

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.25f;
    [SerializeField] private float titlePopDuration = 0.45f;
    [SerializeField] private float buttonDelay = 0.15f;
    [SerializeField] private float buttonStagger = 0.12f;

    [Header("Feel")]
    [SerializeField] private float idleFloatAmount = 4f;

    private Vector2 rootRestPos;
    private bool idle;
    private float idleTime;

    private void Awake()
    {
        if (shakeRoot == null) shakeRoot = (RectTransform)transform;
        rootRestPos = shakeRoot.anchoredPosition;

        // Hide everything until the intro plays.
        title.localScale = Vector3.zero;
        foreach (var r in revealAfterLanding)
            if (r != null) r.localScale = Vector3.zero;
    }

    private void Start() => StartCoroutine(Play());

    private IEnumerator Play()
    {
        yield return Wait(startDelay);

        // 1) Title pops in.
        yield return Tween(titlePopDuration, t =>
        {
            float s = EaseOutBack(t, 2.2f);
            float stretch = Mathf.Sin(t * Mathf.PI) * 0.12f;
            title.localScale = new Vector3(s * (1f - stretch), s * (1f + stretch), 1f);
        });
        title.localScale = Vector3.one;

        yield return Wait(buttonDelay);

        // 2) Buttons pop in one after another.
        foreach (var r in revealAfterLanding)
        {
            if (r == null) continue;
            StartCoroutine(PopIn(r, 0.35f));
            yield return Wait(buttonStagger);
        }

        idle = true;
    }

    private void Update()
    {
        if (!idle) return;
        idleTime += Time.unscaledDeltaTime;
        shakeRoot.anchoredPosition = rootRestPos + Vector2.up * Mathf.Sin(idleTime * 1.6f) * idleFloatAmount;
    }

    private IEnumerator PopIn(RectTransform r, float duration)
    {
        yield return Tween(duration, t =>
        {
            float s = EaseOutBack(t, 2.5f);
            r.localScale = new Vector3(s, s, 1f);
        });
        r.localScale = Vector3.one;
    }

    // ---- helpers (unscaled time so it works even if the game is paused) ----

    private static IEnumerator Wait(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
    }

    private static IEnumerator Tween(float duration, System.Action<float> step)
    {
        float time = 0f;
        while (time < duration)
        {
            step(time / duration);
            yield return null;
            time += Time.unscaledDeltaTime;
        }
        step(1f);
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        t -= 1f;
        return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
    }
}
