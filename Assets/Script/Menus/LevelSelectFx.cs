using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Effects for the level select, built from the main menu's FxKit sprites. Once the screen has slid in,
// the tiles pop in one after another (gold medals give off a small sparkle as they land); playing a level
// bursts from its medal; gold medals twinkle now and then. Lives on a full-screen layer above Safe.
public class LevelSelectFx : MonoBehaviour
{
    public RectTransform row;
    public Color gold = new Color(1f, 0.86f, 0.38f);
    [Tooltip("Seconds between tiles popping in")]
    public float stagger = 0.06f;
    [Tooltip("Seconds between twinkles on the gold medals")]
    public float twinkleInterval = 1.4f;

    RectTransform layer;
    LevelTile[] tiles;
    bool entered;
    float nextTwinkle;

    private void Awake()
    {
        layer = (RectTransform)transform;
        tiles = row.GetComponentsInChildren<LevelTile>();
    }

    private IEnumerator Start()
    {
        foreach (LevelTile tile in tiles)
        {
            tile.Fx = this;
            if (tile.guide)
            {
                tile.SetGlow(CreateGlow(tile));
            }
            tile.Hide();
        }

        // The screen slides in from a picture taken right after loading: wait for it to land so the
        // tiles pop in on the live screen
        yield return null;
        while (SceneTransition.IsTransitioning)
        {
            yield return null;
        }
        yield return FxKit.Wait(0.05f);

        for (int i = 0; i < tiles.Length; i++)
        {
            StartCoroutine(EnterTile(tiles[i]));
            yield return FxKit.Wait(stagger);
        }
        entered = true;
        nextTwinkle = 1f;
    }

    IEnumerator EnterTile(LevelTile tile)
    {
        yield return tile.Enter();
        if (tile.Playable && tile.medal)
        {
            yield return FxKit.Wait(0.12f); // medal is nearly full size by now
            Measure(tile.medal, out Vector2 centre, out Vector2 size);
            StartCoroutine(Shockwave(centre, size * 1.0f, size * 1.35f, gold, 0.4f));
        }
    }

    private void Update()
    {
        if (!entered)
        {
            return;
        }
        nextTwinkle -= Time.unscaledDeltaTime;
        if (nextTwinkle > 0)
        {
            return;
        }
        nextTwinkle = twinkleInterval * Random.Range(0.7f, 1.3f);

        // A twinkle on the rim of a random gold medal
        LevelTile tile = tiles[Random.Range(0, tiles.Length)];
        if (!tile.Playable || !tile.medal)
        {
            return;
        }
        Measure(tile.medal, out Vector2 centre, out Vector2 size);
        float a = Random.Range(20f, 160f) * Mathf.Deg2Rad; // upper half, where light would catch
        Vector2 rim = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size.x * 0.42f;
        StartCoroutine(Twinkle(centre + rim, size.x * Random.Range(0.18f, 0.26f), Color.white, 0.55f));
    }

    // ---- playing a level: flash, rings, sparks from the medal ----

    public void Burst(RectTransform medal)
    {
        Measure(medal, out Vector2 centre, out Vector2 size);
        float d = size.x;
        StartCoroutine(Flash(centre, d * 1.2f, 0.22f));
        StartCoroutine(Shockwave(centre, Vector2.one * d, Vector2.one * d * 1.7f, gold, 0.5f));
        StartCoroutine(Shockwave(centre, Vector2.one * d * 0.9f, Vector2.one * d * 1.3f, Color.white, 0.3f));
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < 8; i++)
        {
            float a = (offset + i * 45f + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
            StartCoroutine(Spark(centre, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), d * Random.Range(0.6f, 0.85f), d * Random.Range(0.16f, 0.24f)));
        }
    }

    // ---- building blocks ----

    Image CreateGlow(LevelTile tile)
    {
        // A sibling under the tile in the row, ignored by the layout; LevelTile keeps it lined up
        RectTransform tileRect = (RectTransform)tile.transform;
        Image img = FxKit.NewImage("GuideGlow", tileRect.parent, BoxGlow, FxKit.WithAlpha(gold, 0));
        img.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        img.transform.SetSiblingIndex(tileRect.GetSiblingIndex());
        RectTransform rt = img.rectTransform;
        rt.anchorMin = tileRect.anchorMin;
        rt.anchorMax = tileRect.anchorMax;
        rt.sizeDelta = tileRect.rect.size / (1 - 2 * BoxGlowEdge);
        return img;
    }

    // Soft halo hugging a box: solid inside, fading out over the outer edge band
    const float BoxGlowEdge = 0.16f;
    static Sprite boxGlow;
    static Sprite BoxGlow => boxGlow ? boxGlow : boxGlow = FxKit.MakeSprite(128, (x, y) =>
    {
        float dx = Mathf.Max(0, Mathf.Abs(x - 0.5f) - (0.5f - BoxGlowEdge));
        float dy = Mathf.Max(0, Mathf.Abs(y - 0.5f) - (0.5f - BoxGlowEdge));
        float outside = Mathf.Sqrt(dx * dx + dy * dy) / BoxGlowEdge;
        return Mathf.Pow(Mathf.Clamp01(1 - outside), 2f);
    });

    IEnumerator Spark(Vector2 centre, Vector2 dir, float distance, float size)
    {
        Image img = Spawn("Spark", FxKit.Sparkle, gold, centre);
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
        Image img = Spawn("Flash", FxKit.Glow, Color.white, centre);
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
        Image img = Spawn("Shockwave", FxKit.Ring, color, centre);
        RectTransform rt = img.rectTransform;
        yield return FxKit.Tween(duration, t =>
        {
            rt.sizeDelta = Vector2.Lerp(from, to, FxKit.EaseOut(t));
            img.color = FxKit.WithAlpha(color, color.a * (1 - t));
        });
        Destroy(img.gameObject);
    }

    IEnumerator Twinkle(Vector2 pos, float size, Color color, float duration)
    {
        Image img = Spawn("Twinkle", FxKit.Sparkle, color, pos);
        RectTransform rt = img.rectTransform;
        float spin = Random.Range(-90f, 90f);
        yield return FxKit.Tween(duration, t =>
        {
            rt.sizeDelta = Vector2.one * size * Mathf.Sin(t * Mathf.PI);
            rt.localRotation = Quaternion.Euler(0, 0, spin * t);
        });
        Destroy(img.gameObject);
    }

    // A centre-anchored image on this layer; positions are relative to the layer's centre
    Image Spawn(string name, Sprite sprite, Color color, Vector2 pos)
    {
        Image img = FxKit.NewImage(name, layer, sprite, color);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.zero;
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
