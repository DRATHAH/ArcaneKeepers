using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cartoon cloud cover for the hop from the main menu into the character select.
// Two solid banks of puffy clouds roll in from the sides and meet in the middle, the next scene
// loads behind them, then they part again. The cloud art is drawn in code: cel-shaded, white tops
// with blue undersides, no outlines.
// SceneTransition drives it: Close() covers the screen, Open() uncovers it.
public class CloudWipe : MonoBehaviour
{
    public static float closeTime = 0.5f;
    public static float openTime = 0.6f;

    static readonly Color Top = new Color(1f, 0.99f, 0.97f);
    static readonly Color Under = new Color(0.74f, 0.83f, 0.96f);
    // Clouds deeper in the bank are tinted a little so the cover has some depth
    static readonly Color BackTint = new Color(0.93f, 0.95f, 1f);

    static Sprite[] puffSprites;

    class Puff
    {
        public RectTransform rect;
        public Vector2 home;
        public float size;
        public float lag;
        public float phase;
    }

    class Bank
    {
        public RectTransform rect;
        // -1 = comes in from the left, 1 = from the right
        public float side;
        public readonly List<Puff> puffs = new List<Puff>();
    }

    RectTransform root;
    Bank left;
    Bank right;
    float height;
    float width;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        puffSprites = null;
    }

    public IEnumerator Close()
    {
        Build();
        for (float t = 0; t < 1; t += Step(closeTime))
        {
            SetCover(EaseOut(t));
            yield return null;
        }
        SetCover(1);
    }

    public IEnumerator Open()
    {
        for (float t = 0; t < 1; t += Step(openTime))
        {
            SetCover(1 - EaseInOut(t));
            yield return null;
        }
        SetCover(0);
        root.gameObject.SetActive(false);
    }

    // Capped step: a slow frame (like the new scene waking up) slows the clouds instead of making them jump
    static float Step(float duration)
    {
        return Mathf.Min(Time.unscaledDeltaTime, 1f / 45f) / duration;
    }

    // Lays the banks out for the current screen size (rebuilt each time in case it rotated)
    void Build()
    {
        if (puffSprites == null)
        {
            puffSprites = new[] { MakePuff(ShapeA), MakePuff(ShapeB), MakePuff(ShapeC) };
        }
        if (root)
        {
            Destroy(root.gameObject);
        }
        root = NewRect("Clouds", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        Rect canvasRect = ((RectTransform)transform).rect;
        width = canvasRect.width;
        height = canvasRect.height;
        // Same layout every time so the clouds feel designed rather than random
        System.Random random = new System.Random(7);
        left = BuildBank(-1, random);
        right = BuildBank(1, random);
    }

    Bank BuildBank(float side, System.Random random)
    {
        Bank bank = new Bank { side = side };
        bank.rect = NewRect(side < 0 ? "LeftBank" : "RightBank", root);
        bank.rect.sizeDelta = Vector2.zero;
        // Towards the middle of the screen
        float forward = -side;

        // Solid body behind the puffs; its straight edge is tucked behind the front row
        Image body = NewRect("Body", bank.rect).gameObject.AddComponent<Image>();
        body.color = Top;
        body.rectTransform.sizeDelta = new Vector2(width * 1.3f, height * 1.4f);
        body.rectTransform.anchoredPosition = new Vector2(forward * (-0.08f * height - width * 0.65f), 0);

        // Back to front: tinted rows deep in the bank (out to the screen edge), then two scalloped
        // rows along the edge. Within a row the lower clouds go first, so each puff's blue underside
        // sits over the one below.
        AddRow(bank, random, forward * -1.15f, 5, 0.62f, BackTint);
        AddRow(bank, random, forward * -0.8f, 6, 0.58f, BackTint);
        AddRow(bank, random, forward * -0.45f, 5, 0.55f, BackTint);
        AddRow(bank, random, forward * -0.15f, 8, 0.48f, Color.white);
        AddRow(bank, random, forward * 0.08f, 8, 0.5f, Color.white);
        return bank;
    }

    void AddRow(Bank bank, System.Random random, float x, int count, float size, Color tint)
    {
        for (int i = 0; i < count; i++)
        {
            float y = Mathf.Lerp(-0.63f, 0.63f, count == 1 ? 0.5f : i / (count - 1f));
            // Alternate rows poke out further, giving the edge its scalloped shape
            float scallop = (i % 2 == 0 ? 0.05f : -0.02f) + Range(random, -0.03f, 0.03f);
            Image image = NewRect("Puff", bank.rect).gameObject.AddComponent<Image>();
            image.sprite = puffSprites[random.Next(puffSprites.Length)];
            image.color = tint;
            image.raycastTarget = false;
            Puff puff = new Puff
            {
                rect = image.rectTransform,
                home = new Vector2(x - bank.side * scallop, y + Range(random, -0.03f, 0.03f)) * height,
                size = size * Range(random, 0.9f, 1.15f) * height,
                lag = Range(random, 0f, 0.3f) * height,
                phase = Range(random, 0f, Mathf.PI * 2),
            };
            // Mirror the right bank's clouds so the two sides aren't copies of each other
            puff.rect.localScale = new Vector3(bank.side < 0 ? 1 : -1, 1, 1);
            puff.rect.sizeDelta = new Vector2(puff.size * PuffAspect, puff.size);
            bank.puffs.Add(puff);
        }
    }

    // cover = 0: banks off screen, cover = 1: they overlap in the middle and hide everything
    void SetCover(float cover)
    {
        root.gameObject.SetActive(true);
        PlaceBank(left, cover);
        PlaceBank(right, cover);
    }

    void PlaceBank(Bank bank, float cover)
    {
        float closedEdge = -bank.side * 0.12f * height;
        float openEdge = bank.side * (width * 0.5f + 0.55f * height);
        bank.rect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(openEdge, closedEdge, cover), 0);

        float time = Time.unscaledTime;
        float trail = (1 - cover) * (1 - cover);
        foreach (Puff puff in bank.puffs)
        {
            // Some puffs trail behind the bank on the way in and leave first on the way out,
            // so the edge rolls instead of moving as one flat wall. They all catch up when it closes.
            float bob = Mathf.Sin(time * 2.6f + puff.phase);
            puff.rect.anchoredPosition = puff.home + new Vector2(bank.side * puff.lag * trail, bob * 0.008f * height);
            float breathe = 1 + 0.03f * bob;
            puff.rect.sizeDelta = new Vector2(puff.size * PuffAspect, puff.size) * breathe;
        }
    }

    static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }

    static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // ---- Cloud art ----

    const int PuffWidth = 512;
    const int PuffHeight = 384;
    const float PuffAspect = (float)PuffWidth / PuffHeight;

    // Each cloud is a union of circles: (x as a share of the width, y and radius as shares of the height)
    static readonly Vector3[] ShapeA =
    {
        new Vector3(0.25f, 0.38f, 0.24f), new Vector3(0.45f, 0.55f, 0.34f), new Vector3(0.70f, 0.45f, 0.28f),
        new Vector3(0.85f, 0.32f, 0.18f), new Vector3(0.50f, 0.30f, 0.28f), new Vector3(0.15f, 0.28f, 0.15f),
    };
    static readonly Vector3[] ShapeB =
    {
        new Vector3(0.50f, 0.50f, 0.40f), new Vector3(0.25f, 0.42f, 0.26f), new Vector3(0.75f, 0.42f, 0.27f),
        new Vector3(0.38f, 0.68f, 0.20f), new Vector3(0.62f, 0.30f, 0.25f),
    };
    static readonly Vector3[] ShapeC =
    {
        new Vector3(0.35f, 0.50f, 0.33f), new Vector3(0.65f, 0.58f, 0.32f), new Vector3(0.50f, 0.30f, 0.27f),
        new Vector3(0.82f, 0.35f, 0.20f), new Vector3(0.18f, 0.33f, 0.19f),
    };

    // Draws one cel-shaded puff: the full shape in the underside blue, and the same circles nudged
    // up and shrunk in white on top, leaving a blue rim along the bottom of every bump.
    static Sprite MakePuff(Vector3[] shape)
    {
        Texture2D texture = new Texture2D(PuffWidth, PuffHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[PuffWidth * PuffHeight];
        for (int y = 0; y < PuffHeight; y++)
        {
            for (int x = 0; x < PuffWidth; x++)
            {
                float under = 0;
                float top = 0;
                foreach (Vector3 circle in shape)
                {
                    float cx = circle.x * PuffWidth;
                    float cy = circle.y * PuffHeight;
                    float r = circle.z * PuffHeight;
                    under = Mathf.Max(under, Coverage(x, y, cx, cy, r));
                    top = Mathf.Max(top, Coverage(x, y, cx, cy + 0.1f * r, 0.88f * r));
                }
                Color color = Color.Lerp(Under, Top, top);
                color.a = Mathf.Max(under, top);
                pixels[y * PuffWidth + x] = color;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, PuffWidth, PuffHeight), new Vector2(0.5f, 0.5f), 100);
    }

    // How much of the pixel at (x, y) is inside the circle, with a soft 1.5 px edge
    static float Coverage(int x, int y, float cx, float cy, float r)
    {
        float dx = x + 0.5f - cx;
        float dy = y + 0.5f - cy;
        return Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) / 1.5f + 0.5f);
    }
}
