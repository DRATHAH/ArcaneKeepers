using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Dims the screen gently except for a soft-edged rounded "hole" over what Dog_Hero is talking
    /// about. The hole glides from one target to the next instead of jumping, and the dim is kept
    /// light so the level never disappears behind it.
    /// </summary>
    public class TutorialSpotlight : MonoBehaviour
    {
        // SpotHole.png: the feathered hole edge sits this far inside the sprite's border
        const float HoleInset = 76f;

        TutorialTheme theme;
        RectTransform root;
        Image hole;
        Image top, bottom, left, right;

        Func<Rect> target;
        Rect current;
        bool hasRect;
        float dim;           // 0..1 current
        float dimTarget;
        float strength = 0.5f;
        float padding;

        public void Init(TutorialTheme th)
        {
            theme = th;
            root = (RectTransform)transform;
            hole = TutorialKit.Img("Hole", root, theme.spotHole, Color.clear, Vector2.one, true);
            top = Fill("Top");
            bottom = Fill("Bottom");
            left = Fill("Left");
            right = Fill("Right");
        }

        Image Fill(string n)
        {
            Image i = TutorialKit.Img(n, root, null, Color.clear, Vector2.one);
            return i;
        }

        /// <summary>Spotlight a rect (tutorial-canvas space). Passing null dims everything evenly.</summary>
        public void Show(Func<Rect> rect, float pad = 26f, float amount = 0.5f)
        {
            target = rect;
            padding = pad;
            strength = amount;
            dimTarget = 1f;
            if (rect != null && (!hasRect || dim < 0.05f))
            {
                current = Grow(rect(), pad);
                hasRect = true;
            }
        }

        public void Hide()
        {
            dimTarget = 0f;
        }

        static Rect Grow(Rect r, float pad) => new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f);

        void LateUpdate()
        {
            dim = Mathf.MoveTowards(dim, dimTarget, Time.unscaledDeltaTime * 3f);
            float a = TutorialKit.Smooth(dim) * strength;
            Color c = TutorialKit.A(theme.ink, a);

            Rect screen = root.rect;
            if (target != null)
            {
                Rect want = Grow(target(), padding);
                float k = TutorialKit.Damp(9f);
                current = Rect.MinMaxRect(
                    Mathf.Lerp(current.xMin, want.xMin, k), Mathf.Lerp(current.yMin, want.yMin, k),
                    Mathf.Lerp(current.xMax, want.xMax, k), Mathf.Lerp(current.yMax, want.yMax, k));
            }
            else
            {
                // no target: shrink the hole to nothing in the middle of the screen
                current = Rect.MinMaxRect(Mathf.Lerp(current.xMin, 0f, 0.2f), Mathf.Lerp(current.yMin, 0f, 0.2f),
                    Mathf.Lerp(current.xMax, 0f, 0.2f), Mathf.Lerp(current.yMax, 0f, 0.2f));
            }

            // hole image = the rect + its feathered border; the four fills cover the rest of the screen
            Rect h = Grow(current, HoleInset);
            Place(hole, h, c);
            Place(top, Rect.MinMaxRect(screen.xMin - 10, h.yMax, screen.xMax + 10, screen.yMax + 10), c);
            Place(bottom, Rect.MinMaxRect(screen.xMin - 10, screen.yMin - 10, screen.xMax + 10, h.yMin), c);
            Place(left, Rect.MinMaxRect(screen.xMin - 10, h.yMin, h.xMin, h.yMax), c);
            Place(right, Rect.MinMaxRect(h.xMax, h.yMin, screen.xMax + 10, h.yMax), c);
        }

        static void Place(Image img, Rect r, Color c)
        {
            bool on = c.a > 0.001f && r.width > 0f && r.height > 0f;
            img.enabled = on;
            if (!on)
            {
                return;
            }
            img.rectTransform.anchoredPosition = r.center;
            img.rectTransform.sizeDelta = r.size;
            img.color = c;
        }
    }
}
