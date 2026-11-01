using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// A soft glowing outline that wraps a UI element (or any rect), pops in, breathes gently and
    /// throws the odd sparkle off its edge. It follows the target every frame.
    /// </summary>
    public class GlowHighlight : MonoBehaviour
    {
        TutorialTheme theme;
        TutorialFx fx;
        Image frame;
        Image fill;
        Func<Rect> target;
        float padding;
        float t;            // time since shown
        float visible;      // 0..1
        bool showing;
        float sparkleTimer;
        Color color;

        public bool Showing => showing;

        public void Init(TutorialTheme th, TutorialFx effects)
        {
            theme = th;
            fx = effects;
            fill = TutorialKit.Img("Fill", transform, theme.softGlow, Color.clear, Vector2.one);
            frame = TutorialKit.Img("Frame", transform, theme.glowFrame, Color.clear, Vector2.one, true);
            gameObject.SetActive(false);
        }

        public void Show(Func<Rect> rect, Color tint, float pad = 18f)
        {
            target = rect;
            padding = pad;
            color = tint;
            if (!showing)
            {
                t = 0f;
            }
            showing = true;
            gameObject.SetActive(true);
            Update();
        }

        public void Hide()
        {
            showing = false;
        }

        void Update()
        {
            if (target == null)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            t += dt;
            visible = Mathf.MoveTowards(visible, showing ? 1f : 0f, dt * (showing ? 4f : 6f));
            if (visible <= 0f && !showing)
            {
                gameObject.SetActive(false);
                return;
            }

            Rect r = target();
            // the frame sprite has 56px of glow around its line: grow the rect by that much
            const float glowMargin = 50f;
            float pop = showing ? TutorialKit.EaseOutBack(Mathf.Clamp01(t / 0.35f), 2f) : 1f;
            float breathe = 1f + Mathf.Sin(t * 3.2f) * 0.012f;
            Vector2 size = (r.size + Vector2.one * (padding + glowMargin) * 2f) * Mathf.Lerp(1.25f, 1f, pop) * breathe;

            RectTransform fr = frame.rectTransform;
            fr.anchoredPosition = r.center;
            fr.sizeDelta = size;
            float pulse = 0.78f + Mathf.Sin(t * 3.2f) * 0.22f;
            frame.color = TutorialKit.A(color, pulse * TutorialKit.Smooth(visible));

            RectTransform fl = fill.rectTransform;
            fl.anchoredPosition = r.center;
            fl.sizeDelta = r.size + Vector2.one * padding * 3f;
            fill.color = TutorialKit.A(color, 0.10f * TutorialKit.Smooth(visible) * (0.8f + 0.2f * Mathf.Sin(t * 3.2f)));

            // a sparkle every so often somewhere on the outline
            sparkleTimer -= dt;
            if (showing && sparkleTimer <= 0f && visible > 0.9f)
            {
                sparkleTimer = UnityEngine.Random.Range(0.25f, 0.5f);
                Rect e = new Rect(r.center - (r.size * 0.5f + Vector2.one * padding), r.size + Vector2.one * padding * 2f);
                fx.Mote(PointOnEdge(e), TutorialKit.A(Color.Lerp(color, Color.white, 0.5f), 1f), UnityEngine.Random.Range(20f, 30f));
            }
        }

        static Vector2 PointOnEdge(Rect r)
        {
            float per = (r.width + r.height) * 2f;
            float d = UnityEngine.Random.value * per;
            if (d < r.width) return new Vector2(r.xMin + d, r.yMax);
            d -= r.width;
            if (d < r.height) return new Vector2(r.xMax, r.yMax - d);
            d -= r.height;
            if (d < r.width) return new Vector2(r.xMax - d, r.yMin);
            d -= r.width;
            return new Vector2(r.xMin, r.yMin + d);
        }
    }
}
