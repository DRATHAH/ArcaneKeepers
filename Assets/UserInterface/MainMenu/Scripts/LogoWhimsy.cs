using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Playful, cartoony main-menu logo animation (Clash of Critters feel):
/// 1. The logo drops in from above, stretched tall, lands with a jelly squash,
///    hops once more and wobbles to rest while little puffs pop out at its feet.
/// 2. Idle: it floats gently up and down, gold stars twinkle around it, and every few
///    seconds it does a springy "boing" hop.
/// Animates the 'title' child (a full-stretch image), so the layout is never touched.
/// Sprites are generated at runtime, so no extra assets are needed.
/// </summary>
public class LogoWhimsy : MonoBehaviour
{
    [SerializeField] private RectTransform title;

    [Header("Intro")]
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float dropHeight = 1.6f;     // in logo heights
    [SerializeField] private float dropDuration = 0.38f;
    [SerializeField] private float landSquash = 0.24f;

    [Header("Idle")]
    [SerializeField] private float floatAmount = 0.025f;  // in logo heights
    [SerializeField] private float floatSpeed = 1.6f;
    [SerializeField] private Vector2 boingInterval = new Vector2(4.5f, 7f);
    [SerializeField] private Vector2 twinkleInterval = new Vector2(0.6f, 1.4f);
    [SerializeField] private Color starColor = new Color(1f, 0.88f, 0.4f, 1f);   // warm gold
    [SerializeField] private Color puffColor = new Color(1f, 1f, 1f, 0.85f);

    // Animation channels, combined every frame in LateUpdate.
    private float hop;                     // extra lift, in logo heights
    private Vector2 squash = Vector2.one;  // jelly scale around the logo's feet
    private float idleWeight;              // fades the float in after the intro
    private float idleTime;
    private RectTransform fxLayer;

    private static Sprite starSprite, puffSprite;

    private void Awake()
    {
        if (title == null) title = (RectTransform)transform.GetChild(0);
        // Squash around the bottom edge so the logo "sits" on something.
        // The title is stretched with zero offsets, so moving the pivot keeps its rect.
        title.pivot = new Vector2(0.5f, 0f);
        title.anchoredPosition = Vector2.zero;
        BuildFxLayer();
        hop = dropHeight * 4f; // parked off-screen until the intro starts
        Apply();
    }

    private void Start() => StartCoroutine(Play());

    private IEnumerator Play()
    {
        yield return Wait(startDelay);

        // Fall, stretched tall and thin like a cartoon.
        yield return Tween(dropDuration, t =>
        {
            float e = t * t;
            hop = Mathf.Lerp(dropHeight, 0f, e);
            float s = Mathf.Lerp(0.18f, 0.1f, t);
            squash = new Vector2(1f - s * 0.6f, 1f + s);
        });

        // Splat, then a little hop and a second softer landing.
        Puffs(7);
        yield return Jelly(0.22f, landSquash, 1);
        yield return Hop(0.32f, 0.14f);
        Puffs(4);
        yield return Jelly(0.6f, landSquash * 0.55f, 3);

        StartCoroutine(Twinkles());
        StartCoroutine(Boings());
        yield return Tween(0.8f, t => idleWeight = t * t * (3f - 2f * t));
    }

    private void Update()
    {
        if (idleWeight > 0f) idleTime += Time.unscaledDeltaTime;
    }

    private void LateUpdate() => Apply();

    private void Apply()
    {
        float h = title.rect.height;
        float bob = (Mathf.Sin(idleTime * floatSpeed) * 0.5f + 0.5f) * floatAmount * idleWeight;
        title.anchoredPosition = new Vector2(0f, (hop + bob) * h);
        title.localScale = new Vector3(squash.x, squash.y, 1f);
        title.localRotation = Quaternion.identity;
    }

    // ---- moves ----

    // Damped squash/stretch spring: starts flat and wide, then wobbles back to 1.
    private IEnumerator Jelly(float duration, float amount, int wobbles)
    {
        float freq = (wobbles + 0.5f) / duration * Mathf.PI;
        yield return Tween(duration + 0.3f, t =>
        {
            float time = t * (duration + 0.3f);
            float w = Mathf.Cos(time * freq) * Mathf.Exp(-4.5f * time / duration) * amount;
            squash = new Vector2(1f + w, 1f - w);
        });
        squash = Vector2.one;
    }

    // A parabolic hop, stretching on the way up and squashing at the top.
    private IEnumerator Hop(float duration, float height)
    {
        yield return Tween(duration, t =>
        {
            hop = 4f * height * t * (1f - t);
            float v = 1f - 2f * t; // vertical speed: stretch while moving fast
            squash = new Vector2(1f - Mathf.Abs(v) * 0.06f, 1f + Mathf.Abs(v) * 0.1f);
        });
        hop = 0f;
    }

