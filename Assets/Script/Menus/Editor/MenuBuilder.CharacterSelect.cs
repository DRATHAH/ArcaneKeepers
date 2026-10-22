using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the character select screen into the CharacterScene file (replacing the old tower select layout):
// a swipeable lineup of full-body keepers on platforms, the centred keeper's name shown big beside it,
// and four round squad medallions with DEPLOY. No boxes: everything floats over the background.
// The scene file keeps its GUID and name, so every existing reference (main menu "Tap to play",
// LevelSelect "Change Team", LevelButton fallback) still works.
public static partial class MenuBuilder
{
    const string HeroFolder = "Assets/UserInterface/Heroes/";

    // Lineup order: the existing towers, their hero art and a colour for each
    struct KeeperInfo
    {
        public string tower, hero, name;
        public Color accent;
        public float neckX, neckY;
        public int health, power, range; // 1-5 pips; health follows the prefabs' maxHealth (150-750)
    }

    static readonly KeeperInfo[] Keepers =
    {
        new KeeperInfo { tower = "KnightTower", hero = "Dog-Hero", name = "Knight", accent = new Color32(240, 88, 64, 255), neckX = 0.55f, neckY = 0.6f, health = 5, power = 2, range = 1 },
        new KeeperInfo { tower = "ArcherTower", hero = "Ferret-Hero", name = "Archer", accent = new Color32(98, 210, 84, 255), neckX = 0.4f, neckY = 0.6f, health = 2, power = 3, range = 5 },
        new KeeperInfo { tower = "ClericTower", hero = "Goat-Hero", name = "Cleric", accent = new Color32(255, 200, 60, 255), neckX = 0.5f, neckY = 0.62f, health = 1, power = 1, range = 3 },
        new KeeperInfo { tower = "AssassinTower", hero = "Mouse-Hero", name = "Assassin", accent = new Color32(196, 112, 245, 255), neckX = 0.62f, neckY = 0.62f, health = 3, power = 5, range = 2 },
        new KeeperInfo { tower = "WizardTower", hero = "Cat-Hero", name = "Wizard", accent = new Color32(80, 156, 255, 255), neckX = 0.45f, neckY = 0.52f, health = 3, power = 4, range = 4 },
    };

    static readonly string[] StatNames = { "HEALTH", "POWER", "RANGE" };

    // Unscaled sizes; CharacterSelectLayout scales them to fit the screen
    static readonly Vector2 IntelSize = new Vector2(560, 470);
    static readonly Vector2 KeeperCardSize = new Vector2(370, 560);
    const float HeaderHeight = 150, FooterHeight = 270;
    const float SlotWidth = 190, SlotSpacing = 22, DiscSize = 150;

