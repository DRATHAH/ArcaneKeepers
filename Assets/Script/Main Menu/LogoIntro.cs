using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arcane main-menu logo intro:
/// 1. A flare of magic light blooms behind the logo and the "ARCANE KEEPERS" title
///    materialises out of it (fade + settle, no bounce), with a shower of sparkles.
/// 2. A gleam of light sweeps across the lettering.
/// 3. The menu buttons pop in one after another.
/// Idle: the magic aura softly pulses, sparkles twinkle around the logo and a gleam
/// sweeps across it every few seconds. The logo itself stays still.
/// The glow/sparkle/gleam sprites are generated at runtime, so no extra assets are needed.
/// </summary>
public class LogoIntro : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform[] revealAfterLanding; // e.g. Start & Quit buttons

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.25f;
    [SerializeField] private float titlePopDuration = 0.7f;
    [SerializeField] private float buttonDelay = 0.15f;
    [SerializeField] private float buttonStagger = 0.12f;

    [Header("Magic")]
    [SerializeField] private Color auraColor = new Color(1f, 0.82f, 0.35f, 1f);   // warm magic gold
    [SerializeField] private Color sparkleColor = new Color(1f, 0.97f, 0.8f, 1f);  // pale gold
    [SerializeField] private float auraIdleAlpha = 0.6f;
    [SerializeField] private float gleamInterval = 5f;
    [SerializeField] private Vector2 sparkleInterval = new Vector2(0.35f, 0.9f);
    [SerializeField] private int introSparkles = 10;

    private CanvasGroup titleGroup;
    private Image aura;
    private RectTransform gleam;
    private RectTransform sparkleLayer;
    private bool idle;

    private static Sprite glowSprite, sparkleSprite, gleamSprite;

    private void Awake()
    {
        BuildMagicLayers();

        // Hide everything until the intro plays.
        titleGroup.alpha = 0f;
        SetAuraAlpha(0f);
        foreach (var r in revealAfterLanding)
            if (r != null) r.localScale = Vector3.zero;
    }

    private void Start() => StartCoroutine(Play());

    private IEnumerator Play()
    {
        yield return Wait(startDelay);

        // 1) Magic flare behind the logo, and the title materialises out of it.
        StartCoroutine(Tween(0.9f, t =>
        {
            // Quick flare up, then settle down to the idle glow.
            float a = t < 0.3f ? Mathf.SmoothStep(0f, 1f, t / 0.3f)
                               : Mathf.Lerp(1f, auraIdleAlpha, Mathf.SmoothStep(0f, 1f, (t - 0.3f) / 0.7f));
            SetAuraAlpha(a);
            float s = Mathf.Lerp(0.6f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            aura.rectTransform.localScale = new Vector3(s, s, 1f);
        }));

        yield return Wait(0.12f);
        for (int i = 0; i < introSparkles; i++)
            StartCoroutine(Sparkle(Random.Range(0.5f, 0.9f), Random.Range(0f, 0.5f)));

        yield return Tween(titlePopDuration, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            titleGroup.alpha = Mathf.Clamp01(t * 1.6f);
            float s = Mathf.Lerp(1.12f, 1f, e);
            title.localScale = new Vector3(s, s, 1f);
        });
        titleGroup.alpha = 1f;
        title.localScale = Vector3.one;

        // 2) Gleam across the lettering.
        StartCoroutine(Gleam());

        yield return Wait(buttonDelay);

        // 3) Buttons pop in one after another.
        foreach (var r in revealAfterLanding)
        {
            if (r == null) continue;
            StartCoroutine(PopIn(r, 0.35f));
            yield return Wait(buttonStagger);
        }

        idle = true;
        StartCoroutine(IdleSparkles());
        StartCoroutine(IdleGleams());
    }

    private void Update()
    {
        if (!idle) return;
        // Slow "breathing" of the magic aura (only the glow changes, the logo stays put).
        float p = Mathf.Sin(Time.unscaledTime * 1.6f) * 0.5f + 0.5f;
        SetAuraAlpha(Mathf.Lerp(auraIdleAlpha * 0.6f, auraIdleAlpha * 1.15f, p));
    }

    // ---- magic effects ----

    private IEnumerator IdleSparkles()
    {
        while (true)
        {
            yield return Wait(Random.Range(sparkleInterval.x, sparkleInterval.y));
            StartCoroutine(Sparkle(Random.Range(0.35f, 0.65f), 0f));
        }
    }

    private IEnumerator IdleGleams()
    {
        while (true)
        {
            yield return Wait(gleamInterval);
            yield return Gleam();
        }
    }

    private IEnumerator Gleam()
    {
        Rect r = title.rect;
        gleam.sizeDelta = new Vector2(r.height * 0.5f, r.height * 1.8f);
        float from = r.xMin - r.height, to = r.xMax + r.height;
        yield return Tween(0.8f, t =>
        {
            float x = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            gleam.anchoredPosition = new Vector2(x, r.center.y);
        });
        gleam.anchoredPosition = new Vector2(from, r.center.y);
    }

    private IEnumerator Sparkle(float duration, float delay)
    {
        yield return Wait(delay);

        var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(sparkleLayer, false);
        var img = go.GetComponent<Image>();
        img.sprite = sparkleSprite;
        img.raycastTarget = false;

        // Somewhere over / just around the logo.
        Rect r = sparkleLayer.rect;
        rt.anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax) * 1.05f,
                                          Random.Range(r.yMin, r.yMax) * 1.1f);
        float size = Mathf.Min(r.width, r.height) * Random.Range(0.2f, 0.34f);
        rt.sizeDelta = new Vector2(size, size);
        float spin = Random.Range(-90f, 90f);

        yield return Tween(duration, t =>
        {
            float s = Mathf.Sin(t * Mathf.PI); // grow in, shrink out
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            var c = sparkleColor;
            c.a = s;
            img.color = c;
        });
        Destroy(go);
    }

    private void SetAuraAlpha(float a)
    {
        var c = auraColor;
        c.a = a;
        aura.color = c;
    }

    // ---- setup ----

    private void BuildMagicLayers()
    {
        EnsureSprites();
        var parent = (RectTransform)title.parent;

        // Aura glow behind the title.
        var auraGo = new GameObject("MagicAura", typeof(RectTransform), typeof(Image));
        var auraRt = (RectTransform)auraGo.transform;
        auraRt.SetParent(parent, false);
        auraRt.SetSiblingIndex(title.GetSiblingIndex());
        MatchArea(auraRt, title, 1.35f, 1.8f);
        aura = auraGo.GetComponent<Image>();
        aura.sprite = glowSprite;
        aura.raycastTarget = false;

        // The title masks a diagonal gleam so it only lights up the lettering.
        titleGroup = title.GetComponent<CanvasGroup>();
        if (titleGroup == null) titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        var mask = title.GetComponent<Mask>();
        if (mask == null) mask = title.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var gleamGo = new GameObject("Gleam", typeof(RectTransform), typeof(Image));
        gleam = (RectTransform)gleamGo.transform;
        gleam.SetParent(title, false);
        gleam.anchorMin = gleam.anchorMax = new Vector2(0.5f, 0.5f);
        gleam.localRotation = Quaternion.Euler(0f, 0f, -20f);
        gleam.anchoredPosition = new Vector2(-100000f, 0f); // parked off-logo until the first sweep
        var gImg = gleamGo.GetComponent<Image>();
        gImg.sprite = gleamSprite;
        gImg.color = new Color(1f, 1f, 1f, 0.75f);
        gImg.raycastTarget = false;

        // Sparkles draw on top of the title.
        var layerGo = new GameObject("Sparkles", typeof(RectTransform));
        sparkleLayer = (RectTransform)layerGo.transform;
        sparkleLayer.SetParent(parent, false);
        sparkleLayer.SetSiblingIndex(title.GetSiblingIndex() + 1);
        MatchArea(sparkleLayer, title, 1f, 1f);
    }

    // Anchor 'rt' over the same area as 'target', grown by (sx, sy) around its centre,
    // so it follows the logo when the layout resizes.
    private static void MatchArea(RectTransform rt, RectTransform target, float sx, float sy)
    {
        Vector2 c = (target.anchorMin + target.anchorMax) * 0.5f;
        Vector2 half = (target.anchorMax - target.anchorMin) * 0.5f;
        half.x *= sx;
        half.y *= sy;
        rt.anchorMin = c - half;
        rt.anchorMax = c + half;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.Scale(target.offsetMin, new Vector2(sx, sy));
        rt.offsetMax = Vector2.Scale(target.offsetMax, new Vector2(sx, sy));
    }

    private static void EnsureSprites()
    {
        if (glowSprite != null) return;

        // Soft radial glow.
        glowSprite = MakeSprite(128, 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - d);
            return a * a;
        });

        // Four-pointed star with a soft core.
        sparkleSprite = MakeSprite(64, 64, (x, y) =>
        {
            float dx = Mathf.Abs(x - 0.5f) * 2f, dy = Mathf.Abs(y - 0.5f) * 2f;
            float star = Mathf.Clamp01(1f - (Mathf.Sqrt(dx) + Mathf.Sqrt(dy)) * 0.9f);
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.5f);
            return Mathf.Clamp01(star * 1.6f + core);
        });

        // Vertical band, bright in the middle, soft at the sides and ends.
        gleamSprite = MakeSprite(32, 64, (x, y) =>
        {
            float across = 1f - Mathf.Abs(x - 0.5f) * 2f;
            float along = 1f - Mathf.Abs(y - 0.5f) * 2f;
            return across * across * Mathf.Clamp01(along * 3f);
        });
    }

    private static Sprite MakeSprite(int w, int h, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[y * w + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / w, (y + 0.5f) / h));
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
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
