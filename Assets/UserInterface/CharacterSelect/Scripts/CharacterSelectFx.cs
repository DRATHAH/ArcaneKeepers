using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Small one-shot effects for the character select, all built from the main menu's FxKit sprites:
// a light column + rune ripple when a keeper is recruited, a sparkle trail behind the flying portrait,
// a burst when they land in their medallion, a soft puff when they leave, and DEPLOY sparkling once ready.
// Lives on a full-screen layer under the FlyLayer; CharacterSelectScreen calls into it.
public class CharacterSelectFx : MonoBehaviour
{
    public Color gold = new Color(1f, 0.86f, 0.38f);
    [Tooltip("Seconds between trail sparkles behind a flying keeper")]
    public float trailInterval = 0.03f;
    [Tooltip("Seconds between twinkles around DEPLOY while the squad is ready")]
    public float deployTwinkleInterval = 0.55f;

    RectTransform layer;
    RectTransform deploy;
    bool deployReady;
    float nextTrail;
    float nextTwinkle;

    private void Awake()
    {
        layer = (RectTransform)transform;
    }

    private void Update()
    {
        if (!deployReady || !deploy)
        {
            return;
        }
        nextTwinkle -= Time.unscaledDeltaTime;
        if (nextTwinkle > 0)
        {
            return;
        }
        nextTwinkle = deployTwinkleInterval * Random.Range(0.7f, 1.3f);

        // A twinkle somewhere along the button's edge
        Measure(deploy, out Vector2 centre, out Vector2 size);
        Vector2 half = size * 0.5f;
        Vector2 p = Random.value < 0.5f
            ? new Vector2(Random.Range(-half.x, half.x), Random.value < 0.5f ? -half.y : half.y)
            : new Vector2(Random.value < 0.5f ? -half.x : half.x, Random.Range(-half.y, half.y));
        StartCoroutine(Twinkle(layer, centre + p * 0.92f, size.y * Random.Range(0.16f, 0.24f), Color.white, 0.5f));
    }

    // ---- recruit: column of light rising off the platform, the rune ripples out, a few motes float up ----

    public void Recruit(CharacterCard card, Color accent)
    {
        Transform visual = card.visual;
        int heroIndex = card.hero.transform.GetSiblingIndex();
        Color tint = Color.Lerp(gold, accent, 0.25f);

        Image beam = Spawn("RecruitBeam", visual, FxKit.Glow, Color.clear, new Vector2(0, 150), Vector2.zero);
        beam.rectTransform.anchorMin = beam.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        beam.rectTransform.pivot = new Vector2(0.5f, 0.08f);
        beam.transform.SetSiblingIndex(heroIndex);
        StartCoroutine(Beam(beam, tint));

        Image ripple = Spawn("RecruitRipple", visual, FxKit.Ring, Color.clear, new Vector2(0, 152), Vector2.zero);
        ripple.rectTransform.anchorMin = ripple.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        ripple.transform.SetSiblingIndex(heroIndex);
        StartCoroutine(Ripple(ripple, tint));

        for (int i = 0; i < 6; i++)
        {
            StartCoroutine(RisingMote(visual, tint, i / 6f));
        }
    }

    IEnumerator Beam(Image img, Color color)
    {
        RectTransform rt = img.rectTransform;
        yield return FxKit.Tween(0.65f, t =>
        {
            rt.sizeDelta = new Vector2(Mathf.Lerp(300, 160, t), 600 * FxKit.EaseOut(Mathf.Min(1, t * 2.2f)));
            img.color = FxKit.WithAlpha(color, 0.7f * (1 - FxKit.Smooth(t)));
        });
        Destroy(img.gameObject);
    }

    IEnumerator Ripple(Image img, Color color)
    {
        RectTransform rt = img.rectTransform;
        yield return FxKit.Tween(0.55f, t =>
        {
            float w = Mathf.Lerp(150, 420, FxKit.EaseOut(t));
            rt.sizeDelta = new Vector2(w, w * 0.24f); // lies flat like the rune circle
            img.color = FxKit.WithAlpha(color, 1 - t);
        });
        Destroy(img.gameObject);
    }

