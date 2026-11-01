using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Lightweight UI particle layer for the tutorial: sparkles, ripples, dust puffs and gold-star
    /// confetti. Particles are pooled Images updated in one loop on unscaled time, so a burst costs
    /// nothing once it's done and keeps playing while the game is paused.
    /// </summary>
    public class TutorialFx : MonoBehaviour
    {
        class Particle
        {
            public Image img;
            public Vector2 pos, vel;
            public float gravity, drag;
            public float age, life;
            public float size0, size1;
            public float spin, angle;
            public Color color;
            public bool fadeIn;
        }

        TutorialTheme theme;
        RectTransform layer;
        readonly List<Particle> live = new List<Particle>();
        readonly Stack<Image> pool = new Stack<Image>();

        public void Init(TutorialTheme t)
        {
            theme = t;
            layer = (RectTransform)transform;
        }

        // ------------------------------------------------------------ presets

        /// <summary>Thin ring that expands and fades (taps, landings).</summary>
        public void Ripple(Vector2 pos, Color color, float from = 30f, float to = 150f, float life = 0.45f)
        {
            Spawn(theme.ring, pos, Vector2.zero, color, from, to, life, 0f, 0f, 0f);
        }

        /// <summary>Four-point twinkles flying out from a point.</summary>
        public void Sparkles(Vector2 pos, Color color, int count, float speed = 420f, float size = 46f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.55f, 1f);
                Spawn(theme.sparkle, pos, v, color, size * Random.Range(0.7f, 1.1f), 0f, Random.Range(0.45f, 0.7f), -200f, 4f, Random.Range(-200f, 200f));
            }
        }

        /// <summary>A soft flash of light.</summary>
        public void Flash(Vector2 pos, Color color, float size = 220f, float life = 0.35f)
        {
            Spawn(theme.softGlow, pos, Vector2.zero, color, size * 0.6f, size, life, 0f, 0f, 0f);
        }

        /// <summary>Chunky gold stars that pop up and fall (celebrations).</summary>
        public void Confetti(Vector2 pos, int count, float power = 900f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Deg2Rad * Random.Range(40f, 140f);
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * power * Random.Range(0.55f, 1f);
                Spawn(theme.starChunk, pos, v, Color.white, Random.Range(28f, 44f), 0f, Random.Range(0.9f, 1.3f), -2200f, 1.2f, Random.Range(-420f, 420f), size1Factor: 0.6f);
            }
        }

        /// <summary>Little dust clouds puffing sideways from a landing.</summary>
        public void Dust(Vector2 pos, int count, float spread = 140f)
        {
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector2 v = new Vector2(side * Random.Range(160f, 320f), Random.Range(30f, 110f));
                Vector2 p = pos + new Vector2(side * Random.Range(10f, spread * 0.4f), 0f);
                Spawn(theme.puff, p, v, new Color(1f, 0.98f, 0.92f, 0.85f), Random.Range(34f, 52f), Random.Range(70f, 96f), Random.Range(0.4f, 0.6f), 120f, 5f, 0f);
            }
        }

        /// <summary>One slow twinkle drifting upward (ambient sparkle around highlights / the wand).</summary>
        public void Mote(Vector2 pos, Color color, float size = 26f)
        {
            Vector2 v = new Vector2(Random.Range(-30f, 30f), Random.Range(40f, 110f));
            var p = Spawn(theme.sparkle, pos, v, color, size, 0f, Random.Range(0.6f, 0.9f), 0f, 1.5f, Random.Range(-90f, 90f));
            p.fadeIn = true;
        }

        // ------------------------------------------------------------ core

        Particle Spawn(Sprite sprite, Vector2 pos, Vector2 vel, Color color, float size0, float size1, float life,
            float gravity, float drag, float spin, float size1Factor = -1f)
        {
            Image img = pool.Count > 0 ? pool.Pop() : TutorialKit.Img("P", layer, null, Color.white, Vector2.one);
            img.gameObject.SetActive(true);
            img.sprite = sprite;
            img.transform.SetAsLastSibling();
            var p = new Particle
            {
                img = img, pos = pos, vel = vel, color = color, size0 = size0,
                size1 = size1Factor >= 0f ? size0 * size1Factor : size1,
                life = life, gravity = gravity, drag = drag, spin = spin, angle = Random.Range(0f, 360f),
            };
            live.Add(p);
            Apply(p, 0f);
            return p;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle p = live[i];
                p.age += dt;
                if (p.age >= p.life || !p.img)
                {
                    if (p.img)
                    {
                        p.img.gameObject.SetActive(false);
                        pool.Push(p.img);
                    }
                    live.RemoveAt(i);
                    continue;
                }
                p.vel.y += p.gravity * dt;
                p.vel *= Mathf.Exp(-p.drag * dt);
                p.pos += p.vel * dt;
                p.angle += p.spin * dt;
                Apply(p, p.age / p.life);
            }
        }

        static void Apply(Particle p, float t)
        {
            var rt = p.img.rectTransform;
            float s = Mathf.Lerp(p.size0, p.size1, TutorialKit.EaseOut(t));
            rt.sizeDelta = new Vector2(s, s);
            rt.anchoredPosition = p.pos;
            rt.localEulerAngles = new Vector3(0f, 0f, p.angle);
            float a = 1f - t * t;
            if (p.fadeIn)
            {
                a *= Mathf.Clamp01(t * 5f);
            }
            p.img.color = TutorialKit.A(p.color, p.color.a * a);
        }

        public void Clear()
        {
            foreach (Particle p in live)
            {
                if (p.img)
                {
                    p.img.gameObject.SetActive(false);
                    pool.Push(p.img);
                }
            }
            live.Clear();
        }
    }
}
