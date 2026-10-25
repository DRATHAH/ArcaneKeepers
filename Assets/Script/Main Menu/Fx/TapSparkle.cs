using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Magic feedback wherever the player taps: a quick glow flash, a gold shockwave ring
/// and a burst of twinkles. Put it on a full-screen layer above the menu art.
/// </summary>
public class TapSparkle : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private int sparks = 8;

    private RectTransform layer;
    private Camera uiCamera;

    private void Awake()
    {
        layer = (RectTransform)transform;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void Update()
    {
        if (PressedThisFrame(out Vector2 screen)
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out Vector2 local))
            Burst(local - layer.rect.min); // images are anchored bottom-left
    }

    public void Burst(Vector2 pos)
    {
        float h = layer.rect.height;
        StartCoroutine(Flash(pos, h * 0.16f));
        StartCoroutine(Shockwave(pos, h * 0.24f));
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < sparks; i++)
            StartCoroutine(Spark(pos, (offset + i * 360f / sparks) * Mathf.Deg2Rad, h));
    }

    private IEnumerator Flash(Vector2 pos, float size)
    {
        var img = FxKit.NewImage("Flash", layer, FxKit.Glow, Color.white);
        var rt = img.rectTransform;
        rt.anchoredPosition = pos;
        yield return FxKit.Tween(0.25f, t =>
        {
            rt.sizeDelta = Vector2.one * size * Mathf.Lerp(0.6f, 1.2f, FxKit.EaseOut(t));
            img.color = FxKit.WithAlpha(Color.white, 0.9f * (1f - t));
        });
        Destroy(img.gameObject);
    }

    private IEnumerator Shockwave(Vector2 pos, float size)
    {
        var img = FxKit.NewImage("Shockwave", layer, FxKit.Ring, color);
        var rt = img.rectTransform;
        rt.anchoredPosition = pos;
        yield return FxKit.Tween(0.45f, t =>
        {
            rt.sizeDelta = Vector2.one * size * Mathf.Lerp(0.2f, 1f, FxKit.EaseOut(t));
            img.color = FxKit.WithAlpha(color, 1f - t);
        });
        Destroy(img.gameObject);
    }

    private IEnumerator Spark(Vector2 pos, float angle, float h)
    {
        var img = FxKit.NewImage("Spark", layer, FxKit.Sparkle, color);
        var rt = img.rectTransform;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float dist = h * Random.Range(0.1f, 0.15f);
        float size = h * Random.Range(0.035f, 0.05f);
        float spin = Random.Range(-180f, 180f);
        yield return FxKit.Tween(Random.Range(0.45f, 0.6f), t =>
        {
            // Fly out, then drift up a touch as they fade, like they're floating away.
            rt.anchoredPosition = pos + dir * dist * FxKit.EaseOut(t) + Vector2.up * h * 0.03f * t * t;
            float s = size * (t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f);
            rt.sizeDelta = new Vector2(s, s);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
        });
        Destroy(img.gameObject);
    }

    private static bool PressedThisFrame(out Vector2 pos)
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            pos = touch.primaryTouch.position.ReadValue();
            return true;
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            return true;
        }
        pos = default;
        return false;
    }
}
