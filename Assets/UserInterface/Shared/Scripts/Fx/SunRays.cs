using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A few soft beams of sunlight fanning down from the top-right corner of the character select,
/// each slowly breathing brighter and dimmer out of step with the others. Very faint, just enough
/// to warm the sky; sits behind all the UI. Beam art is drawn in code.
/// </summary>
public class SunRays : MenuFxLayer
{
    [SerializeField] private int beams = 4;
    [SerializeField] private Vector2 angle = new Vector2(28f, 62f);     // degrees below horizontal, pointing left
    [SerializeField] private float length = 1.3f;                       // share of layer height
    [SerializeField] private Vector2 width = new Vector2(0.10f, 0.20f); // share of layer height
    [SerializeField] private Vector2 alpha = new Vector2(0.10f, 0.22f);
    [SerializeField] private float breathe = 0.12f;                     // cycles per second
    [SerializeField] private Color color = new Color(1f, 0.96f, 0.82f, 1f);

    private static Sprite beamSprite;
    private Image[] rays;
    private float[] phase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { beamSprite = null; }

    protected override void Awake()
    {
        base.Awake();
        if (!beamSprite) beamSprite = MakeBeam();
        rays = new Image[beams];
        phase = new float[beams];
        for (int i = 0; i < beams; i++)
        {
            var img = FxKit.NewImage("Ray", transform, beamSprite, Color.clear);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.one;           // top-right corner
            rt.pivot = new Vector2(0.5f, 1f);                    // beam hangs from its source
            float k = beams > 1 ? (float)i / (beams - 1) : 0.5f;
            // 0 = hanging straight down; (angle - 90) swings it to point down-left at that angle below horizontal
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(angle.x, angle.y, k) - 90f);
            rays[i] = img;
            phase[i] = Random.value * Mathf.PI * 2f;
        }
    }

    protected override void Tick(float dt)
    {
        float h = Layer.rect.height;
        for (int i = 0; i < rays.Length; i++)
        {
            float k = rays.Length > 1 ? (float)i / (rays.Length - 1) : 0.5f;
            var rt = rays[i].rectTransform;
            rt.anchoredPosition = new Vector2(-h * 0.05f, h * 0.05f);
            rt.sizeDelta = new Vector2(h * Mathf.Lerp(width.y, width.x, k), h * length);
            float b = 0.5f + 0.5f * Mathf.Sin(Elapsed * breathe * Mathf.PI * 2f + phase[i]);
            rays[i].color = FxKit.WithAlpha(color, Mathf.Lerp(alpha.x, alpha.y, b));
        }
    }

    // Soft vertical beam: bright and narrow at the top, widening a little and fading out downward.
    private static Sprite MakeBeam()
    {
        const int W = 64, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float down = 1f - (y + 0.5f) / H;                       // 0 at the source (top), 1 at the far end
            float spread = Mathf.Lerp(0.55f, 1f, down);
            float across = Mathf.Clamp01(1f - Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f / spread);
            float a = across * across * Mathf.Pow(1f - down, 1.6f) * Mathf.Clamp01(down * 12f);
            px[y * W + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 1f), 100f);
    }
}
