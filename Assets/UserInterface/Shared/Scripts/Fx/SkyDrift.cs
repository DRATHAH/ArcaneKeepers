using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Living sky behind the character select: a few cel-shaded clouds drift slowly across the top of
/// the painting (bigger = nearer = a bit faster, wrapping round), and every so often a small flock
/// of birds glides over the mountains. Sits just above the Background image, behind all the UI.
/// All art is drawn in code.
/// </summary>
public class SkyDrift : MenuFxLayer
{
    [Header("Clouds")]
    [SerializeField] private int cloudCount = 4;
    [SerializeField] private Vector2 cloudHeight = new Vector2(0.075f, 0.11f);  // share of layer height
    [SerializeField] private Vector2 cloudBand = new Vector2(0.73f, 0.83f);     // vertical band (share of height), under the banner
    [SerializeField] private Vector2 cloudSpeed = new Vector2(0.005f, 0.011f);  // share of height per second
    [SerializeField] private float cloudAlpha = 0.95f;

    [Header("Birds")]
    [SerializeField] private Vector2 flockInterval = new Vector2(6f, 11f);
    [SerializeField] private Vector2Int flockSize = new Vector2Int(3, 5);
    [SerializeField] private Vector2 birdBand = new Vector2(0.62f, 0.78f);
    [SerializeField] private float birdSize = 0.038f;          // wingspan, share of height
    [SerializeField] private float flightTime = 14f;           // seconds to cross the screen
    [SerializeField] private Color birdColor = new Color(0.16f, 0.22f, 0.36f, 0.75f);

    private static readonly Color CloudTop = new Color(1f, 1f, 1f);
    private static readonly Color CloudUnder = new Color(0.72f, 0.83f, 0.97f);
    private static Sprite[] cloudSprites;
    private static Sprite birdSprite;

    private class Cloud { public RectTransform rect; public float x, y, h, speed; }
    private class Bird { public Image img; public Vector2 offset; public float phase, flapRate; }

    private Cloud[] clouds;
    private Bird[] birds;
    private int flockActive;
    private float flockTimer, flockT = -1f, flockDir, flockY, flockDrift;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { cloudSprites = null; birdSprite = null; }

    protected override void Awake()
    {
        base.Awake();
        // Generated sprites can be unloaded on a scene change while the array survives, so test the entry too
        if (cloudSprites == null || !cloudSprites[0])
            cloudSprites = new[] { MakeCloud(ShapeA), MakeCloud(ShapeB), MakeCloud(ShapeC) };
        if (!birdSprite) birdSprite = MakeBird();

        clouds = new Cloud[cloudCount];
        for (int i = 0; i < cloudCount; i++)
        {
            var img = FxKit.NewImage("Cloud", transform, cloudSprites[i % cloudSprites.Length], Color.clear);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (i % 2 == 1) img.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // mirror for variety
            float depth = Random.value;
            clouds[i] = new Cloud
            {
                rect = img.rectTransform,
                // spread evenly across the width, alternating high/low, so they never start in a clump
                x = (i + 0.5f + Random.Range(-0.15f, 0.15f)) / cloudCount,
                y = Mathf.Lerp(cloudBand.x, cloudBand.y, i % 2 == 0 ? Random.Range(0.6f, 1f) : Random.Range(0f, 0.4f)),
                h = Mathf.Lerp(cloudHeight.x, cloudHeight.y, depth),
                speed = Mathf.Lerp(cloudSpeed.x, cloudSpeed.y, depth),
            };
            // far clouds are a touch hazier (bluer, fainter) so they sit back in the sky
            img.color = FxKit.WithAlpha(Color.Lerp(new Color(0.9f, 0.95f, 1f), Color.white, depth),
                                        cloudAlpha * Mathf.Lerp(0.8f, 1f, depth));
        }
        // Nearer clouds draw on top
        System.Array.Sort(clouds, (a, b) => a.h.CompareTo(b.h));
        foreach (var c in clouds) c.rect.SetAsLastSibling();

        birds = new Bird[flockSize.y];
        for (int i = 0; i < birds.Length; i++)
        {
            var img = FxKit.NewImage("Bird", transform, birdSprite, Color.clear);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            birds[i] = new Bird { img = img };
        }
        flockTimer = Random.Range(2f, 4f); // first flock soon after arriving
    }

    protected override void Tick(float dt)
    {
        Rect r = Layer.rect;
        float h = r.height, w = r.width;

        // ---- clouds ----
        foreach (var c in clouds)
        {
            float cw = c.h * h * CloudAspect;
            float margin = cw * 0.5f / w;
            c.x += c.speed * h / w * dt;
            if (c.x > 1f + margin)
            {
                c.x = -margin;
                c.y = Mathf.Lerp(cloudBand.x, cloudBand.y, Random.value);
            }
            c.rect.sizeDelta = new Vector2(cw, c.h * h);
            c.rect.anchoredPosition = new Vector2(c.x * w, c.y * h + Mathf.Sin(Elapsed * 0.2f + c.speed * 400f) * h * 0.004f);
        }

        // ---- birds ----
        if (flockT < 0f)
        {
            flockTimer -= dt;
            if (flockTimer <= 0f) LaunchFlock();
            return;
        }

        flockT += dt / flightTime;
        float span = birdSize * h;
        float fade = Mathf.Clamp01(flockT * 8f) * Mathf.Clamp01((1f - flockT) * 8f);
        float lead = Mathf.Lerp(-0.08f, 1.08f, flockT);
        float leadX = (flockDir > 0f ? lead : 1f - lead) * w;
        float leadY = (flockY + flockDrift * flockT + Mathf.Sin(flockT * Mathf.PI * 2f) * 0.01f) * h;

        for (int i = 0; i < birds.Length; i++)
        {
            var b = birds[i];
            if (i >= flockActive) { b.img.color = Color.clear; continue; }
            var rt = b.img.rectTransform;
            rt.sizeDelta = new Vector2(span, span * 0.5f);
            rt.anchoredPosition = new Vector2(leadX - flockDir * b.offset.x * span, leadY + b.offset.y * span);
            // Flap in bursts, then glide with wings held out
            float cycle = Mathf.Repeat(Elapsed * 0.35f + b.phase, 1f);
            float flap = cycle < 0.55f ? 0.5f + 0.5f * Mathf.Sin(Elapsed * b.flapRate + b.phase * 10f) : 0.85f;
            rt.localScale = new Vector3(flockDir, Mathf.Lerp(0.45f, 1f, flap), 1f);
            b.img.color = FxKit.WithAlpha(birdColor, birdColor.a * fade);
        }

        if (flockT >= 1f)
        {
            flockT = -1f;
            flockTimer = Random.Range(flockInterval.x, flockInterval.y);
            foreach (var b in birds) b.img.color = Color.clear;
        }
    }

