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
        RectTransform shadow;
        Image shadowImg;
        Image tipGlow;
        Image core;           // bright centre on the crystal, flares on taps
        Image glint;          // four-point star that twinkles on the crystal now and then
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
        float appear = 1f;    // 0..1 pop-in when the wand shows up
        float kick;           // 1 on a tap, decays: recoil + crystal flare
        float lean;           // degrees, leans into the direction of travel
        float glintTimer = 1.2f;
        float glintAge = -1f;

        const float TapPeriod = 1.25f;
        const float DragLoop = 3.2f;
        const float AppearTime = 0.45f;
        const float GlintTime = 0.5f;
        static readonly Vector2 ShadowOffset = new Vector2(10f, -14f);

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
            shadowImg = TutorialKit.Img("WandShadow", root, s, TutorialKit.A(theme.ink, 0.22f), size);
            shadow = shadowImg.rectTransform;
            shadow.pivot = theme.wandTip;

            Image w = TutorialKit.Img("Wand", root, s, Color.white, size);
            wand = w.rectTransform;
            wand.pivot = theme.wandTip;

            core = TutorialKit.Img("CrystalCore", root, theme.softGlow, TutorialKit.A(Color.white, 0.5f), new Vector2(56f, 56f));
            glint = TutorialKit.Img("Glint", root, theme.sparkle, TutorialKit.A(Color.white, 0f), new Vector2(70f, 70f));

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
                appear = 0f;
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
            bool wasHidden = alpha < 0.05f;
            alphaTarget = 1f;
            Snap(from() + RestOffset(from()));
            if (wasHidden)
            {
                appear = 0f;
            }
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
            lean = 0f;
        }

        void Kick()
        {
            kick = 1f;
            glintAge = 0f;
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
                    fx.Sparkles(t, theme.wandGlow, 3, 190f, 24f);
                    Kick();
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
                        Kick();
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
                        fx.Flash(b, TutorialKit.A(theme.wandGlow, 0.55f), 200f, 0.3f);
                        Kick();
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
                        appear = 0f;
                    }
                }
                pos = Vector2.Lerp(pos, target, TutorialKit.Damp(c > moveT && c < moveEnd ? 40f : 16f));
            }

            float vdt = Mathf.Max(dt, 0.0001f);
            Vector2 velocity = (pos - lastPos) / vdt;

            // pop-in: grows from small with an overshoot, twirls upright and rises a touch
            float side = SideFor(pos);
            if (appear < 1f)
            {
                float before = appear;
                appear = Mathf.Min(1f, appear + Mathf.Min(dt, 1f / 30f) / AppearTime);
                if (before < 0.35f && appear >= 0.35f)
                {
                    fx.Flash(pos, TutorialKit.A(theme.wandGlow, 0.6f), 170f, 0.3f);
                    fx.Sparkles(pos, theme.wandGlow, 5, 240f, 26f);
                    glintAge = 0f;
                }
            }
            float pop = Mathf.LerpUnclamped(0.35f, 1f, TutorialKit.EaseOutBack(appear, 2.2f));
            float twirl = (1f - TutorialKit.EaseOut(appear)) * -55f * side;
            Vector2 rise = new Vector2(0f, (1f - TutorialKit.EaseOut(appear)) * -40f);

            // lean into the direction of travel, as if a hand were carrying it
            float leanTarget = Mathf.Clamp(-velocity.x * 0.01f, -12f, 12f);
            lean = Mathf.Lerp(lean, leanTarget, TutorialKit.Damp(8f));

            // tap recoil: a quick springy wobble that dies out
            kick = Mathf.MoveTowards(kick, 0f, dt * 2.6f);
            float wobble = Mathf.Sin((1f - kick) * Mathf.PI * 3f) * kick * 0.07f;

            // tip angle: stick comes from the lower right (or lower left near the right edge)
            float targetAngle = side > 0f ? 28f : -28f;
            angle = Mathf.Lerp(angle, targetAngle - press * 6f * side, TutorialKit.Damp(10f));
            float finalAngle = angle + lean + twirl;
            Vector2 drawPos = pos + rise;
            float squeeze = (1f - press * 0.08f + wobble) * pop;

            wand.anchoredPosition = drawPos;
            wand.localEulerAngles = new Vector3(0f, 0f, finalAngle);
            wand.localScale = new Vector3(squeeze, squeeze, 1f);

            // soft drop shadow so the wand reads over a busy board; it tightens as the tip presses in
            shadow.anchoredPosition = drawPos + ShadowOffset * (1f - press * 0.45f);
            shadow.localEulerAngles = wand.localEulerAngles;
            shadow.localScale = wand.localScale;
            shadowImg.color = TutorialKit.A(theme.ink, 0.22f + press * 0.08f);

            float glow = 0.55f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f + press * 0.3f + kick * 0.3f;
            tipGlow.rectTransform.anchoredPosition = drawPos;
            tipGlow.color = TutorialKit.A(theme.wandGlow, glow);
            tipGlow.rectTransform.localScale = Vector3.one * ((0.9f + press * 0.3f + kick * 0.35f) * pop);

            // lit crystal centre: steady, flares white on taps
            core.rectTransform.anchoredPosition = drawPos;
            core.color = TutorialKit.A(Color.white, 0.45f + kick * 0.5f);
            core.rectTransform.localScale = Vector3.one * ((1f + kick * 0.9f) * pop);

            // a little star glints on the crystal every couple of seconds (and on every tap)
            glintTimer -= dt;
            if (glintTimer <= 0f)
            {
                glintTimer = UnityEngine.Random.Range(1.8f, 3f);
                if (glintAge < 0f)
                {
                    glintAge = 0f;
                }
            }
            if (glintAge >= 0f)
            {
                glintAge += dt;
                float g = glintAge / GlintTime;
                if (g >= 1f)
                {
                    glintAge = -1f;
                    glint.color = TutorialKit.A(Color.white, 0f);
                }
                else
                {
                    float bell = Mathf.Sin(g * Mathf.PI);
                    glint.rectTransform.anchoredPosition = drawPos + new Vector2(7f, 9f);
                    glint.rectTransform.localEulerAngles = new Vector3(0f, 0f, g * 90f);
                    glint.rectTransform.localScale = Vector3.one * (bell * 1.1f);
                    glint.color = TutorialKit.A(Color.white, bell);
                }
            }

            ghost.rectTransform.anchoredPosition = ghostPos + new Vector2(0f, 10f);
            ghost.rectTransform.localScale = Vector3.one * ghostScale;
            ghost.color = TutorialKit.A(Color.white, ghostAlpha * group.alpha);
            ghost.transform.SetAsFirstSibling();

            // sparkle trail while the wand travels
            float speed = velocity.magnitude;
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
