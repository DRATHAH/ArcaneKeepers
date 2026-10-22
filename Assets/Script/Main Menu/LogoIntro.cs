using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu logo intro in the style of Supercell games (Clash Royale / Brawl Stars):
/// 1. The logo drops in from huge and tilted and SLAMS onto the screen.
/// 2. Impact: squash-and-stretch spring, a short screen shake, a white flash on the
///    lettering, a shockwave ring and a burst of sparkles flying outward.
/// 3. A shine sweeps across the lettering, then the menu buttons pop in.
/// Idle: the logo gently breathes, sparkles twinkle around it and a shine sweeps
/// across it every few seconds.
/// The ring/sparkle/shine sprites are generated at runtime, so no extra assets are needed.
/// </summary>
public class LogoIntro : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform[] revealAfterLanding; // e.g. Start & Quit buttons
    [Tooltip("Shaken on impact. Defaults to the logo's parent (the menu background).")]
    [SerializeField] private RectTransform shakeTarget;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.25f;
    [SerializeField] private float dropDuration = 0.28f;
    [SerializeField] private float buttonDelay = 0.2f;
    [SerializeField] private float buttonStagger = 0.12f;

    [Header("Slam")]
    [SerializeField] private float dropScale = 2.4f;
    [SerializeField] private float dropTilt = -8f;
    [SerializeField] private float squash = 0.16f;
    [SerializeField] private float shakeStrength = 14f;
    [SerializeField] private int burstSparkles = 14;

    [Header("Idle")]
    [SerializeField] private float breatheAmount = 0.015f;
    [SerializeField] private float breatheSpeed = 2.2f;
    [SerializeField] private float gleamInterval = 4f;
    [SerializeField] private Vector2 sparkleInterval = new Vector2(0.5f, 1.2f);
    [SerializeField] private Color sparkleColor = new Color(1f, 0.97f, 0.8f, 1f);  // pale gold
    [SerializeField] private Color ringColor = new Color(1f, 0.9f, 0.55f, 1f);     // warm gold

    private CanvasGroup titleGroup;
    private Image flash;
    private Image ring;
    private RectTransform gleam;
    private RectTransform sparkleLayer;
    private bool idle;
    private float idleStart;

    private static Sprite ringSprite, sparkleSprite, gleamSprite;

    private void Awake()
    {
        if (shakeTarget == null) shakeTarget = title.parent.parent as RectTransform;
        BuildMagicLayers();

        // Hide everything until the intro plays.
        titleGroup.alpha = 0f;
        foreach (var r in revealAfterLanding)
            if (r != null) r.localScale = Vector3.zero;
    }

    private void Start() => StartCoroutine(Play());

    private IEnumerator Play()
    {
        yield return Wait(startDelay);

        // 1) Drop in: huge + tilted -> full size, accelerating into the impact.
        yield return Tween(dropDuration, t =>
        {
            float e = t * t * t;
            titleGroup.alpha = Mathf.Clamp01(t * 2.5f);
            float s = Mathf.Lerp(dropScale, 1f, e);
            title.localScale = new Vector3(s, s, 1f);
            title.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(dropTilt, 0f, e));
        });
        titleGroup.alpha = 1f;
        title.localRotation = Quaternion.identity;

        // 2) Impact.
        StartCoroutine(Shake(0.3f));
        StartCoroutine(Flash(0.3f));
        StartCoroutine(Shockwave(0.5f));
        for (int i = 0; i < burstSparkles; i++)
            StartCoroutine(BurstSparkle(i * Mathf.PI * 2f / burstSparkles + Random.Range(-0.2f, 0.2f)));

        // Squash wide and flat, then spring back with a couple of wobbles.
        yield return Tween(0.6f, t =>
        {
            float w = Mathf.Cos(t * Mathf.PI * 2f * 2.2f) * Mathf.Exp(-5.5f * t) * squash;
            title.localScale = new Vector3(1f + w, 1f - w, 1f);
        });
        title.localScale = Vector3.one;

        // 3) Shine across the lettering, then the buttons.
        StartCoroutine(Gleam());
        yield return Wait(buttonDelay);
        foreach (var r in revealAfterLanding)
        {
            if (r == null) continue;
            StartCoroutine(PopIn(r, 0.35f));
            yield return Wait(buttonStagger);
        }

        idle = true;
        idleStart = Time.unscaledTime;
        StartCoroutine(IdleSparkles());
        StartCoroutine(IdleGleams());
    }

    private void Update()
    {
        if (!idle) return;
        // Gentle breathing, starting from rest so there's no jump after the slam.
        float s = 1f + Mathf.Sin((Time.unscaledTime - idleStart) * breatheSpeed) * breatheAmount;
        title.localScale = new Vector3(s, s, 1f);
    }

    // ---- impact effects ----

    private IEnumerator Shake(float duration)
    {
        if (shakeTarget == null) yield break;
        Vector2 home = shakeTarget.anchoredPosition;
        Vector3 homeScale = shakeTarget.localScale;
        // Zoom in slightly while shaking so the screen edges never show.
        float zoom = 1f + shakeStrength * 2.5f / Mathf.Max(1f, Mathf.Min(shakeTarget.rect.width, shakeTarget.rect.height));
        yield return Tween(duration, t =>
        {
            float a = shakeStrength * (1f - t) * (1f - t);
            shakeTarget.anchoredPosition = home + new Vector2(Random.Range(-a, a), Random.Range(-a, a));
            shakeTarget.localScale = homeScale * Mathf.Lerp(zoom, 1f, t);
        });
        shakeTarget.anchoredPosition = home;
        shakeTarget.localScale = homeScale;
    }

    private IEnumerator Flash(float duration)
    {
        yield return Tween(duration, t => flash.color = new Color(1f, 1f, 1f, 0.85f * (1f - t) * (1f - t)));
        flash.color = Color.clear;
    }

    private IEnumerator Shockwave(float duration)
    {
        yield return Tween(duration, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            float s = Mathf.Lerp(0.5f, 1.6f, e);
            ring.rectTransform.localScale = new Vector3(s, s, 1f);
            var c = ringColor;
            c.a = 0.9f * (1f - t);
            ring.color = c;
        });
        ring.color = Color.clear;
    }

    // A sparkle shot outward from the logo's centre at the given angle.
    private IEnumerator BurstSparkle(float angle)
    {
        Rect r = sparkleLayer.rect;
        var img = NewSparkle(Mathf.Min(r.width, r.height) * Random.Range(0.14f, 0.26f));
        var rt = img.rectTransform;
        var dir = new Vector2(Mathf.Cos(angle) * r.width * 0.6f, Mathf.Sin(angle) * r.height * 0.75f);
        float distance = Random.Range(0.7f, 1.05f);
        float spin = Random.Range(-180f, 180f);

        yield return Tween(Random.Range(0.45f, 0.7f), t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            rt.anchoredPosition = r.center + dir * Mathf.Lerp(0.3f, distance, e);
            float s = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            var c = sparkleColor;
            c.a = s;
            img.color = c;
        });
        Destroy(img.gameObject);
    }

    // ---- idle effects ----

    private IEnumerator IdleSparkles()
    {
        while (true)
        {
            yield return Wait(Random.Range(sparkleInterval.x, sparkleInterval.y));
            StartCoroutine(Twinkle(Random.Range(0.35f, 0.65f)));
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
        gleam.sizeDelta = new Vector2(r.height * 0.45f, r.height * 1.8f);
        float from = r.xMin - r.height, to = r.xMax + r.height;
        yield return Tween(0.55f, t =>
        {
            float x = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            gleam.anchoredPosition = new Vector2(x, r.center.y);
        });
        gleam.anchoredPosition = new Vector2(from, r.center.y);
    }

    // A sparkle that grows and shrinks in place somewhere over the logo.
    private IEnumerator Twinkle(float duration)
    {
        Rect r = sparkleLayer.rect;
        var img = NewSparkle(Mathf.Min(r.width, r.height) * Random.Range(0.18f, 0.3f));
        var rt = img.rectTransform;
        rt.anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax) * 1.05f,
                                          Random.Range(r.yMin, r.yMax) * 1.1f);
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
        Destroy(img.gameObject);
    }

    private Image NewSparkle(float size)
    {
        var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(sparkleLayer, false);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sparkleSprite;
        img.raycastTarget = false;
        return img;
    }

    // ---- setup ----

    private void BuildMagicLayers()
    {
        EnsureSprites();
        var parent = (RectTransform)title.parent;

        // Shockwave ring behind the title.
        var ringGo = new GameObject("Shockwave", typeof(RectTransform), typeof(Image));
        var ringRt = (RectTransform)ringGo.transform;
        ringRt.SetParent(parent, false);
        ringRt.SetSiblingIndex(title.GetSiblingIndex());
        MatchArea(ringRt, title, 1.1f, 1.6f);
        ring = ringGo.GetComponent<Image>();
        ring.sprite = ringSprite;
        ring.color = Color.clear;
        ring.raycastTarget = false;

        // The title masks the flash and the shine so they only light up the lettering.
        titleGroup = title.GetComponent<CanvasGroup>();
        if (titleGroup == null) titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        var mask = title.GetComponent<Mask>();
        if (mask == null) mask = title.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        var flashRt = (RectTransform)flashGo.transform;
        flashRt.SetParent(title, false);
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = flashRt.offsetMax = Vector2.zero;
        flash = flashGo.GetComponent<Image>();
        flash.color = Color.clear;
        flash.raycastTarget = false;

        var gleamGo = new GameObject("Gleam", typeof(RectTransform), typeof(Image));
        gleam = (RectTransform)gleamGo.transform;
        gleam.SetParent(title, false);
        gleam.anchorMin = gleam.anchorMax = new Vector2(0.5f, 0.5f);
        gleam.localRotation = Quaternion.Euler(0f, 0f, -20f);
        gleam.anchoredPosition = new Vector2(-100000f, 0f); // parked off-logo until the first sweep
        var gImg = gleamGo.GetComponent<Image>();
        gImg.sprite = gleamSprite;
        gImg.color = new Color(1f, 1f, 1f, 0.8f);
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
        if (ringSprite != null) return;

        // Soft ring, brightest on its outer edge.
        ringSprite = MakeSprite(128, 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f) * 2f;
            if (d > 1f) return 0f;
            float edge = Mathf.Clamp01((1f - d) / 0.06f);       // anti-aliased outer rim
            float body = Mathf.Clamp01((d - 0.62f) / 0.36f);    // fades out towards the centre
            return edge * body * body;
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
