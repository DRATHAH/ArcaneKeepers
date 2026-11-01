using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sunlight shimmering on the river in the character select background. Lives as a stretched
/// child of the Background image, so its points (shares of the painting) stay on the river however
/// the background is cropped. Soft horizontal light streaks (like the white dashes painted on the
/// water) open up, drift gently downstream and close again; now and then a small sparkle winks on
/// the wide part of the river. Everything spawns inside the measured water rows, never on the banks.
/// </summary>
public class RiverGlints : MenuFxLayer
{
    [SerializeField] private int streakCount = 9;
    [SerializeField] private int sparkleCount = 2;
    [SerializeField] private Vector2 streakLife = new Vector2(1.4f, 2.4f);
    [SerializeField] private Vector2 sparkleLife = new Vector2(0.8f, 1.2f);
    [SerializeField] private float streakThickness = 0.0045f;           // share of the painting's height
    [SerializeField] private float sparkleSize = 0.016f;
    [SerializeField] private float drift = 0.006f;                       // downstream travel over a life (height share)
    [SerializeField] private Color streakColor = new Color(0.92f, 1f, 1f, 0.85f);
    [SerializeField] private Color sparkleColor = new Color(1f, 1f, 0.94f, 0.95f);

    // Water rows in Green.png, measured from the river's pixels and shrunk away from the banks:
    // (left x, right x, y), all shares of the image, y from the bottom. Far end (source) first,
    // near end (just above the castle wall) last.
    private static readonly Vector3[] Water =
    {
        new Vector3(0.4859f, 0.4926f, 0.5186f), new Vector3(0.4844f, 0.4926f, 0.5146f),
        new Vector3(0.4851f, 0.4978f, 0.5106f), new Vector3(0.4940f, 0.5045f, 0.5066f),
        new Vector3(0.4948f, 0.5089f, 0.5027f), new Vector3(0.4963f, 0.5208f, 0.4987f),
        new Vector3(0.4963f, 0.5208f, 0.4947f), new Vector3(0.5022f, 0.5238f, 0.4907f),
        new Vector3(0.5067f, 0.5387f, 0.4867f), new Vector3(0.5082f, 0.5350f, 0.4827f),
        new Vector3(0.5119f, 0.5350f, 0.4787f), new Vector3(0.5126f, 0.5446f, 0.4747f),
        new Vector3(0.5126f, 0.5551f, 0.4707f), new Vector3(0.5097f, 0.5640f, 0.4668f),
        new Vector3(0.5007f, 0.5647f, 0.4628f), new Vector3(0.4926f, 0.5298f, 0.4588f),
        new Vector3(0.4896f, 0.5804f, 0.4548f), new Vector3(0.4866f, 0.5833f, 0.4508f),
        new Vector3(0.4874f, 0.5774f, 0.4468f), new Vector3(0.4948f, 0.5417f, 0.4428f),
        new Vector3(0.4985f, 0.5394f, 0.4388f), new Vector3(0.5119f, 0.5223f, 0.4348f),
    };

    private static Sprite streakSprite;

    private class Glint
    {
        public Image img;
        public bool sparkle;
        public float t, life, len, scale;
        public Vector2 pos;     // shares of the painting
    }
    private Glint[] glints;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { streakSprite = null; }

    protected override void Awake()
    {
        base.Awake();
        if (!streakSprite) streakSprite = MakeStreak();

        glints = new Glint[streakCount + sparkleCount];
        for (int i = 0; i < glints.Length; i++)
        {
            bool sparkle = i >= streakCount;
            var img = FxKit.NewImage(sparkle ? "Sparkle" : "Streak", transform, sparkle ? FxKit.Sparkle : streakSprite, Color.clear);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var g = new Glint { img = img, sparkle = sparkle };
            Respawn(g);
            // staggered start so the river is already shimmering, but not all in step
            g.t = sparkle ? -Random.Range(0.5f, 3f) / g.life : -Random.Range(0f, 1.2f) / g.life;
            glints[i] = g;
        }
    }

    protected override void Tick(float dt)
    {
        Rect r = Layer.rect;
        foreach (var g in glints)
        {
            g.t += dt / g.life;
            if (g.t >= 1f) Respawn(g);
            if (g.t < 0f) { g.img.color = Color.clear; continue; }
            var rt = g.img.rectTransform;

            if (g.sparkle)
            {
                // quick wink: pop in, turn a little, fade
                float k = g.t < 0.25f ? FxKit.EaseOut(g.t / 0.25f) : 1f - FxKit.Smooth((g.t - 0.25f) / 0.75f);
                rt.sizeDelta = Vector2.one * r.height * sparkleSize * g.scale * Mathf.Lerp(0.3f, 1f, k);
                rt.localRotation = Quaternion.Euler(0f, 0f, g.t * 40f);
                rt.anchoredPosition = new Vector2(g.pos.x * r.width, g.pos.y * r.height);
                g.img.color = FxKit.WithAlpha(sparkleColor, sparkleColor.a * k);
            }
            else
            {
                // open out from a point, then thin away while sliding downstream
                float open = FxKit.Smooth(Mathf.Clamp01(g.t / 0.35f));
                float fade = Mathf.Sin(g.t * Mathf.PI);
                float y = g.pos.y - drift * g.scale * g.t;
                rt.sizeDelta = new Vector2(g.len * r.width * Mathf.Lerp(0.25f, 1f, open),
                                           streakThickness * g.scale * r.height * Mathf.Lerp(1f, 0.6f, g.t));
                rt.anchoredPosition = new Vector2(g.pos.x * r.width, y * r.height);
                g.img.color = FxKit.WithAlpha(streakColor, streakColor.a * fade);
            }
        }
    }

    private void Respawn(Glint g)
    {
        // Pick a spot between two rows; sparkles only on the wide near part where they have room
        int row = g.sparkle ? Random.Range(12, Water.Length - 3) : Random.Range(0, Water.Length - 1);
        Vector3 w = Vector3.Lerp(Water[row], Water[row + 1], Random.value);
        float width = w.y - w.x;
        // perspective: further up the valley = smaller
        g.scale = Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(Water[0].z, Water[Water.Length - 1].z, w.z));
        g.len = g.sparkle ? 0f : Mathf.Min(width * Random.Range(0.3f, 0.5f), 0.026f * g.scale);
        float half = g.len * 0.5f + (g.sparkle ? sparkleSize * 0.3f : 0f);
        g.pos = new Vector2(Mathf.Lerp(w.x + half, w.y - half, Random.value), w.z);
        g.life = g.sparkle ? Random.Range(sparkleLife.x, sparkleLife.y) : Random.Range(streakLife.x, streakLife.y);
        float gap = g.sparkle ? Random.Range(1.5f, 4f) : Random.Range(0.1f, 0.7f);
        g.t = -gap / g.life;
        g.img.color = Color.clear;
    }

    // Soft horizontal light streak: bright core, feathered ends and edges. White so it can be tinted.
    private static Sprite MakeStreak()
    {
        const int W = 64, H = 12;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f;   // 0 centre, 1 ends
            float v = Mathf.Abs((y + 0.5f) / H - 0.5f) * 2f;
            float ends = 1f - FxKit.Smooth(Mathf.InverseLerp(0.35f, 1f, u));
            float edge = 1f - FxKit.Smooth(Mathf.InverseLerp(0.25f, 1f, v));
            px[y * W + x] = new Color(1f, 1f, 1f, ends * edge);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }
}