    IEnumerator RisingMote(Transform parent, Color color, float slot)
    {
        yield return FxKit.Wait(slot * 0.25f);
        float x = Mathf.Lerp(-110, 110, (slot + Random.Range(-0.06f, 0.06f) + 0.08f) % 1f);
        float rise = Random.Range(220, 360);
        float size = Random.Range(18, 30);
        float phase = Random.Range(0f, 6f);
        Image img = Spawn("RecruitMote", parent, FxKit.Sparkle, Color.clear, Vector2.zero, Vector2.zero);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        yield return FxKit.Tween(Random.Range(0.8f, 1.1f), t =>
        {
            rt.anchoredPosition = new Vector2(x + Mathf.Sin(t * 5f + phase) * 10f, 170 + rise * FxKit.EaseOut(t));
            rt.sizeDelta = Vector2.one * size * Mathf.Sin(t * Mathf.PI);
            img.color = FxKit.WithAlpha(color, Mathf.Sin(t * Mathf.PI));
        });
        Destroy(img.gameObject);
    }

    // ---- flight trail ----

    public void Trail(RectTransform flyer)
    {
        if (Time.unscaledTime < nextTrail)
        {
            return;
        }
        nextTrail = Time.unscaledTime + trailInterval;
        Measure(flyer, out Vector2 centre, out Vector2 size);
        Vector2 jitter = Vector2.Scale(Random.insideUnitCircle, size * 0.25f);
        StartCoroutine(Twinkle(layer, centre + jitter, Random.Range(28f, 44f), gold, 0.45f));
    }

    // ---- landing in the medallion: flash, ring, sparks ----

    public void Land(RectTransform disc, Color accent)
    {
        Measure(disc, out Vector2 centre, out Vector2 size);
        float d = Mathf.Max(size.x, size.y);
        Color tint = Color.Lerp(gold, accent, 0.2f);
        StartCoroutine(Flash(centre, d * 1.1f, 0.2f));
        StartCoroutine(Shockwave(centre, new Vector2(d, d) * 1.0f, new Vector2(d, d) * 1.6f, tint, 0.5f));
        StartCoroutine(Shockwave(centre, new Vector2(d, d) * 0.9f, new Vector2(d, d) * 1.3f, Color.white, 0.3f));
        Sparks(centre, d * 0.75f, d * 0.2f, 8, tint);
    }

    // ---- leaving the squad: a soft little puff ----

