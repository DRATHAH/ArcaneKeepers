using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// The guide's magic wand. It hovers by whatever Dog_Hero is talking about and taps it, or acts
    /// out a drag (pick a card up, carry a see-through copy over, drop it on a tile) on a loop until
    /// the player does it themselves. Targets are functions so the wand follows things that move.
    /// </summary>
    public class TutorialPointer : MonoBehaviour
    {
        enum Mode { Hidden, Point, Drag }

        TutorialTheme theme;
        TutorialFx fx;
        RectTransform root;
        RectTransform wand;
        Image tipGlow;
        Image ghost;
        CanvasGroup group;

        Mode mode = Mode.Hidden;
        Func<Vector2> pointAt;
        Func<Vector2> dragFrom, dragTo;
        float clock;
        float alpha;
        float alphaTarget;
        Vector2 pos;
        bool placed;
        float angle;
        float press;          // 0..1, pushes the tip in
        Vector2 lastPos;
        float moteTimer;

        const float TapPeriod = 1.25f;
        const float DragLoop = 3.2f;

        public void Init(TutorialTheme t, TutorialFx effects)
        {
            theme = t;
            fx = effects;
            root = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            ghost = TutorialKit.Img("Ghost", root, null, TutorialKit.A(Color.white, 0f), new Vector2(190f, 190f));
            ghost.preserveAspect = true;

            tipGlow = TutorialKit.Img("TipGlow", root, theme.softGlow, TutorialKit.A(theme.wandGlow, 0.7f), new Vector2(150f, 150f));

            Sprite s = theme.wand;
            Vector2 size = s ? new Vector2(s.rect.width, s.rect.height) * 0.82f : new Vector2(105f, 330f);
            Image w = TutorialKit.Img("Wand", root, s, Color.white, size);
            wand = w.rectTransform;
            wand.pivot = theme.wandTip;

            group.alpha = 0f;
        }

        // ------------------------------------------------------------ api

        public void PointAt(Func<Vector2> target)
        {
            if (mode == Mode.Point && pointAt == target)
            {
                return;
            }
            bool wasHidden = alpha < 0.05f;
            mode = Mode.Point;
            pointAt = target;
            clock = 0f;
            alphaTarget = 1f;
            if (wasHidden)
            {
                Snap(target() + RestOffset(target()));
            }
        }

        public void DemoDrag(Func<Vector2> from, Func<Vector2> to, Sprite ghostSprite)
        {
            mode = Mode.Drag;
            dragFrom = from;
            dragTo = to;
            ghost.sprite = ghostSprite;
            ghost.enabled = ghostSprite != null;
            clock = 0f;
            alphaTarget = 1f;
            Snap(from() + RestOffset(from()));
        }

        public void Hide()
        {
            alphaTarget = 0f;
            if (mode != Mode.Hidden)
            {
                mode = Mode.Hidden;
            }
        }

        /// <summary>Fades the wand while the player is doing the action (so it doesn't get in the way).</summary>
        public void SetDimmed(bool dimmed)
        {
            if (mode != Mode.Hidden)
            {
                alphaTarget = dimmed ? 0f : 1f;
            }
        }

        // ------------------------------------------------------------ motion

        void Snap(Vector2 p)
        {
            pos = p;
            lastPos = p;
            placed = true;
        }

        /// <summary>Where the tip rests near a target: a little below-right of it, or below-left near the right edge.</summary>
        Vector2 RestOffset(Vector2 target)
        {
            return new Vector2(SideFor(target) * 26f, -30f);
        }

        float SideFor(Vector2 target)
        {
            float half = root.rect.width * 0.5f;
            return target.x > half * 0.45f ? -1f : 1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            alpha = Mathf.MoveTowards(alpha, alphaTarget, dt * 5f);
            group.alpha = TutorialKit.Smooth(alpha);
            if (alpha <= 0f && mode == Mode.Hidden)
            {
                return;
            }
            clock += dt;

            Vector2 target = pos;
            float ghostAlpha = 0f;
            float ghostScale = 1f;
            Vector2 ghostPos = pos;
            press = 0f;

            if (mode == Mode.Point && pointAt != null)
            {
                Vector2 t = pointAt();
                float phase = (clock % TapPeriod) / TapPeriod;
                // hover, then a quick tap into the target
                float tap = phase > 0.62f ? Mathf.Sin((phase - 0.62f) / 0.38f * Mathf.PI) : 0f;
                float bob = Mathf.Sin(clock * 3.4f) * 7f;
                Vector2 rest = t + RestOffset(t) + new Vector2(0f, bob) + new Vector2(SideFor(t) * 14f, -16f);
                target = Vector2.Lerp(rest, t, tap * 0.9f);
                press = tap;
                if (Fired(phase, 0.81f, dt / TapPeriod))
                {
                    fx.Ripple(t, TutorialKit.A(theme.gold, 0.9f), 24f, 120f, 0.45f);
                    fx.Sparkles(t, theme.gold, 4, 260f, 32f);
                }
                pos = placed ? Vector2.Lerp(pos, target, TutorialKit.Damp(14f)) : target;
            }
            else if (mode == Mode.Drag && dragFrom != null && dragTo != null)
            {
                Vector2 a = dragFrom();
                Vector2 b = dragTo();
                float c = clock % DragLoop;
                float pressT = 0.45f, moveT = 0.75f, moveEnd = 1.95f, dropEnd = 2.25f, fadeEnd = 2.7f;
                Vector2 restA = a + RestOffset(a);

                if (c < pressT)
                {
                    // arrive and hover over the card
                    target = restA + new Vector2(0f, Mathf.Sin(c * 6f) * 4f);
                    alphaTarget = 1f;
                }
                else if (c < moveT)
                {
                    float p = (c - pressT) / (moveT - pressT);
                    press = Mathf.Sin(Mathf.Min(p, 0.5f) * Mathf.PI);
                    target = Vector2.Lerp(restA, a, press);
                    ghostAlpha = Mathf.Clamp01(p * 2f) * 0.75f;
                    ghostScale = TutorialKit.EaseOutBack(Mathf.Clamp01(p * 1.6f), 2f) * 1.08f;
                    ghostPos = a;
                    if (Fired(c, pressT + 0.08f, dt))
                    {
                        fx.Ripple(a, TutorialKit.A(theme.gold, 0.9f), 30f, 150f, 0.45f);
                    }
                }
                else if (c < moveEnd)
                {
                    float p = TutorialKit.EaseInOut((c - moveT) / (moveEnd - moveT));
                    // a gentle arc, lifted above the straight line
                    Vector2 mid = (a + b) * 0.5f + new Vector2(0f, Mathf.Max(90f, Vector2.Distance(a, b) * 0.22f));
                    Vector2 q = Vector2.Lerp(Vector2.Lerp(a, mid, p), Vector2.Lerp(mid, b, p), p);
                    target = q;
                    press = 0.5f;
                    ghostAlpha = 0.75f;
                    ghostScale = 1.08f;
                    ghostPos = q;
                }
                else if (c < dropEnd)
                {
                    float p = (c - moveEnd) / (dropEnd - moveEnd);
                    target = Vector2.Lerp(b, b + RestOffset(b), TutorialKit.EaseOut(p));
                    ghostAlpha = 0.75f * (1f - p);
                    ghostScale = Mathf.Lerp(1.08f, 0.7f, p);
                    ghostPos = b;
                    if (Fired(c, moveEnd + 0.01f, dt))
                    {
                        fx.Ripple(b, TutorialKit.A(theme.gold, 1f), 40f, 190f, 0.5f);
                        fx.Sparkles(b, theme.gold, 7, 380f, 40f);
                    }
                }
                else if (c < fadeEnd)
                {
                    target = b + RestOffset(b);
                }
                else
                {
                    // fade, then reappear back at the card
                    target = b + RestOffset(b);
                    alphaTarget = 0f;
                }
                if (c < 0.05f)
                {
                    alphaTarget = 1f;
                    if (alpha < 0.05f)
                    {
                        Snap(restA);
                    }
                }
                pos = Vector2.Lerp(pos, target, TutorialKit.Damp(c > moveT && c < moveEnd ? 40f : 16f));
            }

            // tip angle: stick comes from the lower right (or lower left near the right edge)
            float side = SideFor(pos);
            float targetAngle = side > 0f ? 28f : -28f;
            angle = Mathf.Lerp(angle, targetAngle - press * 6f * side, TutorialKit.Damp(10f));
            wand.anchoredPosition = pos;
            wand.localEulerAngles = new Vector3(0f, 0f, angle);
            float squeeze = 1f - press * 0.08f;
            wand.localScale = new Vector3(squeeze, squeeze, 1f);

            float glow = 0.55f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f + press * 0.3f;
            tipGlow.rectTransform.anchoredPosition = pos;
            tipGlow.color = TutorialKit.A(theme.wandGlow, glow);
            tipGlow.rectTransform.localScale = Vector3.one * (0.9f + press * 0.3f);

            ghost.rectTransform.anchoredPosition = ghostPos + new Vector2(0f, 10f);
            ghost.rectTransform.localScale = Vector3.one * ghostScale;
            ghost.color = TutorialKit.A(Color.white, ghostAlpha * group.alpha);
            ghost.transform.SetAsFirstSibling();

            // sparkle trail while the wand travels
            float speed = (pos - lastPos).magnitude / Mathf.Max(dt, 0.0001f);
            moteTimer -= dt;
            if (speed > 250f && moteTimer <= 0f && group.alpha > 0.5f)
            {
                fx.Mote(pos + UnityEngine.Random.insideUnitCircle * 14f, theme.wandGlow, UnityEngine.Random.Range(18f, 30f));
                moteTimer = 0.035f;
            }
            else if (moteTimer <= -0.6f && group.alpha > 0.5f)
            {
                fx.Mote(pos + UnityEngine.Random.insideUnitCircle * 30f, TutorialKit.A(Color.white, 0.9f), 18f);
                moteTimer = 0f;
            }
            lastPos = pos;
        }

        static bool Fired(float t, float at, float dt) => t >= at && t - dt < at;
    }
}
