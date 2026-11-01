using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Level progress meter in the spirit of Plants vs. Zombies: a hand-drawn wooden plank at the top of
    /// the screen whose groove fills green from right to left (the way the enemies march), a skull token
    /// riding the tip, and a flag for every wave that pops up as the fill reaches it. The label says which wave is on and
    /// turns red for the final one. Reads WaveSpawner / WaveManager, so it works in any level.
    /// </summary>
    public class WaveProgressMeter : MonoBehaviour
    {
        public TutorialTheme theme;
        [Tooltip("Size of the plank in reference pixels (1920x1080 canvas); keep the art's 740:88 shape")]
        public float width = 740f;
        public float height = 88f;
        [Tooltip("Where the groove's fill starts / ends across the plank art (0-1), from GenerateTutorialArt")]
        public float fillMin = 70f / 1480f;
        public float fillMax = 1410f / 1480f;

        RectTransform bar;
        Image fill;
        RectTransform token;
        TextMeshProUGUI label;
        readonly List<RectTransform> flags = new List<RectTransform>();
        readonly List<float> flagPop = new List<float>();
        CanvasGroup group;

        // progress tracking
        int lastWave = -1;
        readonly HashSet<EnemyHealth> seen = new HashSet<EnemyHealth>();
        int killedThisWave;
        float progress;      // shown, eased
        float popLabel = -1f;
        bool finalShown;

        public static WaveProgressMeter Instance { get; private set; }

        /// <summary>The bar's rect (for highlights).</summary>
        public RectTransform Bar => bar;

        /// <summary>Fires when the last wave starts.</summary>
        public System.Action OnFinalWave;

        // The spawner bumps currentWave before spawning, so the first wave played is (starting value + 1).
        // Levels start at -1 (all waves play); a level starting at 0 skips wave 0. Both are handled.
        int firstWave = -2;

        int FirstWave
        {
            get
            {
                if (firstWave == -2)
                {
                    WaveSpawner ws = WaveSpawner.instance ? WaveSpawner.instance : FindAnyObjectByType<WaveSpawner>();
                    firstWave = ws ? ws.currentWave + 1 : 0;
                }
                return firstWave;
            }
        }

        int TotalWaves => WaveManager.instance ? Mathf.Max(1, WaveManager.instance.levelWaves.Count - FirstWave) : 1;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Build()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;
            }
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform safe = TutorialKit.Rect("Safe", transform, Vector2.zero);
            TutorialKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            bar = TutorialKit.Rect("WaveMeter", safe, new Vector2(width, height));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(60f, -76f);

            Image back = TutorialKit.Img("Back", bar, theme.meterBack, Color.white, Vector2.zero);
            TutorialKit.Stretch(back.rectTransform);

            // the fill art covers the whole plank; Filled from the right reveals the groove right to left
            fill = TutorialKit.Img("Fill", bar, theme.meterFill, Color.white, Vector2.zero);
            TutorialKit.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Right;
            fill.fillAmount = 0f;

            Image tok = TutorialKit.Img("Skull", bar, theme.skullToken, Color.white, new Vector2(92f, 92f));
            tok.preserveAspect = true;
            token = tok.rectTransform;
            token.anchorMin = token.anchorMax = new Vector2(1f, 0.5f);

            label = TutorialKit.Text("Label", bar, theme, "GET READY", 44f, theme.cream);
            if (theme.chunkyMaterial)
            {
                label.fontSharedMaterial = theme.chunkyMaterial;
            }
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.Right;
            RectTransform lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0f, 0.5f);
            lr.pivot = new Vector2(1f, 0.5f);
            lr.sizeDelta = new Vector2(320f, 70f);
            lr.anchoredPosition = new Vector2(-8f, 2f);
        }

        // Flags need the level's wave list, which may not exist yet in Awake
        void BuildFlags()
        {
            int n = TotalWaves;
            for (int i = 1; i <= n; i++)
            {
                Image f = TutorialKit.Img("Flag" + i, bar, theme.waveFlag, Color.white, new Vector2(54f, 78f));
                f.preserveAspect = true;
                RectTransform r = f.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(Mathf.Lerp(fillMax, fillMin, i / (float)n), 0.5f);
                r.pivot = new Vector2(0.18f, 0.08f); // foot of the pole
                r.anchoredPosition = new Vector2(0f, -14f);
                r.SetSiblingIndex(token.GetSiblingIndex()); // under the skull
                flags.Add(r);
                flagPop.Add(-1f);
            }
        }

        void Start()
        {
            BuildFlags();
        }

        void Update()
        {
            WaveSpawner ws = WaveSpawner.instance;
            float target = progress;
            int n = TotalWaves;
            if (ws)
            {
                int index = ws.currentWave;
                int wave = index - FirstWave + 1; // 1-based wave number, 0 = not started
                if (wave != lastWave)
                {
                    lastWave = wave;
                    seen.Clear();
                    killedThisWave = 0;
                    if (wave >= 1)
                    {
                        popLabel = 0f;
                    }
                    if (wave >= n && n > 0 && !finalShown)
                    {
                        finalShown = true;
                        OnFinalWave?.Invoke();
                    }
                }
                // count kills: anything we saw in this wave's list that has since left it
                foreach (EnemyHealth e in ws.enemiesThisWave)
                {
                    if (e)
                    {
                        seen.Add(e);
                    }
                }
                int alive = 0;
                foreach (EnemyHealth e in ws.enemiesThisWave)
                {
                    if (e && seen.Contains(e)) alive++;
                }
                killedThisWave = Mathf.Max(killedThisWave, seen.Count - alive);

                float waveSize = 1f;
                if (WaveManager.instance && index >= 0 && index < WaveManager.instance.levelWaves.Count)
                {
                    waveSize = Mathf.Max(1, WaveManager.instance.levelWaves[index].waveEnemies.Length);
                }
                float within = wave >= 1 ? Mathf.Clamp01(killedThisWave / waveSize) : 0f;
                target = wave >= 1 ? Mathf.Clamp01((wave - 1 + within) / n) : 0f;

                bool done = wave >= n && ws.enemiesSpawned.Count == 0 && killedThisWave > 0;
                if (done)
                {
                    target = 1f;
                }
                label.text = wave < 1 ? "GET READY" : wave >= n ? "FINAL WAVE" : "WAVE " + wave + "/" + n;
                label.color = wave >= n ? theme.danger : theme.cream;
            }
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime * 0.6f);
            progress = Mathf.Lerp(progress, target, TutorialKit.Damp(3f));

            float full = bar.rect.width;
            float inner = (fillMax - fillMin) * full;
            fill.fillAmount = (1f - fillMax) + progress * (fillMax - fillMin);
            fill.enabled = progress > 0.001f;

            float tt = Time.unscaledTime;
            float x = -(1f - fillMax) * full - progress * inner;
            token.anchoredPosition = new Vector2(x, 2f + Mathf.Abs(Mathf.Sin(tt * 5f)) * 3f);
            token.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(tt * 5f) * 6f);

            for (int i = 0; i < flags.Count; i++)
            {
                float at = (i + 1) / (float)n;
                bool reached = progress >= at - 0.002f;
                if (reached && flagPop[i] < 0f)
                {
                    flagPop[i] = 0f;
                }
                float s = 0.7f;
                float rot = 0f;
                if (flagPop[i] >= 0f)
                {
                    flagPop[i] += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(flagPop[i] / 0.45f);
                    s = Mathf.Lerp(0.7f, 1.1f, TutorialKit.EaseOutBack(p, 3f));
                    rot = Mathf.Sin(tt * 3f + i) * 5f;
                }
                flags[i].localScale = Vector3.one * s;
                flags[i].localEulerAngles = new Vector3(0f, 0f, rot);
            }

            float ls = 1f;
            if (popLabel >= 0f)
            {
                popLabel += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(popLabel / 0.5f);
                ls = 1f + Mathf.Sin(p * Mathf.PI) * 0.25f;
                if (p >= 1f) popLabel = -1f;
            }
            label.rectTransform.localScale = Vector3.one * ls;
        }
    }
}
