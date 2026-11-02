using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    public enum DogMood
    {
        Neutral,
        Happy,     // little double hop + sparkle
        Cheer,     // big jump with confetti
        Worried,   // leans back, shivers, sweat drop
        Surprised, // startled hop + "!"
        Curious,   // head tilt + "?"
        Love,      // happy wiggle + heart
        Point,     // leans toward what he's showing
    }

    /// <summary>
    /// Dog_Hero, the tutorial guide. He lives in the bottom-right corner, pops in from below,
    /// breathes while idle, bobs as he talks and reacts to the player with small squash-and-stretch
    /// animations and emote icons. All motion is layered (idle + talk + one reaction at a time) and
    /// composed in LateUpdate so nothing fights over the transform.
    /// </summary>
    public class DogGuide : MonoBehaviour
    {
        public const float Height = 330f;

        TutorialTheme theme;
        TutorialFx fx;
        RectTransform root;    // anchored corner spot (moves only for enter/exit)
        RectTransform body;    // pivot at his feet: squash, stretch, tilt
        RectTransform art;
        Image shadow;
        Image emote;

        // layered pose
        Vector2 reactOffset;
        Vector2 reactScale = Vector2.one;
        float reactAngle;
        float lean;            // -1 (leans left) .. 1, eased
        float leanTarget;
        float talkKick;        // spikes on every word, decays
        float enter;           // 0 hidden below the screen .. 1 standing in his spot
        Coroutine reaction;
        Coroutine emoteRoutine;

        public bool Visible => enter > 0.01f;
        public RectTransform Root => root;

        /// <summary>Top of his head in the root's parent space (where the bubble's tail points).</summary>
        public Vector2 HeadAnchor => root.anchoredPosition + new Vector2(-40f, Height * 0.86f);

        public void Init(TutorialTheme t, TutorialFx effects, RectTransform parent)
        {
            theme = t;
            fx = effects;
            root = (RectTransform)transform;
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(Height, Height);
            root.anchoredPosition = new Vector2(-420f, 8f); // left of the round MOVE button (bottom-right corner)

            shadow = TutorialKit.Img("Shadow", root, theme.dogShadow, TutorialKit.A(theme.ink, 0.35f), new Vector2(250f, 44f));
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            body = TutorialKit.Rect("Body", root, new Vector2(Height, Height));
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f);
            body.pivot = new Vector2(0.5f, 0f);
            body.anchoredPosition = Vector2.zero;

            Sprite s = theme.dog;
            float aspect = s ? s.rect.width / s.rect.height : 0.85f;
            Image a = TutorialKit.Img("Dog_Hero", body, s, Color.white, new Vector2(Height * aspect, Height));
            a.preserveAspect = true;
            art = a.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0f);
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = Vector2.zero;
            // The art faces right; he stands on the right side of the screen, so turn him to face the board
            art.localScale = new Vector3(-1f, 1f, 1f);

            emote = TutorialKit.Img("Emote", root, null, Color.white, new Vector2(96f, 96f));
            emote.rectTransform.anchorMin = emote.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            emote.gameObject.SetActive(false);

            enter = 0f;
            Compose();
        }

        // ------------------------------------------------------------ public api

        public IEnumerator Enter()
        {
            gameObject.SetActive(true);
            Squash(0f);
            // Rises from below the screen with a stretch, overshoots, lands with a squash and dust
            yield return TutorialKit.Tween(0.42f, p =>
            {
                enter = TutorialKit.EaseOutBack(p, 1.4f);
                reactScale = new Vector2(Mathf.Lerp(0.8f, 1f, p), Mathf.Lerp(1.25f, 1f, p));
            });
            fx.Dust(FeetPos(), 6);
            TutorialKit.Play(theme, theme.popSound);
            yield return Squash(0.28f, 0.22f);
            Emote(theme.emoteSparkle, 1.1f);
        }

        public IEnumerator Exit()
        {
            StopReaction();
            // Little hop of goodbye, then down out of sight
            yield return TutorialKit.Tween(0.18f, p => reactOffset = new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 40f));
            yield return TutorialKit.Tween(0.32f, p =>
            {
                enter = 1f - TutorialKit.EaseIn(p);
                reactScale = new Vector2(Mathf.Lerp(1f, 0.9f, p), Mathf.Lerp(1f, 1.15f, p));
            });
            reactScale = Vector2.one;
            reactOffset = Vector2.zero;
            enter = 0f;
        }

        public void React(DogMood mood)
        {
            StopReaction();
            leanTarget = 0f;
            switch (mood)
            {
                case DogMood.Happy: reaction = StartCoroutine(Happy()); break;
                case DogMood.Cheer: reaction = StartCoroutine(Cheer()); break;
                case DogMood.Worried: reaction = StartCoroutine(Worried()); break;
                case DogMood.Surprised: reaction = StartCoroutine(Surprised()); break;
                case DogMood.Curious: reaction = StartCoroutine(Curious()); break;
                case DogMood.Love: reaction = StartCoroutine(Love()); break;
                case DogMood.Point: leanTarget = -1f; break;
                default: leanTarget = 0f; break;
            }
        }

        /// <summary>Lean toward what he's showing: -1 = toward the board (left), 0 = stand straight.</summary>
        public void Lean(float amount)
        {
            leanTarget = Mathf.Clamp(amount, -1f, 1f);
        }

        /// <summary>Called by the speech bubble on every word: a small bob, like he's talking.</summary>
        public void TalkBeat()
        {
            talkKick = 1f;
        }

        public void Emote(Sprite sprite, float hold = 1.2f)
        {
            if (!sprite)
            {
                return;
            }
            if (emoteRoutine != null)
            {
                StopCoroutine(emoteRoutine);
            }
            emoteRoutine = StartCoroutine(EmoteRoutine(sprite, hold));
        }

        // ------------------------------------------------------------ reactions

        IEnumerator Happy()
        {
            Emote(theme.emoteSparkle);
            for (int i = 0; i < 2; i++)
            {
                yield return Hop(i == 0 ? 46f : 30f, 0.26f);
            }
            reaction = null;
        }

        IEnumerator Cheer()
        {
            Emote(theme.emoteSparkle, 1.4f);
            TutorialKit.Play(theme, theme.cheerSound);
            yield return Squash(0.12f, 0.2f);
            fx.Confetti(FeetPos() + new Vector2(0f, Height * 0.8f), 14);
            // one clean jump straight up (no side-to-side tilt, which read as shaking)
            yield return TutorialKit.Tween(0.5f, p =>
            {
                float h = Mathf.Sin(p * Mathf.PI);
                reactOffset = new Vector2(0f, h * 110f);
                reactScale = new Vector2(1f - h * 0.08f, 1f + h * 0.1f);
            });
            fx.Dust(FeetPos(), 4);
            yield return Squash(0.25f, 0.2f);
            reaction = null;
        }

        IEnumerator Worried()
        {
            Emote(theme.emoteSweat, 1.4f);
            // flinches back (away from the board) and sinks a little, holds, then recovers. No shiver.
            yield return TutorialKit.Tween(0.2f, p => { reactAngle = Mathf.Lerp(0f, -7f, TutorialKit.EaseOut(p)); reactScale = new Vector2(1f + 0.05f * p, 1f - 0.05f * p); });
            yield return TutorialKit.Wait(0.45f);
            yield return TutorialKit.Tween(0.3f, p => { reactAngle = Mathf.Lerp(-7f, 0f, TutorialKit.Smooth(p)); reactScale = Vector2.Lerp(new Vector2(1.05f, 0.95f), Vector2.one, p); });
            reaction = null;
        }

        IEnumerator Surprised()
        {
            Emote(theme.emoteExclaim, 1f);
            yield return TutorialKit.Tween(0.22f, p =>
            {
                float h = Mathf.Sin(p * Mathf.PI);
                reactOffset = new Vector2(0f, h * 50f);
                reactScale = new Vector2(1f - h * 0.1f, 1f + h * 0.14f);
            });
            yield return Squash(0.2f, 0.15f);
            reaction = null;
        }

        IEnumerator Curious()
        {
            Emote(theme.emoteQuestion, 1.3f);
            yield return TutorialKit.Tween(0.3f, p => reactAngle = Mathf.Lerp(0f, 6f, TutorialKit.EaseOutBack(p)));
            yield return TutorialKit.Wait(0.7f);
            yield return TutorialKit.Tween(0.3f, p => reactAngle = Mathf.Lerp(6f, 0f, TutorialKit.Smooth(p)));
            reaction = null;
        }

        IEnumerator Love()
        {
            Emote(theme.emoteHeart, 1.3f);
            // puffs up proudly and settles (replaces the old side-to-side wiggle)
            yield return TutorialKit.Tween(0.25f, p =>
            {
                float k = TutorialKit.EaseOutBack(p);
                reactScale = new Vector2(1f + 0.06f * k, 1f + 0.08f * k);
                reactOffset = new Vector2(0f, 12f * k);
            });
            yield return TutorialKit.Tween(0.35f, p =>
            {
                float k = 1f - TutorialKit.Smooth(p);
                reactScale = new Vector2(1f + 0.06f * k, 1f + 0.08f * k);
                reactOffset = new Vector2(0f, 12f * k);
            });
            reactScale = Vector2.one;
            reactOffset = Vector2.zero;
            reaction = null;
        }

        IEnumerator Hop(float height, float duration)
        {
            yield return Squash(0.08f, 0.12f);
            yield return TutorialKit.Tween(duration, p =>
            {
                float h = Mathf.Sin(p * Mathf.PI);
                reactOffset = new Vector2(0f, h * height);
                reactScale = new Vector2(1f - h * 0.06f, 1f + h * 0.08f);
            });
            yield return Squash(0.14f, 0.12f);
        }

        /// <summary>Squash on landing that springs back.</summary>
        IEnumerator Squash(float duration, float amount = 0.15f)
        {
            if (duration <= 0f)
            {
                yield break;
            }
            yield return TutorialKit.Tween(duration, p =>
            {
                float k = Mathf.Sin(p * Mathf.PI) * amount * (1f - p * 0.3f);
                reactScale = new Vector2(1f + k, 1f - k);
                reactOffset = Vector2.zero;
            });
            reactScale = Vector2.one;
        }

        IEnumerator EmoteRoutine(Sprite sprite, float hold)
        {
            emote.sprite = sprite;
            emote.gameObject.SetActive(true);
            RectTransform rt = emote.rectTransform;
            Vector2 basePos = new Vector2(70f, Height * 0.92f); // beside his head, clear of the bubble tail
            yield return TutorialKit.Tween(0.28f, p =>
            {
                float s = TutorialKit.EaseOutBack(p, 2.6f);
                rt.localScale = Vector3.one * s;
                rt.anchoredPosition = basePos + new Vector2(0f, 30f * p);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-25f, 8f, p));
                emote.color = TutorialKit.A(Color.white, Mathf.Clamp01(p * 3f));
            });
            yield return TutorialKit.Tween(hold, p =>
            {
                rt.anchoredPosition = basePos + new Vector2(0f, 30f + Mathf.Sin(p * Mathf.PI * 2f) * 5f);
                rt.localEulerAngles = new Vector3(0f, 0f, 8f * Mathf.Cos(p * Mathf.PI * 2f));
            });
            yield return TutorialKit.Tween(0.2f, p =>
            {
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, p);
                emote.color = TutorialKit.A(Color.white, 1f - p);
            });
            emote.gameObject.SetActive(false);
            emoteRoutine = null;
        }

        void StopReaction()
        {
            if (reaction != null)
            {
                StopCoroutine(reaction);
                reaction = null;
            }
            reactOffset = Vector2.zero;
            reactScale = Vector2.one;
            reactAngle = 0f;
        }

        // ------------------------------------------------------------ compose

        Vector2 FeetPos() => root.anchoredPosition + RootToFxOffset();

        // The fx layer shares the dog's parent space; root is anchored bottom-right, so convert
        // through world space to be safe whatever the parent's anchors are.
        Vector2 RootToFxOffset()
        {
            RectTransform fxRect = (RectTransform)fx.transform;
            Vector2 local = fxRect.InverseTransformPoint(root.position);
            return local - root.anchoredPosition;
        }

        /// <summary>A point on his body in the fx layer's space (0,0 = feet, 0,1 = top of the head).</summary>
        public Vector2 PointOnBody(Vector2 normalized)
        {
            RectTransform fxRect = (RectTransform)fx.transform;
            Vector3 world = body.TransformPoint(new Vector3((normalized.x - 0.5f) * Height * -0.8f, normalized.y * Height, 0f));
            return fxRect.InverseTransformPoint(world);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            talkKick = Mathf.MoveTowards(talkKick, 0f, dt * 7f);
            lean = Mathf.Lerp(lean, leanTarget, TutorialKit.Damp(6f));
            Compose();
        }

        void Compose()
        {
            if (!body)
            {
                return;
            }
            float t = Time.unscaledTime;
            // idle breathing: subtle, slow
            float breathe = Mathf.Sin(t * 2.1f) * 0.016f;
            // talking: quick bob on every word
            float talk = Mathf.Sin(talkKick * Mathf.PI) * 0.05f;

            float sx = reactScale.x * (1f - breathe * 0.6f + talk * 0.4f);
            float sy = reactScale.y * (1f + breathe - talk * 0.5f);
            body.localScale = new Vector3(sx, sy, 1f);
            body.localEulerAngles = new Vector3(0f, 0f, reactAngle + lean * 5f);
            body.anchoredPosition = reactOffset + new Vector2(lean * -14f, talk * 60f);

            float hidden = -Height - 40f;
            float y = Mathf.LerpUnclamped(hidden, 8f, enter);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, y);

            // shadow shrinks while he's in the air
            float air = Mathf.Clamp01((reactOffset.y + talk * 60f) / 120f);
            shadow.rectTransform.localScale = new Vector3(1f - air * 0.35f, 1f - air * 0.35f, 1f);
            shadow.color = TutorialKit.A(theme.ink, 0.35f * (1f - air * 0.5f) * Mathf.Clamp01(enter));
        }
    }
}