    [MenuItem("Arcane Keepers/Rebuild Character Select")]
    public static void BuildCharacterSelectOnly()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Menu Builder", "Exit Play Mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }
        string startScene = SceneManager.GetActiveScene().path;
        BuildCharacterSelectScene();
        if (!string.IsNullOrEmpty(startScene) && startScene != ScenesFolder + CharacterScene + ".unity")
        {
            EditorSceneManager.OpenScene(startScene);
        }
        Debug.Log("Menu Builder: character select rebuilt.");
    }

    static void BuildCharacterSelectScene()
    {
        LoadStyleAssets();
        EnsureCharacterSelectAssets();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndEventSystem();

        // Same managers the old screen had: the team is saved through Inventory -> PlayerManager
        GameObject managers = new GameObject("==Managers==");
        GameObject inventoryGo = new GameObject("Inventory");
        inventoryGo.transform.SetParent(managers.transform, false);
        inventoryGo.AddComponent<Inventory>();
        PlayerManager playerManager = new GameObject("PlayerManager").AddComponent<PlayerManager>();

        Transform canvas = CreateMenuCanvas("Canvas");
        SceneLoader loader = canvas.gameObject.AddComponent<SceneLoader>();
        CanvasGroup screenGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        CharacterSelectScreen screen = canvas.gameObject.AddComponent<CharacterSelectScreen>();
        screen.sceneLoader = loader;
        screen.screenGroup = screenGroup;
        screen.playScene = LevelSelectScene;
        screen.backScene = MainMenuScene;

        CreateMenuBackground(canvas);
        Image shade = CreatePanel("Shade", canvas, new Color(Ink.r, Ink.g, Ink.b, 0.3f));
        shade.sprite = null;
        shade.raycastTarget = false;
        Stretch(shade.rectTransform);
        shade.transform.SetSiblingIndex(2);

        Transform safe = CreateSafeArea(canvas);

        // Characters, in lineup order
        List<CharacterProfile> profiles = new List<CharacterProfile>();
        foreach (KeeperInfo k in Keepers)
        {
            Tower tower = AssetDatabase.LoadAssetAtPath<Tower>("Assets/Towers/" + k.tower + ".asset");
            profiles.Add(new CharacterProfile
            {
                tower = tower,
                heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroFolder + k.hero + ".png"),
                displayName = tower && !string.IsNullOrEmpty(tower.towerName) ? tower.towerName : k.name,
                accent = k.accent,
                health = k.health,
                power = k.power,
                range = k.range,
            });
            playerManager.cheatedTowers.Add(new TowerEntry { tower = tower, amount = 1 });
        }
        screen.characters = profiles.ToArray();

        BuildHeader(safe, screen);
        RectTransform intel = BuildNameBlock(safe, screen);
        BuildLineup(safe, screen);
        BuildRoster(safe, screen);

        // Sizes the name block and the keepers to whatever screen the game runs on
        CharacterSelectLayout layout = safe.gameObject.AddComponent<CharacterSelectLayout>();
        layout.area = (RectTransform)safe;
        layout.intelPanel = intel;
        layout.lineup = (RectTransform)screen.scroller.transform;
        layout.scroller = screen.scroller;
        layout.cards = screen.cards;
        layout.headerHeight = HeaderHeight;
        layout.footerHeight = FooterHeight;
        layout.intelSize = IntelSize;
        layout.cardSize = KeeperCardSize;
        layout.gap = -40; // the soft glow behind the name can overlap the lineup's faded edge

        // Flying portraits go above everything
        GameObject fly = new GameObject("FlyLayer", typeof(RectTransform));
        fly.transform.SetParent(canvas, false);
        Stretch((RectTransform)fly.transform);
        screen.flyLayer = (RectTransform)fly.transform;

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenesFolder + CharacterScene + ".unity");
    }

    static void LoadStyleAssets()
    {
        if (!styleFont)
        {
            styleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StyleFolder + "LilitaOne SDF.asset");
        }
        if (!outlineMaterial)
        {
            outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Outline.mat");
        }
        if (!chunkyMaterial)
        {
            chunkyMaterial = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Chunky.mat");
        }
    }

    #region Character select layout

    static void BuildHeader(Transform safe, CharacterSelectScreen screen)
    {
        Button back = CreateChunkyButton("BackButton", safe, "Back", "CC_Blue", new Vector2(0f, 1f), new Vector2(140, -78), new Vector2(210, 100), 50);
        UnityEventTools.AddVoidPersistentListener(back.onClick, screen.Back);

        // On the canvas, not the safe area, so a one-sided notch doesn't push the title off centre
        Image banner = CreatePanel("Banner", safe.parent, Color.white);
        banner.sprite = StyleSprite("CC_Banner");
        banner.type = Image.Type.Sliced;
        banner.pixelsPerUnitMultiplier = 200f / 140f;
        banner.raycastTarget = false;
        Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(740, 140));
        TextMeshProUGUI title = CreateChunkyText("Title", banner.transform, "Choose Your Squad", 64, Color.white);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(560, 80));
    }

    // Left: the keeper in the middle of the lineup, Fortnite-lobby style. A big name, a slanted
    // status tag and a row of dots, floating on a soft glow of the keeper's colour (no panel).
    static RectTransform BuildNameBlock(Transform safe, CharacterSelectScreen screen)
    {
        GameObject block = new GameObject("NameBlock", typeof(RectTransform));
        block.transform.SetParent(safe, false);
        RectTransform rt = (RectTransform)block.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = IntelSize;
        rt.anchoredPosition = new Vector2(24, FooterHeight + (1080 - HeaderHeight - FooterHeight) / 2);

        // Two soft glows instead of a box: dark for contrast, the keeper's colour on top
        Image dark = CreateImage("Shadow", rt, StyleSprite("CS_Glow"));
        dark.preserveAspect = false;
        dark.color = new Color(Ink.r, Ink.g, Ink.b, 0.55f);
        Place(dark.rectTransform, new Vector2(0.45f, 0.5f), Vector2.zero, new Vector2(880, 720));
        Image glow = CreateImage("Glow", rt, StyleSprite("CS_Glow"));
        glow.preserveAspect = false;
        glow.color = new Color(1, 1, 1, 0.35f);
        Place(glow.rectTransform, new Vector2(0.4f, 0.62f), Vector2.zero, new Vector2(640, 420));
        screen.intelGlow = glow;

        GameObject inner = new GameObject("Content", typeof(RectTransform));
        inner.transform.SetParent(rt, false);
        Stretch((RectTransform)inner.transform);
        CanvasGroup group = inner.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        screen.intelGroup = group;

        Image tag = CreateImage("Tag", inner.transform, StyleSprite("CS_Slant"));
        tag.type = Image.Type.Sliced;
        tag.preserveAspect = false;
        // Everything lines up on one left edge: tag, name, stat rows, lineup dots
        const float left = 24;
        TopLeft(tag.rectTransform, new Vector2(left, -30), new Vector2(290, 64));
        screen.intelTag = tag;
        screen.intelStatus = CreateChunkyText("Status", tag.transform, "TAP TO ADD", 34, Color.white);
        Stretch(screen.intelStatus.rectTransform);
        screen.intelStatus.rectTransform.offsetMin = new Vector2(30, 6);
        screen.intelStatus.rectTransform.offsetMax = new Vector2(-30, -4);

        TextMeshProUGUI name = CreateChunkyText("Name", inner.transform, "Cleric", 140, Color.white);
        name.alignment = TextAlignmentOptions.Left;
        name.fontSizeMin = 80;
        TopLeft(name.rectTransform, new Vector2(left - 6, -100), new Vector2(540, 150));
        screen.intelName = name;

        // Stats: a short label and five chunky pips each (dark wells, gold fills like the loading bar)
        const float pipWidth = 50, pipHeight = 26, pipGap = 8, labelWidth = 128;
        List<StatPips> stats = new List<StatPips>();
        for (int s = 0; s < StatNames.Length; s++)
        {
            GameObject row = new GameObject("Stat" + StatNames[s], typeof(RectTransform));
            row.transform.SetParent(inner.transform, false);
            TopLeft((RectTransform)row.transform, new Vector2(left, -270 - s * 48), new Vector2(labelWidth + 5 * (pipWidth + pipGap), 40));

            TextMeshProUGUI label = CreateChunkyText("Label", row.transform, StatNames[s], 28, new Color(1, 1, 1, 0.9f));
            label.alignment = TextAlignmentOptions.Left;
            TopLeft(label.rectTransform, Vector2.zero, new Vector2(labelWidth, 40));

            List<Image> fills = new List<Image>();
            for (int i = 0; i < 5; i++)
            {
                Image well = CreateImage("Pip" + (i + 1), row.transform, StyleSprite("CC_BarBg"));
                well.type = Image.Type.Sliced;
                well.preserveAspect = false;
                well.pixelsPerUnitMultiplier = 1.4f;
                TopLeft(well.rectTransform, new Vector2(labelWidth + i * (pipWidth + pipGap), -(40 - pipHeight) / 2), new Vector2(pipWidth, pipHeight));

                Image fill = CreateImage("Fill", well.transform, StyleSprite("CC_BarFill"));
                fill.type = Image.Type.Sliced;
                fill.preserveAspect = false;
                fill.pixelsPerUnitMultiplier = 1.8f;
                Stretch(fill.rectTransform);
                fill.rectTransform.offsetMin = new Vector2(4, 4);
                fill.rectTransform.offsetMax = new Vector2(-4, -4);
                fills.Add(fill);
            }
            StatPips pips = row.AddComponent<StatPips>();
            pips.fills = fills.ToArray();
            stats.Add(pips);
        }
        screen.intelStats = stats.ToArray();

        // One dot per keeper: white = where you are, gold = in the squad
        List<Image> dots = new List<Image>();
        for (int i = 0; i < Keepers.Length; i++)
        {
            Image dot = CreateImage("Dot" + (i + 1), inner.transform, StyleSprite("CS_Dot"));
            Place(dot.rectTransform, new Vector2(0f, 1f), new Vector2(left + 13 + i * 44, -440), new Vector2(26, 26));
            dots.Add(dot);
        }
        screen.lineupDots = dots.ToArray();

        foreach (Graphic g in rt.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
        return rt;
    }

    // Middle: horizontally swipeable lineup of keepers on platforms
    static void BuildLineup(Transform safe, CharacterSelectScreen screen)
    {
        GameObject root = new GameObject("Lineup", typeof(RectTransform));
        root.transform.SetParent(safe, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(24 + IntelSize.x - 40, FooterHeight);
        rt.offsetMax = new Vector2(-10, -HeaderHeight);
        Image hit = root.AddComponent<Image>();
        hit.color = Color.clear; // lets swipes start between keepers
        // No mask: keepers fade out near the edges (LineupScroller) instead of being cut off

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(root.transform, false);
        RectTransform content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0, 0);
        content.anchorMax = new Vector2(0, 1);
        content.pivot = new Vector2(0, 0.5f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        HorizontalLayoutGroup layout = contentGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 0;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = rt;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.12f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 40;

        List<CharacterCard> cards = new List<CharacterCard>();
        for (int i = 0; i < Keepers.Length; i++)
        {
            cards.Add(CreateCharacterCard(content, Keepers[i], screen.characters[i]));
        }
        screen.cards = cards.ToArray();

        LineupScroller scroller = root.AddComponent<LineupScroller>();
        scroller.cards = screen.cards;
        screen.scroller = scroller;
    }

    static CharacterCard CreateCharacterCard(Transform parent, KeeperInfo info, CharacterProfile profile)
    {
        GameObject go = new GameObject(info.name + "Card", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = KeeperCardSize.x;
        Image hit = go.AddComponent<Image>();
        hit.color = Color.clear;
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        CharacterCard card = go.AddComponent<CharacterCard>();
        card.button = button;

        // Fixed-size box standing on the card's bottom edge; CharacterSelectLayout scales it to the screen
        GameObject fitGo = new GameObject("Fit", typeof(RectTransform));
        fitGo.transform.SetParent(go.transform, false);
        RectTransform fit = (RectTransform)fitGo.transform;
        fit.anchorMin = fit.anchorMax = new Vector2(0.5f, 0f);
        fit.pivot = new Vector2(0.5f, 0f);
        fit.anchoredPosition = Vector2.zero;
        fit.sizeDelta = KeeperCardSize;
        card.fit = fit;

        // Everything visible lives under Visual, which scales around the platform
        GameObject visualGo = new GameObject("Visual", typeof(RectTransform));
        visualGo.transform.SetParent(fit, false);
        RectTransform visual = (RectTransform)visualGo.transform;
        Stretch(visual);
        visual.pivot = new Vector2(0.5f, 0.2f);
        card.visual = visual;
        card.group = visualGo.AddComponent<CanvasGroup>();

        card.glow = Bottom(CreateImage("Glow", visual, StyleSprite("CS_Glow")), new Vector2(0, 110), new Vector2(460, 460));
        card.platform = Bottom(CreateImage("Platform", visual, StyleSprite("CS_Platform")), new Vector2(0, 58), new Vector2(320, 138));

        // Rune circle carved into the pedestal's top: a round sigil in a squashed parent, so it
        // can spin around its own centre and still lie flat on the stone
        GameObject tiltGo = new GameObject("RuneTilt", typeof(RectTransform));
        tiltGo.transform.SetParent(visual, false);
        RectTransform tilt = (RectTransform)tiltGo.transform;
        tilt.anchorMin = tilt.anchorMax = new Vector2(0.5f, 0f);
        tilt.pivot = new Vector2(0.5f, 0.5f);
        tilt.anchoredPosition = new Vector2(0, 152);
        tilt.sizeDelta = new Vector2(276, 276);
        tilt.localScale = new Vector3(1f, 0.22f, 1f);
        card.ring = CreateImage("Rune", tilt, StyleSprite("CS_Rune"));
        Stretch(card.ring.rectTransform);
        Image shadow = Bottom(CreateImage("Shadow", visual, StyleSprite("CS_Glow")), new Vector2(0, 144), new Vector2(230, 46));
        shadow.preserveAspect = false;
        shadow.color = new Color(0, 0, 0, 0.55f);

        Sprite heroSprite = profile.heroSprite;
        card.hero = Bottom(CreateImage("Hero", visual, heroSprite), new Vector2(0, 158), SizeFromHeight(heroSprite, 360));
        card.hero.rectTransform.pivot = new Vector2(0.5f, 0f);
        HeroIdle idle = card.hero.gameObject.AddComponent<HeroIdle>();
        idle.neckX = info.neckX;
        idle.neckHeight = info.neckY;
        card.idle = idle;

        // Just the name under the platform, no plate; it turns gold while the keeper is in the squad
        card.nameText = CreateChunkyText("Name", visual, profile.displayName, 50, Color.white);
        Bottom(card.nameText, new Vector2(0, 0), new Vector2(340, 62));

        // Slot number badge on the keeper's shoulder
        Image badge = CreateImage("Badge", visual, StyleSprite("CC_CircleGold"));
        Bottom(badge, new Vector2(118, 432), new Vector2(92, 92));
        card.badge = badge.rectTransform;
        card.badgeNumber = CreateChunkyText("Number", badge.transform, "1", 54, Color.white);
        StretchLabel(card.badgeNumber, 92);
        badge.gameObject.SetActive(false);

        foreach (Graphic g in visual.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
        // Start state (not in the squad), so the scene looks right in the editor too
        Color rune = info.accent;
        rune.a = 0.5f;
        card.ring.color = rune;
        return card;
    }

    // Bottom: four round medallions, "SQUAD 0/4" + hint to their right, DEPLOY on the far right
    static void BuildRoster(Transform safe, CharacterSelectScreen screen)
    {
        float width = SlotWidth * CharacterSelectScreen.SquadSize + SlotSpacing * (CharacterSelectScreen.SquadSize - 1);
        GameObject rosterGo = new GameObject("SquadRoster", typeof(RectTransform));
        rosterGo.transform.SetParent(safe, false);
        RectTransform rt = (RectTransform)rosterGo.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(40, 12);
        rt.sizeDelta = new Vector2(width, FooterHeight - 20);
        screen.rosterRoot = rt;

        // Soft shadow under the medallions so they read against the busy wall (no box)
        Image shadow = CreateImage("Shadow", rt, StyleSprite("CS_Glow"));
        shadow.preserveAspect = false;
        shadow.color = new Color(Ink.r, Ink.g, Ink.b, 0.5f);
        shadow.raycastTarget = false;
        Place(shadow.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(width + 260, 380));

        // Status block in the open floor right of the medallions (clear of DEPLOY): "SQUAD 0/4" over the hint.
        // A soft dark glow behind it keeps the text readable on the stone wall (no box)
        Image statusShadow = CreateImage("StatusShadow", rt, StyleSprite("CS_Glow"));
        statusShadow.preserveAspect = false;
        statusShadow.color = new Color(Ink.r, Ink.g, Ink.b, 0.6f);
        statusShadow.raycastTarget = false;
        Place(statusShadow.rectTransform, new Vector2(1f, 0f), new Vector2(44 + 270, 110), new Vector2(820, 340));

        // Counter: punches whenever the squad changes
        GameObject counterGo = new GameObject("Counter", typeof(RectTransform));
        counterGo.transform.SetParent(rt, false);
        RectTransform counter = (RectTransform)counterGo.transform;
        Place(counter, new Vector2(1f, 0f), new Vector2(44 + 160, 166), new Vector2(320, 80));
        TextMeshProUGUI label = CreateChunkyText("Label", counter, "SQUAD", 50, Color.white);
        label.alignment = TextAlignmentOptions.Left;
        Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70, 0), new Vector2(180, 74));
        TextMeshProUGUI count = CreateChunkyText("Count", counter, "0/4", 66, TextGold);
        count.alignment = TextAlignmentOptions.Left;
        Place(count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(90, 3), new Vector2(140, 80));
        screen.counterRoot = counter;
        screen.counterText = count;

        // Hint under the counter, big enough to read at a glance; long lines wrap to two rows
        TextMeshProUGUI hint = CreateChunkyText("Hint", rt, "Swipe and tap keepers\nto add them", 42, Color.white);
        hint.alignment = TextAlignmentOptions.TopLeft;
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.lineSpacing = -12;
        hint.fontSizeMin = 30;
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(1, 0);
        hint.rectTransform.pivot = new Vector2(0, 1);
        hint.rectTransform.anchoredPosition = new Vector2(44, 122);
        hint.rectTransform.sizeDelta = new Vector2(520, 116);
        screen.hintText = hint;

        GameObject row = new GameObject("Slots", typeof(RectTransform));
        row.transform.SetParent(rt, false);
        RectTransform rowRt = (RectTransform)row.transform;
        rowRt.anchorMin = new Vector2(0, 0);
        rowRt.anchorMax = new Vector2(1, 0);
        rowRt.pivot = new Vector2(0.5f, 0);
        rowRt.offsetMin = new Vector2(0, 0);
        rowRt.offsetMax = new Vector2(0, 196);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = SlotSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        List<SquadSlot> slots = new List<SquadSlot>();
        for (int i = 0; i < CharacterSelectScreen.SquadSize; i++)
        {
            slots.Add(CreateSquadSlot(row.transform, i + 1));
        }
        screen.slots = slots.ToArray();

        // DEPLOY: grey and locked until the squad is full, then gold
        Button play = CreateChunkyButton("PlayButton", safe, "DEPLOY", "CC_Gray", new Vector2(1f, 0f), new Vector2(-236, 112), new Vector2(400, 172), 76);
        ColorBlock colors = play.colors;
        colors.disabledColor = Color.white; // the grey art already shows it's locked
        play.colors = colors;
        play.interactable = false;
        screen.playButton = play;
        screen.playImage = play.GetComponent<Image>();
        screen.playLabel = play.GetComponentInChildren<TextMeshProUGUI>();
        screen.playReadySprite = StyleSprite("CC_Gold");
        screen.playLockedSprite = StyleSprite("CC_Gray");
    }

    static SquadSlot CreateSquadSlot(Transform parent, int number)
    {
        GameObject go = new GameObject("Slot" + number, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image hit = go.AddComponent<Image>();
        hit.color = Color.clear;
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        SquadSlot slot = go.AddComponent<SquadSlot>();
        slot.button = button;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(go.transform, false);
        RectTransform content = (RectTransform)contentGo.transform;
        Stretch(content);
        content.pivot = new Vector2(0.5f, 0.6f); // pop around the medallion
        slot.content = content;
        Vector2 discPos = new Vector2(0, 50);

        // Empty: dark see-through disc, spinning dashed ring and a "+"
        GameObject emptyGo = new GameObject("Empty", typeof(RectTransform));
        emptyGo.transform.SetParent(content, false);
        Stretch((RectTransform)emptyGo.transform);
        slot.emptyGroup = emptyGo.AddComponent<CanvasGroup>();
        Image well = CreateImage("Well", emptyGo.transform, StyleSprite("CS_Disc"));
        well.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
        Place(well.rectTransform, new Vector2(0.5f, 0f), discPos, new Vector2(DiscSize, DiscSize));
        well.rectTransform.pivot = new Vector2(0.5f, 0f);
        slot.emptyRing = CreateImage("Ring", emptyGo.transform, StyleSprite("CS_DashRing"));
        Place(slot.emptyRing.rectTransform, new Vector2(0.5f, 0f), discPos + new Vector2(0, DiscSize / 2), new Vector2(DiscSize + 12, DiscSize + 12));
        TextMeshProUGUI plus = CreateChunkyText("Plus", emptyGo.transform, "+", 80, new Color(1, 1, 1, 0.75f));
        Place(plus.rectTransform, new Vector2(0.5f, 0f), discPos + new Vector2(0, DiscSize / 2 + 6), new Vector2(100, 100));

        // Filled: portrait inside a disc of the keeper's colour, gold rim, name underneath
        GameObject filledGo = new GameObject("Filled", typeof(RectTransform));
        filledGo.transform.SetParent(content, false);
        Stretch((RectTransform)filledGo.transform);
        slot.filledGroup = filledGo.AddComponent<CanvasGroup>();
        slot.filledGroup.alpha = 0;

        Image disc = CreateImage("Disc", filledGo.transform, StyleSprite("CS_Disc"));
        Place(disc.rectTransform, new Vector2(0.5f, 0f), discPos + new Vector2(0, DiscSize / 2), new Vector2(DiscSize, DiscSize));
        disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        slot.disc = disc;
        Image portrait = CreateImage("Portrait", disc.transform, null);
        RectTransform p = portrait.rectTransform;
        p.anchorMin = p.anchorMax = new Vector2(0.5f, 1f);
        p.pivot = new Vector2(0.5f, 1f);
        p.anchoredPosition = new Vector2(0, -4);
        p.sizeDelta = new Vector2(200, 250);
        slot.portrait = portrait;
        HeroIdle idle = portrait.gameObject.AddComponent<HeroIdle>();
        idle.breathRise = 0.008f;
        idle.hipShift = 0.006f;
        idle.headTurn = 1.6f;
        idle.timeOffset = number * 2.1f;
        slot.portraitIdle = idle;

        slot.rim = CreateImage("Rim", filledGo.transform, StyleSprite("CS_Ring"));
        Place(slot.rim.rectTransform, new Vector2(0.5f, 0f), discPos + new Vector2(0, DiscSize / 2), new Vector2(DiscSize + 14, DiscSize + 14));

        // Small red "x" on the rim: tapping anywhere on the medallion removes the keeper, this just shows it
        Image remove = CreateImage("Remove", filledGo.transform, StyleSprite("CS_Disc"));
        remove.color = new Color32(235, 70, 70, 255);
        Place(remove.rectTransform, new Vector2(0.5f, 0f), discPos + new Vector2(DiscSize * 0.36f, DiscSize * 0.86f), new Vector2(46, 46));
        TextMeshProUGUI x = CreateChunkyText("X", remove.transform, "x", 34, Color.white);
        Stretch(x.rectTransform);
        x.rectTransform.offsetMin = new Vector2(0, 4);

        slot.nameText = CreateChunkyText("Name", filledGo.transform, "", 36, Color.white);
        Place(slot.nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(SlotWidth, 44));

        foreach (Graphic g in content.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
        return slot;
    }

    static T Bottom<T>(T graphic, Vector2 pos, Vector2 size) where T : Graphic
    {
        RectTransform rt = graphic.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return graphic;
    }

    static void TopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    #endregion

    #region Character select art

    static void EnsureCharacterSelectAssets()
    {
        // Hero art: crisp, uncompressed sprites
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HeroFolder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled)
            {
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // Sprites from the earlier stat-panel layout that nothing uses any more
        foreach (string old in new[] { "CS_Gem", "CS_IconHeart", "CS_IconSword", "CS_IconTarget", "CS_IconCross", "CS_Pip", "CS_Well", "CS_WellFrame" })
        {
            AssetDatabase.DeleteAsset(StyleFolder + old + ".png");
        }

        if (!StyleSprite("CS_Platform"))
        {
            WritePlatform("CS_Platform");
        }
        if (!StyleSprite("CS_Rune"))
        {
            WriteRuneCircle("CS_Rune");
        }
        if (!StyleSprite("CS_Glow"))
        {
            RenderSprite("CS_Glow", 256, 256, (px, py) =>
            {
                float d = Mathf.Clamp01(new Vector2(px - 128, py - 128).magnitude / 128f);
                float a = 1 - Mathf.SmoothStep(0, 1, d);
                return new Color(1, 1, 1, a * a);
            }, Vector4.zero);
        }
        // Round squad medallion parts: tintable disc with a dark outline, gold-able rim, dashed "empty" ring
        if (!StyleSprite("CS_Disc"))
        {
            WriteDisc("CS_Disc", 256, 10);
        }
        if (!StyleSprite("CS_Dot"))
        {
            WriteDisc("CS_Dot", 64, 9);
        }
        if (!StyleSprite("CS_Ring"))
        {
            WriteRing("CS_Ring", 256, 104, 128, 6, 0);
        }
        if (!StyleSprite("CS_DashRing"))
        {
            WriteRing("CS_DashRing", 256, 114, 126, 0, 18);
        }
        if (!StyleSprite("CS_Slant"))
        {
            WriteSlant("CS_Slant");
        }
    }

    // Round stone pedestal: lit top with a gold rim, darker side, thick dark outline
    static void WritePlatform(string name)
    {
        const int w = 512, h = 220;
        const float cx = 256, rx = 248, ry = 62, topY = 150, bottomY = 70, outline = 8, rim = 14;
        Color topCentre = new Color32(178, 182, 226, 255), topEdge = new Color32(124, 128, 178, 255);
        Color gold = new Color32(255, 206, 84, 255), goldDark = new Color32(206, 132, 22, 255);
        Color sideTop = new Color32(92, 94, 140, 255), sideBottom = new Color32(54, 54, 92, 255);

        RenderSprite(name, w, h, (px, py) =>
        {
            float dx = px - cx;
            float topInner = Ellipse(dx, py - topY, rx - outline, ry - outline);
            float topOuter = Ellipse(dx, py - topY, rx, ry);
            float rimInner = Ellipse(dx, py - topY, rx - outline - rim, ry - outline - rim * 0.45f);
            if (rimInner <= 1)
            {
                float t = Mathf.Sqrt(rimInner);
                Color c = Color.Lerp(topCentre, topEdge, t * t);
                // soft highlight towards the back
                return Lighten(c, 0.12f * Mathf.Clamp01((py - topY) / ry + 0.3f));
            }
            if (topInner <= 1)
            {
                return Color.Lerp(goldDark, gold, Mathf.Clamp01((py - topY) / ry * 0.5f + 0.6f));
            }
            if (topOuter <= 1)
            {
                return Ink;
            }
            bool sideInner = (Mathf.Abs(dx) <= rx - outline && py <= topY && py >= bottomY) || Ellipse(dx, py - bottomY, rx - outline, ry - outline) <= 1;
            if (sideInner && py < topY + 1)
            {
                float t = Mathf.InverseLerp(bottomY - ry, topY, py);
                return Color.Lerp(sideBottom, sideTop, t);
            }
            bool sideOuter = (Mathf.Abs(dx) <= rx && py <= topY && py >= bottomY) || Ellipse(dx, py - bottomY, rx, ry) <= 1;
            if (sideOuter)
            {
                return Ink;
            }
            return Color.clear;
        }, Vector4.zero);
    }

    // Magic sigil seen from above: double outer ring with rune marks between, a four-point star inside.
    // White so it can be tinted; drawn round and squashed by its parent to lie on the pedestal.
    static void WriteRuneCircle(string name)
    {
        const int size = 512;
        const float c = size / 2f;
        RenderSprite(name, size, size, (px, py) =>
        {
            float dx = px - c, dy = py - c;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            bool outer = Mathf.Abs(r - 244) < 9;
            bool inner = Mathf.Abs(r - 196) < 5;
            bool core = Mathf.Abs(r - 92) < 5;

            // 12 rune marks in the band: alternating short bars and diamonds
            float sector = Mathf.Repeat(angle / (Mathf.PI * 2) * 12f, 1f) - 0.5f;
            int index = Mathf.FloorToInt(Mathf.Repeat(angle / (Mathf.PI * 2) * 12f, 12f));
            float along = sector * r * Mathf.PI * 2 / 12f; // arc distance from the mark's centre
            float across = r - 220;
            bool rune = index % 2 == 0
                ? Mathf.Abs(along) < 5 && Mathf.Abs(across) < 13
                : Mathf.Abs(along) + Mathf.Abs(across) < 12;

            // Four-point star joining the core ring to the inner ring
            float star = Mathf.Abs(Mathf.Cos(angle * 2));
            float starEdge = Mathf.Lerp(196, 70, Mathf.Pow(1 - star, 0.35f));
            bool starLine = r < 196 && Mathf.Abs(r - starEdge) < 5;

            if (outer || inner || core || rune || starLine)
            {
                return Color.white;
            }
            // Faint glow filling the circle
            return r < 244 ? new Color(1, 1, 1, 0.12f) : Color.clear;
        }, Vector4.zero);
    }

    // White disc with a dark outline and a soft shine at the top; tinted at runtime
    static void WriteDisc(string name, int size, float outline)
    {
        float c = size / 2f, radius = c - 1;
        RenderSprite(name, size, size, (px, py) =>
        {
            float r = new Vector2(px - c, py - c).magnitude;
            if (r > radius)
            {
                return Color.clear;
            }
            if (r > radius - outline)
            {
                return Ink;
            }
            float shine = Mathf.Clamp01((py - c) / radius);
            return Color.Lerp(new Color(0.86f, 0.86f, 0.9f), Color.white, shine);
        }, Vector4.zero);
    }

    // Ring between two radii. outline > 0 adds dark edges (rim); dashes > 0 cuts it into rounded dashes
    static void WriteRing(string name, int size, float inner, float outer, float outline, int dashes)
    {
        float c = size / 2f;
        outer -= 1;
        RenderSprite(name, size, size, (px, py) =>
        {
            float dx = px - c, dy = py - c;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < inner || r > outer)
            {
                return Color.clear;
            }
            if (dashes > 0)
            {
                float mid = (inner + outer) / 2, half = (outer - inner) / 2;
                float sector = Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2) * dashes, 1f) - 0.5f;
                float along = Mathf.Max(0, Mathf.Abs(sector * mid * Mathf.PI * 2 / dashes) - mid * Mathf.PI / dashes * 0.35f);
                return new Vector2(along, r - mid).magnitude <= half ? Color.white : Color.clear;
            }
            if (r < inner + outline || r > outer - outline)
            {
                return Ink;
            }
            float t = Mathf.InverseLerp(inner + outline, outer - outline, r);
            return Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.82f), Mathf.Abs(t - 0.4f) * 1.4f);
        }, Vector4.zero);
    }

    // Slanted tag (parallelogram) with a dark outline; 9-sliced so it stretches to any width
    static void WriteSlant(string name)
    {
        const int w = 160, h = 80;
        const float slant = 22, outline = 6;
        RenderSprite(name, w, h, (px, py) =>
        {
            float shift = slant * (py / h);
            float left = 1 + shift, right = w - 1 - slant + shift;
            if (px < left || px > right || py < 1 || py > h - 1)
            {
                return Color.clear;
            }
            float edge = Mathf.Min(px - left, right - px, py - 1, h - 1 - py);
            if (edge < outline)
            {
                return Ink;
            }
            return Color.Lerp(new Color(0.84f, 0.84f, 0.88f), Color.white, py / h);
        }, new Vector4(slant + 12, 0, slant + 12, 0));
    }

    static float Ellipse(float dx, float dy, float rx, float ry)
    {
        return (dx / rx) * (dx / rx) + (dy / ry) * (dy / ry);
    }

    #endregion
}
