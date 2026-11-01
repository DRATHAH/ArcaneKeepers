using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Shows the objective without a wall of text: soft arrows flow along the lanes from right to
    /// left, little skull tokens hop down a few lanes, and the castle end of the board pulses red.
    /// When a skull reaches that end it bursts with a red flash, which is exactly what the player
    /// has to prevent.
    /// </summary>
    public class LaneDemo : MonoBehaviour
    {
        class Walker
        {
            public Image img;
            public int lane;
            public float x;       // 0 = right end (spawn), 1 = castle end
            public float delay;
        }

        TutorialTheme theme;
        TutorialFx fx;
        RectTransform root;
        readonly List<Image> chevrons = new List<Image>();
        readonly List<Walker> walkers = new List<Walker>();
        Image danger;
        float t;
        float visible;
        bool showing;

        // board in world space
        float xLeft, xRight, y;
        float[] laneZ;

        public System.Action OnBreach;

        public void Init(TutorialTheme th, TutorialFx effects)
        {
            theme = th;
            fx = effects;
            root = (RectTransform)transform;

            danger = TutorialKit.Img("Danger", root, theme.softGlow, Color.clear, new Vector2(200f, 600f));
            for (int i = 0; i < 25; i++)
            {
                Image c = TutorialKit.Img("Chevron", root, theme.chevron, Color.clear, new Vector2(56f, 56f));
                chevrons.Add(c);
            }
            int[] lanes = { 1, 3, 2 };
            for (int i = 0; i < lanes.Length; i++)
            {
                Image s = TutorialKit.Img("Skull", root, theme.skullToken, Color.clear, new Vector2(86f, 86f));
                s.preserveAspect = true;
                walkers.Add(new Walker { img = s, lane = lanes[i], x = 0f, delay = i * 0.75f });
            }
            gameObject.SetActive(false);
        }

        public bool MeasureBoard()
        {
            GameObject grid = GameObject.Find("Grid");
            if (!grid || !(grid.transform is RectTransform rt))
            {
                return false;
            }
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float zMin = float.MaxValue, zMax = float.MinValue;
            xLeft = float.MaxValue;
            xRight = float.MinValue;
            foreach (Vector3 c in corners)
            {
                xLeft = Mathf.Min(xLeft, c.x);
                xRight = Mathf.Max(xRight, c.x);
                zMin = Mathf.Min(zMin, c.z);
                zMax = Mathf.Max(zMax, c.z);
            }
            y = rt.position.y + 0.25f;
            int lanes = 5;
            var layout = grid.GetComponent<GridLayoutGroup>();
            if (layout && layout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                lanes = layout.constraintCount;
            }
            laneZ = new float[lanes];
            for (int i = 0; i < lanes; i++)
            {
                laneZ[i] = Mathf.Lerp(zMax, zMin, (i + 0.5f) / lanes);
            }
            return true;
        }

        public void Show()
        {
            if (laneZ == null && !MeasureBoard())
            {
                return;
            }
            gameObject.SetActive(true);
            showing = true;
            t = 0f;
            foreach (Walker w in walkers)
            {
                w.x = 0f;
                w.delay = walkers.IndexOf(w) * 0.75f;
            }
        }

        public void Hide()
        {
            showing = false;
        }

        /// <summary>The castle end of the middle lane, in tutorial-canvas space.</summary>
        public Vector2 CastleEnd()
        {
            if (laneZ == null)
            {
                MeasureBoard();
            }
            TutorialKit.WorldToLocal(root, new Vector3(xLeft + 0.6f, y, laneZ != null ? laneZ[laneZ.Length / 2] : 0f), out Vector2 p);
            return p;
        }

        Vector2 Lane(int lane, float x01)
        {
            float wx = Mathf.Lerp(xRight - 0.4f, xLeft + 0.4f, x01);
            TutorialKit.WorldToLocal(root, new Vector3(wx, y, laneZ[Mathf.Clamp(lane, 0, laneZ.Length - 1)]), out Vector2 p);
            return p;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            visible = Mathf.MoveTowards(visible, showing ? 1f : 0f, dt * 3f);
            if (!showing && visible <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            float v = TutorialKit.Smooth(visible);

            // flowing chevrons, five per lane
            int perLane = chevrons.Count / laneZ.Length;
            for (int i = 0; i < chevrons.Count; i++)
            {
                int lane = i / perLane;
                if (lane >= laneZ.Length)
                {
                    chevrons[i].color = Color.clear;
                    continue;
                }
                float u = ((i % perLane) / (float)perLane + t * 0.12f + lane * 0.07f) % 1f;
                Vector2 p = Lane(lane, u);
                Image c = chevrons[i];
                c.rectTransform.anchoredPosition = p;
                float depth = Mathf.Lerp(0.85f, 1.15f, lane / (float)(laneZ.Length - 1));
                c.rectTransform.localScale = new Vector3(depth, depth * 0.8f, 1f);
                float edge = Mathf.Clamp01(u / 0.12f) * Mathf.Clamp01((1f - u) / 0.15f);
                c.color = TutorialKit.A(Color.white, 0.45f * edge * v);
            }

            // red pulse at the castle end
            Vector2 top = Lane(0, 1.02f), bottom = Lane(laneZ.Length - 1, 1.02f);
            RectTransform d = danger.rectTransform;
            d.anchoredPosition = (top + bottom) * 0.5f;
            d.sizeDelta = new Vector2(230f, Mathf.Abs(top.y - bottom.y) + 260f);
            danger.color = TutorialKit.A(theme.danger, (0.35f + Mathf.Sin(t * 4f) * 0.15f) * v);

            // hopping skulls
            foreach (Walker w in walkers)
            {
                if (w.delay > 0f)
                {
                    w.delay -= dt;
                    w.img.color = Color.clear;
                    continue;
                }
                w.x += dt * 0.32f;
                if (w.x >= 1f)
                {
                    Vector2 hit = Lane(w.lane, 1f);
                    if (showing)
                    {
                        fx.Flash(hit, TutorialKit.A(theme.danger, 0.9f), 260f, 0.4f);
                        fx.Ripple(hit, theme.danger, 40f, 200f, 0.45f);
                        fx.Dust(hit, 4, 80f);
                        OnBreach?.Invoke();
                    }
                    w.x = 0f;
                    w.delay = 0.6f;
                    continue;
                }
                Vector2 p = Lane(w.lane, w.x);
                float hop = Mathf.Abs(Mathf.Sin(w.x * Mathf.PI * 9f));
                w.img.rectTransform.anchoredPosition = p + new Vector2(0f, 26f + hop * 22f);
                float squash = 1f + (1f - hop) * 0.12f;
                w.img.rectTransform.localScale = new Vector3(squash, 2f - squash, 1f);
                w.img.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(w.x * Mathf.PI * 9f) * 8f);
                float fade = Mathf.Clamp01(w.x / 0.08f);
                w.img.color = TutorialKit.A(Color.white, fade * v);
            }
        }
    }
}
