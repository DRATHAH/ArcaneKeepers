using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Company splash ("Nakasaur Games") shown before the main menu.
// Black screen -> a white splash bursts out of the centre (expanding disc, shockwave ring, flying droplets)
// -> the company name pops in on white -> holds -> everything washes to white and the main menu fades in
// from under the white. Tap / click / any key skips to the wash.
// The whole screen is built in code, so the scene only needs a camera and this component.
public class SplashScreen : MonoBehaviour
{
    public string nextScene = "MainMenu";
    public string companyName = "NAKASAUR";
    public string subtitle = "GAMES";
    public TMP_FontAsset font;
    public Color inkColor = new Color(0.13f, 0.13f, 0.16f);
    public Color backdropColor = Color.black;

    [Header("Timing (seconds)")]
    public float startDelay = 0.3f;
    public float burstTime = 0.55f;
    public float holdTime = 1.6f;
    public float washTime = 0.45f;
    public float revealTime = 0.6f;

    const int DropletCount = 14;

    RectTransform root;
    Image backdrop;
    Image disc;
    Image ring;
    Image wash;
    RectTransform[] droplets;
    Vector2[] dropletDirs;
    TextMeshProUGUI title;
    TextMeshProUGUI sub;
    Sprite circle;
    bool skip;
    bool washing;

    void Awake()
    {
        Time.timeScale = 1;
        circle = MakeCircleSprite(128);
        Build();
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    void Update()
    {
        if (washing)
        {
            return;
        }
        bool pressed = (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
        if (pressed)
        {
            skip = true;
        }
    }

    IEnumerator Run()
    {
        // Start loading the menu straight away so the hand-off is instant
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        yield return Wait(startDelay);

        // --- Splash burst ---
        float size = Mathf.Max(root.rect.width, root.rect.height) * 1.6f;
        disc.enabled = ring.enabled = true;
        foreach (RectTransform d in droplets)
        {
            d.gameObject.SetActive(true);
        }
        for (float t = 0; t < 1 && !skip; t += Time.unscaledDeltaTime / burstTime)
        {
            float e = EaseOutExpo(t);
            disc.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(20, size, e);

            ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(40, size * 1.25f, EaseOutCubic(t));
            ring.color = new Color(1, 1, 1, 1 - t);

            for (int i = 0; i < droplets.Length; i++)
            {
                float dist = Mathf.Lerp(0, size * 0.55f, EaseOutCubic(t)) * (0.6f + 0.4f * ((i * 7) % 5) / 4f);
                droplets[i].anchoredPosition = dropletDirs[i] * dist;
                droplets[i].localScale = Vector3.one * (1 - t * 0.7f);
            }

            // Brief overexposed flash as the splash lands
            wash.color = new Color(1, 1, 1, Mathf.Clamp01(1 - Mathf.Abs(t - 0.35f) * 4f) * 0.8f);
            yield return null;
        }
        disc.rectTransform.sizeDelta = Vector2.one * size;
        ring.enabled = false;
        wash.color = Clear;
        foreach (RectTransform d in droplets)
        {
            d.gameObject.SetActive(false);
        }
        backdrop.color = Color.white;

        // --- Name pops in ---
        title.gameObject.SetActive(true);
        sub.gameObject.SetActive(true);
        for (float t = 0; t < 1 && !skip; t += Time.unscaledDeltaTime / 0.5f)
        {
            title.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.35f, 1f, EaseOutBack(t));
            title.alpha = Mathf.Clamp01(t * 3);
            float s = Mathf.Clamp01((t - 0.35f) / 0.65f);
            sub.alpha = s;
            sub.characterSpacing = Mathf.Lerp(0, 40, EaseOutCubic(s));
            yield return null;
        }
        title.rectTransform.localScale = Vector3.one;
        title.alpha = sub.alpha = 1;
        sub.characterSpacing = 40;

        yield return Wait(holdTime);

        // --- Wash to white, then hand over to the menu ---
        washing = true;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / washTime)
        {
            wash.color = new Color(1, 1, 1, EaseInOut(t));
            title.rectTransform.localScale = Vector3.one * (1 + 0.08f * t);
            yield return null;
        }
        wash.color = Color.white;

        // Keep the white cover across the scene change and fade it off the live menu
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        backdrop.enabled = disc.enabled = false;
        title.gameObject.SetActive(false);
        sub.gameObject.SetActive(false);

        while (op.progress < 0.9f)
        {
            yield return null;
        }
        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            yield return null;
        }
        // Let the menu lay itself out under the white first
        yield return null;
        yield return null;

        for (float t = 0; t < 1; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / revealTime)
        {
            wash.color = new Color(1, 1, 1, 1 - EaseInOut(t));
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds && !skip; t += Time.unscaledDeltaTime)
        {
            yield return null;
        }
    }

    // ---------- Building ----------

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // Blocks taps reaching the menu while the white fades off it
        gameObject.AddComponent<GraphicRaycaster>();
        root = (RectTransform)transform;

        backdrop = MakeImage("Backdrop", null, backdropColor);
        Stretch(backdrop.rectTransform);

        ring = MakeImage("Ring", MakeRingSprite(128, 0.12f), Color.white);
        ring.enabled = false;

        droplets = new RectTransform[DropletCount];
        dropletDirs = new Vector2[DropletCount];
        for (int i = 0; i < DropletCount; i++)
        {
            float angle = (i / (float)DropletCount) * Mathf.PI * 2 + Random.Range(-0.15f, 0.15f);
            dropletDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Image d = MakeImage("Droplet" + i, circle, Color.white);
            d.rectTransform.sizeDelta = Vector2.one * Random.Range(22f, 60f);
            droplets[i] = d.rectTransform;
            d.gameObject.SetActive(false);
        }

        disc = MakeImage("Disc", circle, Color.white);
        disc.enabled = false;

        title = MakeText("Title", companyName, 190, new Vector2(0, 40));
        sub = MakeText("Subtitle", subtitle, 64, new Vector2(0, -95));
        title.gameObject.SetActive(false);
        sub.gameObject.SetActive(false);

        wash = MakeImage("Wash", null, Clear);
        Stretch(wash.rectTransform);
        wash.raycastTarget = true;
    }

    Image MakeImage(string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    TextMeshProUGUI MakeText(string name, string text, float size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = inkColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = new Vector2(1800, size * 1.4f);
        tmp.rectTransform.anchoredPosition = pos;
        tmp.alpha = 0;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite MakeCircleSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float r = res / 2f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeRingSprite(int res, float thickness)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float r = res / 2f;
        float inner = r * (1 - thickness);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - inner);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    static readonly Color Clear = new Color(1, 1, 1, 0);

    static float EaseOutExpo(float t) => t >= 1 ? 1 : 1 - Mathf.Pow(2, -10 * Mathf.Clamp01(t));
    static float EaseOutCubic(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t) * (1 - t); }
    static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f, c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
}
