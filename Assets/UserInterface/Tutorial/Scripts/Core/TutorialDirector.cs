using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Runs the first-level tutorial with Dog_Hero as the guide.
    ///
    /// Flow: Dog_Hero pops up in the bottom-right corner -> shows the objective (lanes, castle) ->
    /// the wave meter -> the Keeper cards -> the player drags their first Keeper onto a glowing tile
    /// (the wand demonstrates until they do) -> card resting / recharging -> a second Keeper ->
    /// moving a Keeper -> pause -> "Here they come!" and the waves are released.
    /// Info steps wait for a tap anywhere; action steps wait for the player to actually do it.
    /// During that first battle Dog_Hero pops back up to cheer the first kill, the final wave, or
    /// commiserate when a Keeper falls.
    ///
    /// Waves are held until the tutorial ends or is skipped. Finishing (or skipping) is saved, so it
    /// only plays once; TutorialDirector.ResetProgress() (or the menu item) brings it back.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public const string DoneKey = "AK.Tutorial.FirstLevel.Done";

        public TutorialTheme theme;
        [Tooltip("Skip the tutorial if the player has already finished it")]
        public bool onlyFirstTime = true;
        [Tooltip("Pop Dog_Hero back up with short reactions during the first battle")]
        public bool battleReactions = true;
        [Tooltip("Preferred tiles for the first and second Keeper, as (column, lane) from the castle end / top lane")]
        public Vector2Int[] suggestedTiles = { new Vector2Int(2, 2), new Vector2Int(2, 1), new Vector2Int(2, 3), new Vector2Int(1, 2), new Vector2Int(3, 2) };
        public TutorialLines lines = new TutorialLines();

        // ---- built at runtime
        Canvas canvas;
        RectTransform root, safe;
        TutorialSpotlight spot;
        LaneDemo lanes;
        GlowHighlight glowA, glowB;
        Image blocker;
        DogGuide dog;
        SpeechBubble bubble;
        TutorialPointer pointer;
        TutorialFx fx;
        RectTransform skipButton;
        readonly List<TileHighlight> tileGlows = new List<TileHighlight>();

        WaveSpawner heldSpawner;
        bool tapped;
        float tapReadyAt;
        bool finished;
        bool skipping;
        Coroutine flow;
        readonly HashSet<string> saidInBattle = new HashSet<string>();

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.Save();
        }

        public static bool IsDone => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        // ------------------------------------------------------------ lifecycle

        void Awake()
        {
            if (onlyFirstTime && IsDone)
            {
                enabled = false;
                return;
            }
            // Hold the waves before the spawner's first Update (it has no grace period in this level)
            heldSpawner = FindAnyObjectByType<WaveSpawner>();
            if (heldSpawner)
            {
                heldSpawner.enabled = false;
            }
        }

        void Start()
        {
            if (!enabled)
            {
                return;
            }
            if (!theme)
            {
                Debug.LogWarning("TutorialDirector has no theme; skipping the tutorial.");
                ReleaseWaves();
                return;
            }
            Build();
            flow = StartCoroutine(Run());
        }

        void OnDisable()
        {
            // never leave the level stuck without waves
            ReleaseWaves();
        }

        // ------------------------------------------------------------ building

        void Build()
        {
            var go = new GameObject("TutorialCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // over the HUD, under the pause menu (50) and end screen (60)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;

            spot = Layer<TutorialSpotlight>("Spotlight");
            spot.Init(theme);

            fx = null; // created last (drawn on top) but needed by others: build it now and reorder after
            RectTransform fxLayer = TutorialKit.Rect("Fx", root, Vector2.zero);
            TutorialKit.Stretch(fxLayer);
            fx = fxLayer.gameObject.AddComponent<TutorialFx>();
            fx.Init(theme);

            lanes = Layer<LaneDemo>("Lanes");
            lanes.Init(theme, fx);

            glowA = Layer<GlowHighlight>("GlowA");
            glowA.Init(theme, fx);
            glowB = Layer<GlowHighlight>("GlowB");
            glowB.Init(theme, fx);

            blocker = TutorialKit.Img("TapCatcher", root, null, Color.clear, Vector2.zero);
            TutorialKit.Stretch(blocker.rectTransform);
            blocker.gameObject.AddComponent<TapCatcher>().onTap = OnTap;

            safe = TutorialKit.Rect("Safe", root, Vector2.zero);
            TutorialKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            dog = new GameObject("Dog_Hero", typeof(RectTransform)).AddComponent<DogGuide>();
            dog.Init(theme, fx, safe);

            bubble = new GameObject("SpeechBubble", typeof(RectTransform)).AddComponent<SpeechBubble>();
            bubble.Init(theme, dog, safe);
            bubble.PointTailAt(new Vector2(-330f - 60f, 8f + DogGuide.Height * 0.93f));

            pointer = Layer<TutorialPointer>("Pointer");
            pointer.Init(theme, fx);

            fxLayer.SetAsLastSibling();
            BuildSkip();
            SetBlocking(false);
        }

        T Layer<T>(string name) where T : Component
        {
            RectTransform rt = TutorialKit.Rect(name, root, Vector2.zero);
            TutorialKit.Stretch(rt);
            return rt.gameObject.AddComponent<T>();
        }

        void BuildSkip()
        {
            RectTransform holder = TutorialKit.Rect("SkipArea", root, Vector2.zero);
            TutorialKit.Stretch(holder);
            holder.gameObject.AddComponent<SafeArea>();

            Image face = TutorialKit.Img("Skip", holder, theme.skipButton, Color.white, new Vector2(170f, 74f), true);
            face.raycastTarget = true;
            face.pixelsPerUnitMultiplier = 1.4f;
            skipButton = face.rectTransform;
            skipButton.anchorMin = skipButton.anchorMax = new Vector2(1f, 1f);
            skipButton.anchoredPosition = new Vector2(-95f, -205f);
            TextMeshProUGUI t = TutorialKit.Text("Label", skipButton, theme, "SKIP", 36f, theme.cream);
            if (theme.chunkyMaterial)
            {
                t.fontSharedMaterial = theme.chunkyMaterial;
            }
            TutorialKit.Stretch(t.rectTransform);
            t.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            var b = skipButton.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.onClick.AddListener(Skip);
            skipButton.localScale = Vector3.zero;
        }

        void SetBlocking(bool on)
        {
            blocker.raycastTarget = on;
        }

        // ------------------------------------------------------------ input

        void OnTap()
        {
            if (Time.unscaledTime < tapReadyAt)
            {
                return;
            }
            tapped = true;
        }

        sealed class TapCatcher : MonoBehaviour, IPointerDownHandler
        {
            public Action onTap;
            public void OnPointerDown(PointerEventData e) => onTap?.Invoke();
        }

        /// <summary>Dog_Hero says a line. With <paramref name="waitTap"/> it waits for a tap anywhere (first tap finishes the typing).</summary>
        IEnumerator Say(string line, DogMood mood = DogMood.Neutral, bool waitTap = true)
        {
            if (mood != DogMood.Neutral)
            {
                dog.React(mood);
            }
            SetBlocking(waitTap);
            tapped = false;
            IEnumerator typing = bubble.Say(line, waitTap);
            bool done = false;
            StartCoroutine(Track(typing, () => done = true));
            while (!done)
            {
                if (tapped && waitTap)
                {
                    tapped = false;
                    bubble.SkipTyping();
                }
                yield return null;
            }
            if (!waitTap)
            {
                yield break;
            }
            tapReadyAt = Time.unscaledTime + 0.25f; // no accidental double-tap skipping a line
            tapped = false;
            while (!tapped)
            {
                yield return null;
            }
            tapped = false;
            bubble.SetContinueArrow(false);
            TutorialKit.GameSound("ButtonClick");
            fx.Ripple(PointerScreenPos(), TutorialKit.A(Color.white, 0.6f), 20f, 90f, 0.3f);
        }

        IEnumerator Track(IEnumerator routine, Action onDone)
        {
            yield return routine;
            onDone();
        }

        Vector2 PointerScreenPos()
        {
            Vector2 screen = UnityEngine.InputSystem.Pointer.current != null
                ? UnityEngine.InputSystem.Pointer.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return TutorialKit.ScreenToLocal(root, screen);
        }

        // ------------------------------------------------------------ the tutorial

        IEnumerator Run()
        {
            // let the level settle and the cards deal in first
            yield return TutorialKit.Wait(1.1f);
            StartCoroutine(PopSkip());
            yield return dog.Enter();

            // ---- hello
            yield return Say(lines.welcome, DogMood.Happy);
            yield return Say(lines.intro, DogMood.Love);

            // ---- objective
            lanes.OnBreach = null;
            lanes.Show();
            yield return Say(lines.enemies, DogMood.Point);
            bool flinched = false;
            lanes.OnBreach = () =>
            {
                if (!flinched)
                {
                    flinched = true;
                    dog.React(DogMood.Worried);
                }
            };
            yield return Say(lines.castle, DogMood.Surprised);
            lanes.OnBreach = null;
            lanes.Hide();
            dog.Lean(0f);

            // ---- wave meter
            WaveProgressMeter meter = WaveProgressMeter.Instance;
            if (meter)
            {
                Func<Rect> meterRect = () => TutorialKit.RectIn(root, meter.Bar);
                spot.Show(meterRect, 34f, 0.45f);
                glowA.Show(meterRect, theme.gold, 14f);
                pointer.PointAt(() => meterRect().center + new Vector2(0f, -24f));
                yield return Say(lines.meter, DogMood.Point);
                glowA.Hide();
            }

            // ---- cards
            List<TowerButton> cards = Cards();
            if (cards.Count > 0)
            {
                Func<Rect> cardsRect = () => CardsRect(cards);
                spot.Show(cardsRect, 20f, 0.45f);
                glowA.Show(cardsRect, theme.gold, 10f);
                pointer.PointAt(() => CardPoint(cards[0]));
                yield return Say(lines.cards, DogMood.Point);
                glowA.Hide();
                spot.Hide();

                // ---- first Keeper (action)
                TowerButton first = null;
                yield return PlaceKeeper(cards, 0, lines.drag, b => first = b);
                if (first)
                {
                    string name = first.tower ? first.tower.towerName : "Your Keeper";
                    yield return Celebrate(first.createdObject);
                    yield return Say(lines.placed.Replace("{name}", name), DogMood.Cheer);

                    // ---- card rests / recharges
                    Func<Rect> cardRect = () => TutorialKit.RectIn(root, (RectTransform)first.transform);
                    spot.Show(cardRect, 16f, 0.4f);
                    glowA.Show(cardRect, theme.gold, 8f);
                    pointer.PointAt(() => CardPoint(first));
                    yield return Say(lines.cardRests, DogMood.Point);
                    glowA.Hide();
                    spot.Hide();
                    pointer.Hide();

                    // ---- second Keeper (action) if there's one to place
                    if (ReadyCards().Count > 0)
                    {
                        TowerButton second = null;
                        yield return PlaceKeeper(cards, 1, lines.second, b => second = b);
                        if (second)
                        {
                            yield return Celebrate(second.createdObject);
                            yield return Say(lines.secondPlaced, DogMood.Love);
                        }
                    }

                    // ---- moving a Keeper (action)
                    yield return RelocateLesson();
                }
            }
            else
            {
                Debug.LogWarning("Tutorial: no Keeper cards in the level (start from the character menu); skipping the placing lessons.");
            }

            // ---- pause
            GameObject pauseBtn = GameObject.Find("PauseButton");
            if (pauseBtn)
            {
                Func<Rect> pauseRect = () => TutorialKit.RectIn(root, (RectTransform)pauseBtn.transform);
                spot.Show(pauseRect, 16f, 0.4f);
                glowA.Show(pauseRect, theme.gold, 6f);
                pointer.PointAt(() => pauseRect().center);
                yield return Say(lines.pause, DogMood.Point);
                glowA.Hide();
                spot.Hide();
            }

            // ---- go!
            pointer.Hide();
            yield return Say(lines.go, DogMood.Cheer);
            yield return Finish();
        }

        // ---- placing a Keeper -------------------------------------------------------

        IEnumerator PlaceKeeper(List<TowerButton> cards, int index, string prompt, Action<TowerButton> placed)
        {
            List<TowerButton> ready = ReadyCards();
            if (ready.Count == 0)
            {
                yield break;
            }
            TowerButton demoCard = ready[0];
            Tile tile = SuggestTile(index);
            if (!tile)
            {
                yield break;
            }

            ClearTileGlows();
            tileGlows.Add(TileHighlight.Create(theme, tile, theme.gold));
            Func<Rect> cardRect = () => TutorialKit.RectIn(root, (RectTransform)demoCard.transform);
            glowB.Show(cardRect, theme.gold, 8f);
            pointer.DemoDrag(() => CardPoint(demoCard), () => TilePoint(tile), demoCard.tower ? demoCard.tower.towerIcon : null);

            // remember what's already placed BEFORE talking, so a Keeper dropped mid-sentence still counts
            HashSet<TowerButton> before = new HashSet<TowerButton>(PlacedCards());
            yield return Say(prompt, DogMood.Point, false);
            bool wasHolding = false;
            float idle = 0f;
            int nudges = 0;
            TowerButton result = null;
            while (result == null)
            {
                foreach (TowerButton b in PlacedCards())
                {
                    if (!before.Contains(b))
                    {
                        result = b;
                    }
                }
                if (result)
                {
                    break;
                }

                bool holding = IsHoldingPlacer();
                pointer.SetDimmed(holding);
                if (holding)
                {
                    idle = 0f;
                    glowB.Hide();
                }
                else if (wasHolding)
                {
                    // let go somewhere that isn't a free tile
                    glowB.Show(cardRect, theme.gold, 8f);
                    yield return Say(lines.dragMissed, DogMood.Worried, false);
                    idle = 0f;
                }
                wasHolding = holding;

                idle += Time.unscaledDeltaTime;
                if (!holding && idle > 7f)
                {
                    idle = 0f;
                    nudges++;
                    yield return Say(nudges % 2 == 1 ? lines.dragNudge : prompt, DogMood.Surprised, false);
                }
                yield return null;
            }

            pointer.Hide();
            glowB.Hide();
            ClearTileGlows();
            dog.Lean(0f);
            placed(result);
        }

        IEnumerator Celebrate(GameObject tower)
        {
            if (tower && TutorialKit.WorldToLocal(root, tower.transform.position + Vector3.up * 0.8f, out Vector2 p))
            {
                fx.Flash(p, TutorialKit.A(theme.gold, 0.9f), 320f, 0.4f);
                fx.Ripple(p, theme.gold, 40f, 240f, 0.5f);
                fx.Sparkles(p, theme.gold, 10, 520f, 48f);
                fx.Confetti(p, 10, 800f);
            }
            yield return TutorialKit.Wait(0.25f);
        }

        // ---- moving a Keeper --------------------------------------------------------

        IEnumerator RelocateLesson()
        {
            GameObject relocateBtn = GameObject.Find("RelocateButton");
            RelocationManager rm = RelocationManager.instance;
            List<TowerButton> placedCards = PlacedCards();
            if (!relocateBtn || !rm || placedCards.Count == 0)
            {
                yield break;
            }

            Func<Rect> btnRect = () => TutorialKit.RectIn(root, (RectTransform)relocateBtn.transform);
            int misses = 0;
            while (true)
            {
                // step 1: tap the move button
                glowA.Show(btnRect, theme.gold, 8f);
                pointer.PointAt(() => btnRect().center);
                yield return Say(lines.relocate, DogMood.Point, false);
                while (!rm.canRelocate)
                {
                    yield return null;
                }
                glowA.Hide();
                dog.React(DogMood.Happy);

                // step 2: drag a placed Keeper to a new tile
                Dictionary<GameObject, Transform> parents = new Dictionary<GameObject, Transform>();
                foreach (TowerButton b in PlacedCards())
                {
                    parents[b.createdObject] = b.createdObject.transform.parent;
                }
                GameObject demoTower = PlacedCards()[0].createdObject;
                Tile from = demoTower.GetComponentInParent<Tile>();
                Tile to = NeighbourTile(from);
                ClearTileGlows();
                if (to)
                {
                    tileGlows.Add(TileHighlight.Create(theme, to, theme.gold));
                    pointer.DemoDrag(() => WorldPoint(demoTower.transform.position), () => TilePoint(to), null);
                }
                yield return Say(lines.relocateDrag, DogMood.Neutral, false);

                bool moved = false;
                while (rm.canRelocate || rm.relocatedObject != null)
                {
                    if (rm.relocatedObject != null)
                    {
                        pointer.SetDimmed(true);
                    }
                    yield return null;
                }
                foreach (var kv in parents)
                {
                    if (kv.Key && kv.Key.activeInHierarchy && kv.Key.transform.parent != kv.Value && kv.Key.transform.parent != null)
                    {
                        moved = true;
                    }
                }
                // the tower can take a frame to settle onto its new tile
                if (!moved)
                {
                    yield return null;
                    foreach (var kv in parents)
                    {
                        if (kv.Key && kv.Key.transform.parent != kv.Value && kv.Key.transform.parent != null)
                        {
                            moved = true;
                        }
                    }
                }
                ClearTileGlows();
                pointer.Hide();

                if (moved)
                {
                    GameObject t = null;
                    foreach (var kv in parents)
                    {
                        if (kv.Key && kv.Key.transform.parent != kv.Value) t = kv.Key;
                    }
                    yield return Celebrate(t);
                    yield return Say(lines.relocateDone, DogMood.Happy);
                    yield break;
                }

                misses++;
                if (misses >= 2)
                {
                    yield return Say(lines.relocateGiveUp, DogMood.Love);
                    yield break;
                }
                yield return Say(lines.relocateMissed, DogMood.Worried);
                // wait out the move cooldown so the button works again
                float wait = 0f;
                while (!relocateBtn.GetComponent<Button>().interactable && wait < 3f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        // ---- wrap up ---------------------------------------------------------------

        IEnumerator Finish()
        {
            finished = true;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            SetBlocking(false);
            pointer.Hide();
            glowA.Hide();
            glowB.Hide();
            spot.Hide();
            ClearTileGlows();
            StartCoroutine(HideSkip());

            // the meter gets a little flourish as the battle starts
            if (WaveProgressMeter.Instance)
            {
                Rect r = TutorialKit.RectIn(root, WaveProgressMeter.Instance.Bar);
                fx.Sparkles(r.center, theme.gold, 10, 420f, 40f);
                fx.Ripple(r.center, theme.gold, 60f, 420f, 0.5f);
            }
            yield return bubble.Close();
            yield return dog.Exit();
            ReleaseWaves();

            if (battleReactions)
            {
                StartCoroutine(BattleReactions());
            }
        }

        void Skip()
        {
            if (finished || skipping)
            {
                return;
            }
            skipping = true;
            TutorialKit.GameSound("ButtonClick");
            if (flow != null)
            {
                StopCoroutine(flow);
            }
            StartCoroutine(SkipRoutine());
        }

        IEnumerator SkipRoutine()
        {
            finished = true;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            SetBlocking(false);
            pointer.Hide();
            glowA.Hide();
            glowB.Hide();
            spot.Hide();
            lanes.Hide();
            ClearTileGlows();
            dog.Lean(0f);
            StartCoroutine(HideSkip());
            ReleaseWaves();
            if (!dog.Visible)
            {
                yield return dog.Enter();
            }
            yield return Say(lines.skipped, DogMood.Happy, false);
            yield return TutorialKit.Wait(1.2f);
            yield return bubble.Close();
            yield return dog.Exit();
        }

        void ReleaseWaves()
        {
            if (heldSpawner)
            {
                heldSpawner.enabled = true;
                heldSpawner = null;
            }
        }

        IEnumerator PopSkip()
        {
            yield return TutorialKit.Tween(0.35f, p => skipButton.localScale = Vector3.one * TutorialKit.EaseOutBack(p, 2f));
        }

        IEnumerator HideSkip()
        {
            Vector3 from = skipButton.localScale;
            yield return TutorialKit.Tween(0.2f, p => skipButton.localScale = Vector3.Lerp(from, Vector3.zero, TutorialKit.EaseIn(p)));
            skipButton.gameObject.SetActive(false);
        }

        // ---- the first battle ----------------------------------------------------------

        IEnumerator BattleReactions()
        {
            WaveProgressMeter meter = WaveProgressMeter.Instance;
            bool finalWave = false;
            if (meter)
            {
                meter.OnFinalWave += () => finalWave = true;
            }
            int lastKills = 0;
            int seenEnemies = 0;
            Dictionary<TowerButton, bool> wasPlaced = new Dictionary<TowerButton, bool>();
            WaveSpawner ws = WaveSpawner.instance;

            while (true)
            {
                if (WinLossManager.instance && (WinLossManager.instance.winner || WinLossManager.instance.loser))
                {
                    yield break;
                }

                // first enemy defeated: count how many have ever spawned versus how many are left
                if (ws)
                {
                    seenEnemies = Mathf.Max(seenEnemies, ws.enemiesSpawned.Count + lastKills);
                    int kills = seenEnemies - ws.enemiesSpawned.Count;
                    if (kills > lastKills)
                    {
                        lastKills = kills;
                        yield return Quip("kill", lines.firstKill, DogMood.Cheer);
                    }
                }

                foreach (TowerButton b in Cards())
                {
                    bool placed = b.createdObject != null && b.createdObject.activeInHierarchy;
                    if (wasPlaced.TryGetValue(b, out bool was) && was && !placed && b.CooldownFraction > 0f)
                    {
                        yield return Quip("down", lines.keeperDown, DogMood.Worried);
                    }
                    wasPlaced[b] = placed;
                }

                if (finalWave)
                {
                    finalWave = false;
                    yield return Quip("final", lines.finalWave, DogMood.Surprised);
                }
                yield return null;
            }
        }

        /// <summary>Dog_Hero pops up, says something short, and ducks back down (once per kind).</summary>
        IEnumerator Quip(string key, string line, DogMood mood)
        {
            if (!saidInBattle.Add(key))
            {
                yield break;
            }
            yield return dog.Enter();
            yield return Say(line, mood, false);
            yield return TutorialKit.Wait(1.8f);
            yield return bubble.Close();
            yield return dog.Exit();
        }

        // ------------------------------------------------------------ queries

        List<TowerButton> Cards()
        {
            var list = new List<TowerButton>();
            Transform parent = GameManager.instance ? GameManager.instance.towerButtonParent : null;
            if (!parent)
            {
                return list;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                var b = parent.GetChild(i).GetComponent<TowerButton>();
                if (b && b.gameObject.activeInHierarchy)
                {
                    list.Add(b);
                }
            }
            return list;
        }

        List<TowerButton> PlacedCards()
        {
            var list = new List<TowerButton>();
            foreach (TowerButton b in Cards())
            {
                if (b.createdObject != null && b.createdObject.activeInHierarchy)
                {
                    list.Add(b);
                }
            }
            return list;
        }

        List<TowerButton> ReadyCards()
        {
            var list = new List<TowerButton>();
            foreach (TowerButton b in Cards())
            {
                if ((b.createdObject == null || !b.createdObject.activeInHierarchy) && b.CooldownFraction <= 0f)
                {
                    list.Add(b);
                }
            }
            return list;
        }

        static bool IsHoldingPlacer()
        {
            RelocationManager rm = RelocationManager.instance;
            return rm && rm.relocatedObject && rm.relocatedObject.activeInHierarchy && rm.relocatedObject.GetComponent<TowerPlacer>();
        }

        Rect CardsRect(List<TowerButton> cards)
        {
            Rect r = TutorialKit.RectIn(root, (RectTransform)cards[0].transform);
            for (int i = 1; i < cards.Count; i++)
            {
                r = TutorialKit.Encapsulate(r, TutorialKit.RectIn(root, (RectTransform)cards[i].transform));
            }
            return r;
        }

        Vector2 CardPoint(TowerButton b)
        {
            Rect r = TutorialKit.RectIn(root, (RectTransform)b.transform);
            return r.center + new Vector2(r.width * 0.12f, -r.height * 0.05f);
        }

        Vector2 WorldPoint(Vector3 world)
        {
            TutorialKit.WorldToLocal(root, world, out Vector2 p);
            return p;
        }

        Vector2 TilePoint(Tile tile)
        {
            Transform t = tile.anchorPos ? tile.anchorPos : tile.transform;
            return WorldPoint(t.position);
        }

        Tile[] Tiles()
        {
            Transform grid = GameManager.instance && GameManager.instance.lanes ? GameManager.instance.lanes.transform : null;
            return grid ? grid.GetComponentsInChildren<Tile>() : new Tile[0];
        }

        int LaneCount()
        {
            GridLayoutGroup g = GameManager.instance ? GameManager.instance.lanes : null;
            return g && g.constraint == GridLayoutGroup.Constraint.FixedRowCount ? g.constraintCount : 5;
        }

        Tile TileAt(Tile[] tiles, int column, int lane)
        {
            // the grid fills column by column (vertical start axis), top lane first
            int rows = LaneCount();
            int i = column * rows + lane;
            return i >= 0 && i < tiles.Length ? tiles[i] : null;
        }

        Tile SuggestTile(int nth)
        {
            Tile[] tiles = Tiles();
            int skip = nth;
            foreach (Vector2Int c in suggestedTiles)
            {
                Tile t = TileAt(tiles, c.x, c.y);
                if (t && t.tileObject == null)
                {
                    if (skip-- <= 0 || nth == 0)
                    {
                        return t;
                    }
                }
            }
            foreach (Tile t in tiles)
            {
                if (t.tileObject == null) return t;
            }
            return null;
        }

        Tile NeighbourTile(Tile from)
        {
            Tile[] tiles = Tiles();
            int idx = Array.IndexOf(tiles, from);
            int rows = LaneCount();
            if (idx < 0)
            {
                return SuggestTile(0);
            }
            int col = idx / rows, lane = idx % rows;
            int[][] tries = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { -1, 0 }, new[] { 2, 0 } };
            foreach (int[] d in tries)
            {
                int l = lane + d[1];
                if (l < 0 || l >= rows) continue;
                Tile t = TileAt(tiles, col + d[0], l);
                if (t && t.tileObject == null) return t;
            }
            return SuggestTile(0);
        }

        void ClearTileGlows()
        {
            foreach (TileHighlight h in tileGlows)
            {
                if (h) h.Hide();
            }
            tileGlows.Clear();
        }
    }
}