    private IEnumerator Boings()
    {
        while (true)
        {
            yield return Wait(Random.Range(boingInterval.x, boingInterval.y));
            // Wind up (squash down), spring up, land and wobble, with a couple of stars.
            yield return Tween(0.14f, t => squash = new Vector2(1f + 0.1f * t, 1f - 0.1f * t));
            for (int i = 0; i < 3; i++) StartCoroutine(Twinkle(0.5f, true));
            yield return Hop(0.36f, 0.1f);
            Puffs(3);
            yield return Jelly(0.45f, 0.13f, 2);
        }
    }

    // ---- effects ----

    private IEnumerator Twinkles()
    {
        while (true)
        {
            yield return Wait(Random.Range(twinkleInterval.x, twinkleInterval.y));
            StartCoroutine(Twinkle(Random.Range(0.5f, 0.8f), false));
        }
    }

    // A star that pops in around the logo's edge, spins a little and fades.
    private IEnumerator Twinkle(float duration, bool big)
    {
        Rect r = fxLayer.rect;
        float size = r.height * (big ? Random.Range(0.2f, 0.26f) : Random.Range(0.12f, 0.18f));
        var img = NewFx(starSprite, size);
        var rt = img.rectTransform;
        // Pick a spot on an ellipse hugging the logo, mostly above and to the sides.
        float a = Random.Range(-0.25f, 1.25f) * Mathf.PI;
        rt.anchoredPosition = r.center + new Vector2(Mathf.Cos(a) * r.width * 0.5f, Mathf.Sin(a) * r.height * 0.48f)
                                       * Random.Range(0.85f, 1.05f);
        float spin = Random.Range(60f, 140f) * (Random.value < 0.5f ? -1f : 1f);

        yield return Tween(duration, t =>
        {
            float s = t < 0.3f ? EaseOutBack(t / 0.3f, 3f) : 1f - Smooth((t - 0.3f) / 0.7f);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            var c = starColor;
            c.a = Mathf.Clamp01(s);
            img.color = c;
        });
        Destroy(img.gameObject);
    }

    // Little round dust puffs squirting sideways from under the logo's feet.
    private void Puffs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            StartCoroutine(Puff(side, Random.Range(0.1f, 0.5f)));
        }
    }

    private IEnumerator Puff(float side, float spread)
    {
        Rect r = fxLayer.rect;
        var img = NewFx(puffSprite, r.height * Random.Range(0.1f, 0.18f));
        var rt = img.rectTransform;
        Vector2 start = new Vector2(r.center.x + side * r.width * Random.Range(0.15f, 0.35f), r.yMin + r.height * 0.06f);
        Vector2 travel = new Vector2(side * r.width * spread * 0.5f, r.height * Random.Range(0.02f, 0.12f));

        yield return Tween(Random.Range(0.4f, 0.6f), t =>
        {
            float e = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = start + travel * e;
            float s = Mathf.Lerp(0.5f, 1.2f, e);
            rt.localScale = new Vector3(s, s, 1f);
            var c = puffColor;
            c.a *= 1f - t;
            img.color = c;
        });
        Destroy(img.gameObject);
    }

    private Image NewFx(Sprite sprite, float size)
    {
        var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(fxLayer, false);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // ---- setup ----

    private void BuildFxLayer()
    {
        EnsureSprites();
        var go = new GameObject("LogoFx", typeof(RectTransform));
        fxLayer = (RectTransform)go.transform;
        fxLayer.SetParent(transform, false); // same box as the logo, drawn over the title
        fxLayer.anchorMin = Vector2.zero;
        fxLayer.anchorMax = Vector2.one;
        fxLayer.offsetMin = fxLayer.offsetMax = Vector2.zero;
    }

    private static void EnsureSprites()
    {
        if (starSprite != null) return;

        // Chunky rounded four-point star with a bright core.
        starSprite = FxKit.MakeSprite(64, (x, y) =>
        {
            float dx = Mathf.Abs(x - 0.5f) * 2f, dy = Mathf.Abs(y - 0.5f) * 2f;
            float star = Mathf.Pow(dx, 0.6f) + Mathf.Pow(dy, 0.6f);
            return Mathf.Clamp01((1.02f - star) / 0.08f);
        });

        // Soft round puff.
        puffSprite = FxKit.MakeSprite(64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f) * 2f;
            return Mathf.Clamp01((1f - d) / 0.25f);
        });
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

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private static float EaseOutBack(float t, float overshoot)
    {
        t -= 1f;
        return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
    }
}
