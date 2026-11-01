using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Soft rotating sunburst behind the logo, the classic mobile title-screen "glory".
/// Two counter-rotating layers so the rays shimmer instead of just spinning.
/// Put it on a stretched child of the Logo, placed before the Title.
/// </summary>
public class LogoGlory : MenuFxLayer
{
    [SerializeField] private Color color = new Color(1f, 0.92f, 0.6f, 0.5f);
    [SerializeField] private float size = 2.2f;          // in logo heights
    [SerializeField] private float spinSpeed = 6f;       // degrees per second
    [SerializeField] private float pulseSpeed = 0.9f;

    private RectTransform front, back;
    private Image frontImg, backImg;

    protected override void Awake()
    {
        base.Awake();
        backImg = FxKit.NewImage("GloryBack", transform, FxKit.Sunburst, color);
        frontImg = FxKit.NewImage("GloryFront", transform, FxKit.Sunburst, color);
        back = Centre(backImg.rectTransform);
        front = Centre(frontImg.rectTransform);
    }

    protected override void Tick(float dt)
    {
        float h = Layer.rect.height * size;
        float pulse = Mathf.Sin(Elapsed * pulseSpeed) * 0.5f + 0.5f;
        front.sizeDelta = Vector2.one * h * (1f + 0.04f * pulse);
        back.sizeDelta = Vector2.one * h * 1.15f;
        front.localRotation = Quaternion.Euler(0f, 0f, -Elapsed * spinSpeed);
        back.localRotation = Quaternion.Euler(0f, 0f, Elapsed * spinSpeed * 0.6f + 15f);
        frontImg.color = FxKit.WithAlpha(color, color.a * Mathf.Lerp(0.75f, 1f, pulse));
        backImg.color = FxKit.WithAlpha(color, color.a * 0.45f);
    }

    private static RectTransform Centre(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
}