    public void Puff(RectTransform disc)
    {
        Measure(disc, out Vector2 centre, out Vector2 size);
        float d = Mathf.Max(size.x, size.y);
        Color smoke = new Color(0.85f, 0.88f, 1f);
        StartCoroutine(Shockwave(centre, new Vector2(d, d) * 0.7f, new Vector2(d, d) * 1.15f, FxKit.WithAlpha(smoke, 0.5f), 0.3f));
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < 6; i++)
        {
            float a = (offset + i * 60f) * Mathf.Deg2Rad;
            StartCoroutine(PuffMote(centre, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), d, smoke));
        }
    }

    IEnumerator PuffMote(Vector2 centre, Vector2 dir, float d, Color color)
    {
        Image img = Spawn("PuffMote", layer, FxKit.Glow, Color.clear, centre, Vector2.zero);
        RectTransform rt = img.rectTransform;
        float size = d * Random.Range(0.18f, 0.26f);
        yield return FxKit.Tween(0.4f, t =>
        {
            rt.anchoredPosition = centre + dir * d * Mathf.Lerp(0.3f, 0.65f, FxKit.EaseOut(t)) + Vector2.up * 12f * t;
            rt.sizeDelta = Vector2.one * size * Mathf.Lerp(0.8f, 1.3f, t);
            img.color = FxKit.WithAlpha(color, 0.55f * (1 - t));
        });
        Destroy(img.gameObject);
    }

    // ---- DEPLOY ----

    // Squad just filled up: burst around the button. While ready, it keeps twinkling now and then.
    public void SetDeployReady(RectTransform button, bool ready, bool burst)
    {
        deploy = button;
        deployReady = ready;
        nextTwinkle = 0.6f;
        if (!ready || !burst)
        {
            return;
        }
        Measure(button, out Vector2 centre, out Vector2 size);
        StartCoroutine(Shockwave(centre, size * 1.0f, size * 1.35f, gold, 0.5f));
        Sparks(centre, size.y * 1.0f, size.y * 0.24f, 10, gold);
    }

    // ---- building blocks ----

    void Sparks(Vector2 centre, float distance, float size, int count, Color color)
    {
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float a = (offset + i * 360f / count + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
            StartCoroutine(Spark(centre, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), distance * Random.Range(0.8f, 1.1f), size * Random.Range(0.8f, 1.2f), color));
        }
    }

    IEnumerator Spark(Vector2 centre, Vector2 dir, float distance, float size, Color color)
    {
        Image img = Spawn("Spark", layer, FxKit.Sparkle, color, centre, Vector2.zero);
        RectTransform rt = img.rectTransform;
        float spin = Random.Range(-180f, 180f);
        yield return FxKit.Tween(Random.Range(0.45f, 0.6f), t =>
        {
            rt.anchoredPosition = centre + dir * distance * FxKit.EaseOut(t) + Vector2.up * distance * 0.15f * t * t;
            float s = size * (t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f);
            rt.sizeDelta = new Vector2(s, s);
            rt.localRotation = Quaternion.Euler(0, 0, spin * t);
        });
        Destroy(img.gameObject);
    }

    IEnumerator Flash(Vector2 centre, float size, float duration)
    {
        Image img = Spawn("Flash", layer, FxKit.Glow, Color.white, centre, Vector2.zero);
        RectTransform rt = img.rectTransform;
        yield return FxKit.Tween(duration, t =>
        {
            rt.sizeDelta = Vector2.one * size * Mathf.Lerp(0.6f, 1.1f, FxKit.EaseOut(t));
            img.color = FxKit.WithAlpha(Color.white, 0.8f * (1 - t));
        });
        Destroy(img.gameObject);
    }

    IEnumerator Shockwave(Vector2 centre, Vector2 from, Vector2 to, Color color, float duration)
    {
        Image img = Spawn("Shockwave", layer, FxKit.Ring, color, centre, from);
        RectTransform rt = img.rectTransform;
        yield return FxKit.Tween(duration, t =>
        {
            rt.sizeDelta = Vector2.Lerp(from, to, FxKit.EaseOut(t));
            img.color = FxKit.WithAlpha(color, color.a * (1 - t));
        });
        Destroy(img.gameObject);
    }

    IEnumerator Twinkle(Transform parent, Vector2 pos, float size, Color color, float duration)
    {
        Image img = Spawn("Twinkle", parent, FxKit.Sparkle, color, pos, Vector2.zero);
        RectTransform rt = img.rectTransform;
        float spin = Random.Range(-90f, 90f);
        yield return FxKit.Tween(duration, t =>
        {
            rt.anchoredPosition = pos + Vector2.down * 10f * t;
            rt.sizeDelta = Vector2.one * size * Mathf.Sin(t * Mathf.PI);
            rt.localRotation = Quaternion.Euler(0, 0, spin * t);
        });
        Destroy(img.gameObject);
    }

    // A centre-anchored image; positions are relative to the parent's centre
    static Image Spawn(string name, Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        Image img = FxKit.NewImage(name, parent, sprite, color);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    // Where a rect is on this layer (centre relative to the layer's centre) and how big it looks
    void Measure(RectTransform target, out Vector2 centre, out Vector2 size)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 a = layer.InverseTransformPoint(corners[0]);
        Vector2 b = layer.InverseTransformPoint(corners[2]);
        centre = (a + b) / 2 - layer.rect.center;
        size = b - a;
    }
}
