using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared toolkit for the main-menu effects: procedurally generated sprites (so no art assets
/// are needed), a UI image factory, and small tween/easing helpers. Everything runs on unscaled time.
/// </summary>
public static class FxKit
{
    // ---- sprites (generated once, white so they can be tinted) ----

    private static Sprite glow, sparkle, ring, sunburst;

    /// <summary>Bright core with a soft halo: magic motes, comet heads, flashes.</summary>
    public static Sprite Glow => glow ? glow : glow = MakeSprite(64, (x, y) =>
    {
        float d = Radius(x, y);
        float core = Mathf.Clamp01((0.3f - d) / 0.12f);
        float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f) * 0.6f;
        return Mathf.Max(core, halo);
    });

    /// <summary>Slim four-point twinkle.</summary>
    public static Sprite Sparkle => sparkle ? sparkle : sparkle = MakeSprite(64, (x, y) =>
    {
        float dx = Mathf.Abs(x - 0.5f) * 2f, dy = Mathf.Abs(y - 0.5f) * 2f;
        float star = Mathf.Pow(dx, 0.45f) + Mathf.Pow(dy, 0.45f);
        return Mathf.Clamp01((1.02f - star) / 0.1f);
    });

    /// <summary>Thin soft ring for shockwaves.</summary>
    public static Sprite Ring => ring ? ring : ring = MakeSprite(128, (x, y) =>
        Mathf.Clamp01(1f - Mathf.Abs(Radius(x, y) - 0.88f) / 0.07f));

    /// <summary>Radial light rays fading toward the rim (the classic title-screen "glory").</summary>
    public static Sprite Sunburst => sunburst ? sunburst : sunburst = MakeSprite(256, (x, y) =>
    {
        const int rays = 12;
        float d = Radius(x, y);
        float a = Mathf.Atan2(y - 0.5f, x - 0.5f);
        float beam = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * rays * 0.5f)), 6f);
        float falloff = Mathf.Clamp01(1f - d) * Mathf.Clamp01(d / 0.15f);
        float centre = Mathf.Pow(Mathf.Clamp01(1f - d * 1.6f), 2f) * 0.5f;
        return Mathf.Clamp01(beam * falloff + centre);
    });

    public static Sprite MakeSprite(int size, Func<float, float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            px[y * size + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / size, (y + 0.5f) / size));
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Radius(float x, float y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f)) * 2f;

    // ---- UI ----

    /// <summary>A non-interactive image, anchored bottom-left of its parent (positions are in parent pixels).</summary>
    public static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // ---- timing ----

    public static IEnumerator Wait(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
    }

    public static IEnumerator Tween(float duration, Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            step(t / duration);
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        step(1f);
    }

    public static float Smooth(float t) => t * t * (3f - 2f * t);
    public static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    public static float EaseInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
}
