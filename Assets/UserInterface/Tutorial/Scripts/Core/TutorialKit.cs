using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Small helpers shared by the tutorial pieces: UI factories, easing, unscaled tweens,
    /// and conversions from screen / world / other-canvas rects into the tutorial canvas.
    /// Everything runs on unscaled time so it keeps animating while the game is paused.
    /// </summary>
    public static class TutorialKit
    {
        // ------------------------------------------------------------ factories

        public static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 size, bool sliced = false)
        {
            RectTransform rt = Rect(name, parent, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced)
            {
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TutorialTheme theme, string text, float size, Color color)
        {
            RectTransform rt = Rect(name, parent, new Vector2(400, 100));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (theme && theme.font)
            {
                t.font = theme.font;
            }
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Color A(Color c, float a)
        {
            c.a = a;
            return c;
        }

        // ------------------------------------------------------------ easing

        public static float Smooth(float t) => t * t * (3f - 2f * t);
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
        public static float EaseIn(float t) => t * t * t;
        public static float EaseInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        public static float EaseOutBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>Damped spring from 0 to 1 (a wobble that settles).</summary>
        public static float Spring(float t, float freq = 3.2f, float damping = 5f)
        {
            return 1f - Mathf.Exp(-damping * t) * Mathf.Cos(freq * Mathf.PI * 2f * t);
        }

        /// <summary>Frame-rate independent smoothing factor.</summary>
        public static float Damp(float speed) => 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);

        // ------------------------------------------------------------ timing

        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
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

        // ------------------------------------------------------------ coordinates

        /// <summary>Screen pixel -> local point of <paramref name="space"/> (an overlay canvas child).</summary>
        public static Vector2 ScreenToLocal(RectTransform space, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screen, CanvasCamera(space), out Vector2 local);
            return local;
        }

        /// <summary>The camera a canvas renders through (null for overlay canvases).</summary>
        public static Camera CanvasCamera(Transform t)
        {
            Canvas c = t ? t.GetComponentInParent<Canvas>() : null;
            if (c)
            {
                c = c.rootCanvas;
            }
            return c && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        }

        public static bool WorldToLocal(RectTransform space, Vector3 world, out Vector2 local)
        {
            Camera cam = Camera.main;
            local = Vector2.zero;
            if (!cam)
            {
                return false;
            }
            Vector3 sp = cam.WorldToScreenPoint(world);
            local = ScreenToLocal(space, sp);
            return sp.z > 0f;
        }

        /// <summary>Bounds of any UI element (on any overlay canvas) in <paramref name="space"/>'s local coordinates.</summary>
        public static UnityEngine.Rect RectIn(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Camera cam = CanvasCamera(target);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 c in corners)
            {
                Vector2 local = ScreenToLocal(space, RectTransformUtility.WorldToScreenPoint(cam, c));
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            return new UnityEngine.Rect(min, max - min);
        }

        public static UnityEngine.Rect Encapsulate(UnityEngine.Rect a, UnityEngine.Rect b)
        {
            return UnityEngine.Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        // ------------------------------------------------------------ sound

        static AudioSource source;

        public static void Play(TutorialTheme theme, AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (!clip || !theme)
            {
                return;
            }
            if (!source)
            {
                var go = new GameObject("TutorialAudio");
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.ignoreListenerPause = true;
            }
            source.pitch = pitch;
            source.PlayOneShot(clip, theme.volume * volume);
        }

        /// <summary>Game sound by name (SoundManager), silently skipped if there is none.</summary>
        public static void GameSound(string name)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(name);
            }
        }
    }
}
