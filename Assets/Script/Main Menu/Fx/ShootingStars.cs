using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Every so often a small magic comet arcs across the sky, trailing fading sparks.
/// Sits behind the Stage, so it passes behind the logo and characters.
/// One comet at a time; the head and trail are a fixed set of images.
/// </summary>
public class ShootingStars : MenuFxLayer
{
    [SerializeField] private Vector2 interval = new Vector2(6f, 10f);
    [SerializeField] private float duration = 1.3f;
    [SerializeField] private int trailLength = 14;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.75f, 1f);

    private RectTransform head;
    private Image headImg;
    private Image[] trail;
    private float timer, flight = -1f;
    private Vector2 from, to;
    private float arc;

    protected override void Awake()
    {
        base.Awake();
        trail = new Image[trailLength];
        for (int i = trailLength - 1; i >= 0; i--) trail[i] = FxKit.NewImage("Trail", transform, FxKit.Glow, Color.clear);
        headImg = FxKit.NewImage("Comet", transform, FxKit.Sparkle, Color.clear);
        head = headImg.rectTransform;
        timer = 2f; // first one shortly after the menu settles
    }

    protected override void Tick(float dt)
    {
        if (flight < 0f)
        {
            timer -= dt;
            if (timer <= 0f) Launch();
            return;
        }

        flight += dt / duration;
        float h = Layer.rect.height;
        float fade = Mathf.Clamp01(flight * 6f) * Mathf.Clamp01((1.25f - flight) * 4f);

        Vector2 p = Point(Mathf.Min(flight, 1f));
        head.anchoredPosition = p;
        head.sizeDelta = Vector2.one * h * 0.075f;
        head.localRotation = Quaternion.Euler(0f, 0f, Elapsed * 360f);
        headImg.color = FxKit.WithAlpha(color, fade * (flight < 1f ? 1f : 0f));

        for (int i = 0; i < trail.Length; i++)
        {
            float k = 1f - (float)i / trail.Length;                // 1 near the head, 0 at the tail
            float t = flight - (i + 1) * 0.02f;
            var rt = trail[i].rectTransform;
            rt.anchoredPosition = Point(Mathf.Clamp01(t));
            rt.sizeDelta = Vector2.one * h * 0.055f * k;
            trail[i].color = FxKit.WithAlpha(color, t > 0f && t < 1f ? 0.7f * k * fade : 0f);
        }

        if (flight > 1.3f)
        {
            flight = -1f;
            timer = Random.Range(interval.x, interval.y);
        }
    }

    private void Launch()
    {
        // A gentle arc high in the sky, left-to-right or right-to-left.
        Rect r = Layer.rect;
        bool rightward = Random.value < 0.5f;
        float y0 = Random.Range(0.75f, 0.9f), y1 = y0 - Random.Range(0.1f, 0.2f);
        float x0 = Random.Range(0.05f, 0.3f), x1 = x0 + Random.Range(0.35f, 0.5f);
        if (!rightward) { x0 = 1f - x0; x1 = 1f - x1; }
        from = new Vector2(x0 * r.width, y0 * r.height);
        to = new Vector2(x1 * r.width, y1 * r.height);
        arc = r.height * Random.Range(0.05f, 0.1f);
        flight = 0f;
    }

    private Vector2 Point(float t) => Vector2.Lerp(from, to, t) + Vector2.up * arc * 4f * t * (1f - t);
}