    private void LaunchFlock()
    {
        flockActive = Random.Range(flockSize.x, flockSize.y + 1);
        flockDir = Random.value < 0.5f ? 1f : -1f;
        flockY = Mathf.Lerp(birdBand.x, birdBand.y, Random.value);
        flockDrift = Random.Range(-0.04f, 0.04f);
        for (int i = 0; i < flockActive; i++)
        {
            // Loose V: alternate sides behind the leader, with a little jitter
            int rank = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            birds[i].offset = new Vector2(rank * 1.4f + Random.Range(-0.3f, 0.3f),
                                          side * rank * 0.8f + Random.Range(-0.2f, 0.2f));
            birds[i].phase = Random.value;
            birds[i].flapRate = Random.Range(9f, 12f);
        }
        flockT = 0f;
    }

    // ---- art ----

    private const int CloudW = 256, CloudH = 160;
    private const float CloudAspect = (float)CloudW / CloudH;

    // Union of circles: (x share of width, y and radius as shares of height), cut flat at CloudBase
    // so each reads as a cumulus with a flat bottom like the ones painted in the background.
    private const float CloudBase = 0.18f;
    private static readonly Vector3[] ShapeA =
    {
        new Vector3(0.20f, 0.30f, 0.16f), new Vector3(0.36f, 0.46f, 0.25f), new Vector3(0.56f, 0.56f, 0.33f),
        new Vector3(0.74f, 0.42f, 0.22f), new Vector3(0.86f, 0.30f, 0.13f),
    };
    private static readonly Vector3[] ShapeB =
    {
        new Vector3(0.18f, 0.30f, 0.14f), new Vector3(0.40f, 0.52f, 0.31f), new Vector3(0.62f, 0.46f, 0.26f),
        new Vector3(0.82f, 0.32f, 0.15f),
    };
    private static readonly Vector3[] ShapeC =
    {
        new Vector3(0.16f, 0.28f, 0.12f), new Vector3(0.32f, 0.40f, 0.21f), new Vector3(0.50f, 0.58f, 0.34f),
        new Vector3(0.70f, 0.48f, 0.26f), new Vector3(0.86f, 0.32f, 0.14f),
    };

    private static Sprite MakeCloud(Vector3[] shape)
    {
        var tex = new Texture2D(CloudW, CloudH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float baseY = CloudBase * CloudH;
        var body = new float[CloudW * CloudH];
        for (int y = 0; y < CloudH; y++)
        for (int x = 0; x < CloudW; x++)
        {
            float a = 0f;
            foreach (var c in shape)
                a = Mathf.Max(a, Coverage(x, y, c.x * CloudW, c.y * CloudH, c.z * CloudH));
            body[y * CloudW + x] = a * Mathf.Clamp01((y + 0.5f - baseY) / 1.5f + 0.5f);   // flat bottom
        }

        // Cel shade: a pixel is lit when the cloud continues a little below (and right of) it, so the
        // blue band follows the underside of every puff and runs along the flat bottom.
        int dy = Mathf.RoundToInt(0.15f * CloudH), dx = Mathf.RoundToInt(0.04f * CloudW);
        var px = new Color32[CloudW * CloudH];
        for (int y = 0; y < CloudH; y++)
        for (int x = 0; x < CloudW; x++)
        {
            int sx = Mathf.Min(x + dx, CloudW - 1), sy = y - dy;
            float lit = sy >= 0 ? body[sy * CloudW + sx] : 0f;
            Color col = Color.Lerp(CloudUnder, CloudTop, lit);
            col.a = body[y * CloudW + x];
            px[y * CloudW + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, CloudW, CloudH), new Vector2(0.5f, 0.5f), 100f);
    }

    // Gull silhouette: two wings rising from the body and drooping at the tips, white so it can be tinted.
    private static Sprite MakeBird()
    {
        const int W = 96, H = 48;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f;         // 0 at the body, 1 at the wing tip
            float v = (y + 0.5f) / H;
            float centre = 0.30f + 0.95f * u - 0.70f * u * u;        // wing line
            float thick = Mathf.Lerp(0.16f, 0.03f, u);
            float a = Mathf.Clamp01((thick - Mathf.Abs(v - centre)) * H / 1.5f + 0.5f);
            if (u > 0.97f) a *= Mathf.Clamp01((1f - u) / 0.03f);
            px[y * W + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Coverage(int x, int y, float cx, float cy, float r)
    {
        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
        return Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) / 1.5f + 0.5f);
    }
}
