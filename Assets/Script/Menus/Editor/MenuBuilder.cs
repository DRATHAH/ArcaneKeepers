using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the SelectMenu + LevelSelect scenes and adds the pause menu to DemoLevel.
// Safe to run again: it rebuilds the generated scenes and replaces the old pause menu.
public static class MenuBuilder
{
    const string ScenesFolder = "Assets/Scenes/";
    const string UIFolder = "Assets/UserInterface/";

    const string SelectMenuScene = "SelectMenu";
    const string LevelSelectScene = "LevelSelect";
    const string CharacterScene = "TowerSelect";
    const string MainMenuScene = "MainMenu";
    const string GameScene = "DemoLevel";

    // Level number -> scene. Levels whose scene isn't in the build show as locked.
    static readonly string[] LevelScenes = { GameScene, "Level2", "Level3", "Level4", "Level5" };

    static readonly Color DarkBrown = new Color32(59, 42, 20, 255);

    [MenuItem("Arcane Keepers/Build Select Menus + Pause Menu")]
    public static void BuildAll()
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

        EnsureStyleAssets();
        EnsureCritterAssets();
        BuildSelectMenu();
        BuildLevelSelect();
        AddScenesToBuild();
        RetargetButtons(MainMenuScene, CharacterScene, SelectMenuScene, null);
        RetargetButtons(CharacterScene, GameScene, SelectMenuScene, "Done");
        BuildCharacterSelect();
        StyleMainMenu();
        BuildPauseMenu();

        if (!string.IsNullOrEmpty(startScene))
        {
            EditorSceneManager.OpenScene(startScene);
        }
        Debug.Log("Menu Builder: SelectMenu, LevelSelect and the DemoLevel pause menu are ready.");
    }

    // Rebuilds only the select screens (SelectMenu, LevelSelect, TowerSelect); leaves the main menu and pause menu alone
    [MenuItem("Arcane Keepers/Rebuild Select Screens")]
    public static void BuildSelectScreens()
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

        EnsureStyleAssets();
        EnsureCritterAssets();
        BuildSelectMenu();
        BuildLevelSelect();
        AddScenesToBuild();
        BuildCharacterSelect();

        if (!string.IsNullOrEmpty(startScene))
        {
            EditorSceneManager.OpenScene(startScene);
        }
        Debug.Log("Menu Builder: select screens rebuilt.");
    }

    // Rebuilds only the DemoLevel pause menu
    [MenuItem("Arcane Keepers/Rebuild Pause Menu")]
    public static void BuildPauseMenuOnly()
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

        EnsureStyleAssets();
        EnsureCritterAssets();
        BuildPauseMenu();

        if (!string.IsNullOrEmpty(startScene))
        {
            EditorSceneManager.OpenScene(startScene);
        }
        Debug.Log("Menu Builder: pause menu rebuilt.");
    }

    #region Scenes

    // Menus are landscape: everything important sits inside a 1920x1080 area that always fits the screen
    // (phones get extra width, tablets extra height), and inside the device's safe area.

    static void BuildSelectMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndEventSystem();
        Transform canvas = CreateMenuCanvas("Canvas");
        SceneLoader loader = canvas.gameObject.AddComponent<SceneLoader>();

        CreateMenuBackground(canvas);
        Transform safe = CreateSafeArea(canvas);
        CreateBanner(safe, "Get Ready!");

        Button characters = CreateStepCard(safe, "Step1", 1, new Vector2(-390, -45), "Characters", "Pick Team", "CC_Blue",
            "Main-Background", "MainMenu-Heroes", "Start here!", "Team: {0}/{1}", GuidePulse.Condition.WhenNoTeam);
        UnityEventTools.AddStringPersistentListener(characters.onClick, loader.LoadScene, CharacterScene);

        Button levels = CreateStepCard(safe, "Step2", 2, new Vector2(390, -45), "Levels", "Choose Level", "CC_Gold",
            "LevelBackground", "MainMenu-Skeletons", "Pick a team first", "Ready!", GuidePulse.Condition.WhenTeamReady);
        UnityEventTools.AddStringPersistentListener(levels.onClick, loader.LoadScene, LevelSelectScene);

        CreateBackButton(safe, loader, MainMenuScene);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenesFolder + SelectMenuScene + ".unity");
    }

    static void BuildLevelSelect()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndEventSystem();
        Transform canvas = CreateMenuCanvas("Canvas");
        SceneLoader loader = canvas.gameObject.AddComponent<SceneLoader>();

        CreateMenuBackground(canvas);
        Transform safe = CreateSafeArea(canvas);
        CreateBanner(safe, "Select Level");

        // Hint that changes once the player has a team
        Image hint = CreatePanel("Hint", safe, Color.white);
        Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(820, 90));
        ApplyChunky(hint, "CC_Dark", 90);
        hint.raycastTarget = false;
        TextMeshProUGUI hintText = CreateChunkyText("HintText", hint.transform, "Pick a team first!", 42, WarnOrange);
        StretchLabel(hintText, 90);
        TeamStatusText hintStatus = hintText.gameObject.AddComponent<TeamStatusText>();
        hintStatus.emptyText = "Pick a team first!";
        hintStatus.readyText = "Tap Level 1 to play!";
        hintStatus.emptyColor = WarnOrange;
        hintStatus.readyColor = OkGreen;

        // One row of level tiles
        GameObject row = new GameObject("LevelRow", typeof(RectTransform));
        row.transform.SetParent(safe, false);
        Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1800, 430));
        GridLayoutGroup layout = row.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(310, 420);
        layout.spacing = new Vector2(36, 0);
        layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layout.constraintCount = 1;
        layout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < LevelScenes.Length; i++)
        {
            CreateLevelTile(row.transform, i + 1, LevelScenes[i]);
        }
        // Point the player at the first level
        row.transform.GetChild(0).gameObject.AddComponent<GuidePulse>().amount = 0.03f;

        CreateBackButton(safe, loader, SelectMenuScene);

        Button changeTeam = CreateChunkyButton("ChangeTeamButton", safe, "Change Team", "CC_Blue",
            new Vector2(0.5f, 0f), new Vector2(0, 105), new Vector2(400, 120), 56);
        UnityEventTools.AddStringPersistentListener(changeTeam.onClick, loader.LoadScene, CharacterScene);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenesFolder + LevelSelectScene + ".unity");
    }

    // A big tappable card: step badge, picture, title, live team status and a call-to-action pill
    static Button CreateStepCard(Transform parent, string name, int number, Vector2 pos, string title, string action, string actionSprite,
        string backdrop, string art, string emptyText, string readyText, GuidePulse.Condition pulse)
    {
        Image card = CreatePanel(name, parent, Color.white);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(700, 660));
        ApplyChunky(card, "CC_Stone", 660);
        Button button = card.gameObject.AddComponent<Button>();
        SetPressedTint(button);
        GuidePulse guide = card.gameObject.AddComponent<GuidePulse>();
        guide.condition = pulse;
        guide.amount = 0.02f;

        // Picture in a dark frame: backdrop scenery with the characters standing in front
        Image frame = CreatePanel("Picture", card.transform, Color.white);
        Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 135), new Vector2(620, 330));
        ApplyChunky(frame, "CC_Dark", 330);
        frame.raycastTarget = false;
        GameObject mask = new GameObject("Mask", typeof(RectTransform));
        mask.transform.SetParent(frame.transform, false);
        RectTransform maskRect = (RectTransform)mask.transform;
        Stretch(maskRect);
        maskRect.offsetMin = new Vector2(9, 32);
        maskRect.offsetMax = new Vector2(-9, -9);
        mask.AddComponent<RectMask2D>();

        Sprite backdropSprite = LoadSprite(backdrop);
        Image backdropImage = CreateImage("Backdrop", mask.transform, backdropSprite);
        backdropImage.raycastTarget = false;
        backdropImage.preserveAspect = false;
        Place(backdropImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 290));
        AspectRatioFitter fitter = backdropImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = backdropSprite ? backdropSprite.rect.width / backdropSprite.rect.height : 16f / 9f;

        Sprite artSprite = LoadSprite(art);
        Image artImage = CreateImage("Art", mask.transform, artSprite);
        artImage.raycastTarget = false;
        artImage.rectTransform.anchorMin = artImage.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        artImage.rectTransform.pivot = new Vector2(0.5f, 0f);
        artImage.rectTransform.anchoredPosition = new Vector2(0, -6);
        artImage.rectTransform.sizeDelta = SizeFromHeight(artSprite, 285);

        // Step number badge on the corner
        Image badge = CreateImage("StepBadge", card.transform, StyleSprite("CC_CircleGold"));
        badge.raycastTarget = false;
        Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(34, -30), new Vector2(124, 124));
        TextMeshProUGUI badgeNumber = CreateChunkyText("Number", badge.transform, number.ToString(), 72, Color.white);
        StretchLabel(badgeNumber, 124);

        TextMeshProUGUI titleText = CreateChunkyText("Title", card.transform, title, 76, Color.white);
        Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(640, 90));

        TextMeshProUGUI statusText = CreateChunkyText("Status", card.transform, emptyText, 42, WarnOrange);
        Place(statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(640, 56));
        TeamStatusText status = statusText.gameObject.AddComponent<TeamStatusText>();
        status.emptyText = emptyText;
        status.readyText = readyText;
        status.emptyColor = WarnOrange;
        status.readyColor = OkGreen;

        // Looks like a button; taps go through to the card
        Image pill = CreatePanel("Action", card.transform, Color.white);
        Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -246), new Vector2(420, 116));
        ApplyChunky(pill, actionSprite, 116);
        pill.raycastTarget = false;
        TextMeshProUGUI pillText = CreateChunkyText("Text", pill.transform, action, 58, Color.white);
        StretchLabel(pillText, 116);

        return button;
    }

    static void CreateLevelTile(Transform parent, int number, string sceneName)
    {
        // Levels that exist are blue with a gold medal; ones not made yet are grey and locked
        bool exists = EditorBuildSettings.scenes.Any(s => s.enabled && System.IO.Path.GetFileNameWithoutExtension(s.path) == sceneName);

        Image tile = CreatePanel("Level" + number, parent, Color.white);
        ApplyChunky(tile, exists ? "CC_Blue" : "CC_Gray", 420);
        Button button = tile.gameObject.AddComponent<Button>();
        SetPressedTint(button);
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white; // the grey art already shows it's locked
        button.colors = colors;

        Image medal = CreateImage("Medal", tile.transform, StyleSprite(exists ? "CC_CircleGold" : "CC_CircleGray"));
        medal.raycastTarget = false;
        Place(medal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 88), new Vector2(190, 190));
        TextMeshProUGUI medalNumber = CreateChunkyText("Number", medal.transform, number.ToString(), 110, Color.white);
        StretchLabel(medalNumber, 190);

        if (!exists)
        {
            Image padlock = CreateImage("Lock", tile.transform, StyleSprite("CC_Lock"));
            padlock.raycastTarget = false;
            Place(padlock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(78, 150), new Vector2(96, 96));
        }

        TextMeshProUGUI label = CreateChunkyText("Label", tile.transform, "Level " + number, 56, Color.white);
        Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -48), new Vector2(290, 70));

        Image pill = CreatePanel("StatusPill", tile.transform, Color.white);
        Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -132), new Vector2(250, 92));
        ApplyChunky(pill, exists ? "CC_Gold" : "CC_Dark", 92);
        pill.raycastTarget = false;
        TextMeshProUGUI status = CreateChunkyText("Status", pill.transform, exists ? "Play" : "Locked", 44, Color.white);
        StretchLabel(status, 92);

        LevelButton levelButton = tile.gameObject.AddComponent<LevelButton>();
        levelButton.sceneName = sceneName;
        levelButton.statusText = status;
    }

    // Restyles the existing character (tower select) screen: team row on top, all keepers below, Done in the corner
    static void BuildCharacterSelect()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + CharacterScene + ".unity");
        Transform canvas = scene.GetRootGameObjects().First(r => r.name == "Canvas").transform;
        Transform picker = canvas.GetComponentsInChildren<Transform>(true).First(t => t.name == "TowerSelectUI");
        Transform done = canvas.GetComponentsInChildren<Button>(true).First(b => b.name == "PlayButton").transform;

        // Undo a previous build: put the kept objects back, then remove everything generated
        picker.SetParent(canvas, false);
        done.SetParent(canvas, false);
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Guide_") || t.name.StartsWith("CC_")).ToList())
        {
            if (t)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }
        RemoveMissingListeners(done.GetComponent<Button>());

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // The old front view (single keeper + open-picker button) is replaced by the always-visible picker
        Transform frontView = canvas.Find("MainMenuUI");
        if (frontView)
        {
            frontView.gameObject.SetActive(false);
        }

        CreateMenuBackground(canvas, "CC_");
        Transform safe = CreateSafeArea(canvas, "CC_Safe");
        CreateBanner(safe, "Choose Your Team", "CC_Banner");

        SerializedObject ui = new SerializedObject(canvas.GetComponent<InventoryUI>());
        ui.FindProperty("towerSelectionCanvas").objectReferenceValue = null;
        ui.ApplyModifiedPropertiesWithoutUndo();

        picker.gameObject.SetActive(true);
        picker.SetParent(safe, false);
        Stretch((RectTransform)picker);
        Transform close = picker.Find("CloseButton");
        if (close)
        {
            close.gameObject.SetActive(false);
        }

        // Your team: dark tray with 4 slots
        RectTransform team = (RectTransform)picker.Find("EquippedTowers");
        Place(team, new Vector2(0.5f, 0.5f), new Vector2(0, 198), new Vector2(1480, 250));
        ApplyChunky(team.GetComponent<Image>(), "CC_Dark", 250);
        GridLayoutGroup teamGrid = team.GetComponent<GridLayoutGroup>();
        teamGrid.cellSize = new Vector2(180, 180);
        teamGrid.spacing = new Vector2(30, 0);
        teamGrid.padding = new RectOffset(400, 40, 14, 34);
        teamGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        teamGrid.constraintCount = 1;
        teamGrid.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI teamLabel = CreateChunkyText("CC_TeamLabel", team, "YOUR TEAM", 60, TextGold);
        Place(teamLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(220, 34), new Vector2(340, 74));
        teamLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        TextMeshProUGUI teamHint = CreateChunkyText("CC_TeamHint", team, "tap to remove", 36, Color.white);
        Place(teamHint.rectTransform, new Vector2(0f, 0.5f), new Vector2(220, -28), new Vector2(340, 50));
        teamHint.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        foreach (InventorySlot slot in team.GetComponentsInChildren<InventorySlot>(true))
        {
            StyleSlot(slot, true);
        }

        // All keepers: stone panel with a card per keeper
        RectTransform keepers = (RectTransform)picker.Find("UnlockedTowers");
        Place(keepers, new Vector2(0.5f, 0.5f), new Vector2(0, -142), new Vector2(1480, 400));
        ApplyChunky(keepers.GetComponent<Image>(), "CC_Stone", 400);
        GridLayoutGroup keeperGrid = keepers.GetComponent<GridLayoutGroup>();
        keeperGrid.cellSize = new Vector2(220, 286);
        keeperGrid.spacing = new Vector2(34, 0);
        keeperGrid.padding = new RectOffset(30, 30, 70, 36);
        keeperGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        keeperGrid.constraintCount = 1;
        keeperGrid.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI keepersLabel = CreateChunkyText("CC_KeepersLabel", keepers, "Tap a keeper to add  (max 4)", 46, Color.white);
        Place(keepersLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(1300, 60));
        keepersLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        foreach (InventorySlot slot in keepers.GetComponentsInChildren<InventorySlot>(true))
        {
            StyleSlot(slot, false);
        }

        // Done: big gold button in the bottom-right corner, with the team status next to it
        done.SetParent(safe, false);
        Place((RectTransform)done, new Vector2(1f, 0f), new Vector2(-205, 100), new Vector2(340, 130));
        ApplyChunky(done.GetComponent<Image>(), "CC_Gold", 130);
        Outline oldBorder = done.GetComponent<Outline>();
        if (oldBorder)
        {
            Object.DestroyImmediate(oldBorder);
        }
        SetPressedTint(done.GetComponent<Button>());
        TextMeshProUGUI doneLabel = done.GetComponentInChildren<TextMeshProUGUI>(true);
        doneLabel.text = "Done";
        doneLabel.color = Color.white;
        doneLabel.fontSize = 68;
        doneLabel.fontSharedMaterial = chunkyMaterial;
        doneLabel.font = styleFont;
        doneLabel.fontStyle = FontStyles.Normal;
        doneLabel.alignment = TextAlignmentOptions.Center;
        StretchLabel(doneLabel, 130);
        if (!done.GetComponent<GuidePulse>())
        {
            done.gameObject.AddComponent<GuidePulse>().condition = GuidePulse.Condition.WhenTeamReady;
        }

        TextMeshProUGUI status = CreateChunkyText("CC_Status", safe, "Pick at least 1 keeper", 46, WarnOrange);
        Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(-120, 100), new Vector2(1000, 64));
        TeamStatusText teamStatus = status.gameObject.AddComponent<TeamStatusText>();
        teamStatus.emptyText = "Pick at least 1 keeper";
        teamStatus.readyText = "{0}/{1} keepers ready - tap Done!";
        teamStatus.emptyColor = WarnOrange;
        teamStatus.readyColor = OkGreen;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Team slots are dark wells with a "+" while empty; keeper slots are blue cards with name and upgrade bar
    static void StyleSlot(InventorySlot slot, bool teamSlot)
    {
        ApplyChunky(slot.GetComponent<Image>(), teamSlot ? "CC_Slot" : "CC_Blue", teamSlot ? 180 : 286);
        SetPressedTint(slot.GetComponent<Button>());

        RectTransform icon = slot.icon.rectTransform;
        Stretch(icon);
        icon.offsetMin = teamSlot ? new Vector2(18, 44) : new Vector2(20, 104);
        icon.offsetMax = teamSlot ? new Vector2(-18, -14) : new Vector2(-20, -16);
        slot.icon.preserveAspect = true;

        RectTransform bar = (RectTransform)slot.amountSlider.transform;
        bar.anchorMin = new Vector2(0, 0);
        bar.anchorMax = new Vector2(1, 0);
        bar.pivot = new Vector2(0.5f, 0);
        bar.anchoredPosition = new Vector2(0, teamSlot ? 22 : 30);
        bar.sizeDelta = teamSlot ? new Vector2(-36, 28) : new Vector2(-40, 36);
        foreach (Image part in bar.GetComponentsInChildren<Image>(true))
        {
            bool fill = part.name == "Fill";
            part.sprite = StyleSprite(fill ? "CC_BarFill" : "CC_BarBg");
            part.type = Image.Type.Sliced;
            part.color = Color.white;
            part.pixelsPerUnitMultiplier = 2f;
            Stretch(part.rectTransform);
        }
        Transform fillArea = bar.Find("Fill Area");
        if (fillArea)
        {
            Stretch((RectTransform)fillArea);
            ((RectTransform)fillArea).offsetMin = new Vector2(4, 4);
            ((RectTransform)fillArea).offsetMax = new Vector2(-4, -4);
        }
        TMP_Text number = slot.number;
        number.font = styleFont;
        number.fontSharedMaterial = chunkyMaterial;
        number.color = Color.white;
        number.fontSize = teamSlot ? 22 : 26;
        number.enableAutoSizing = false;
        number.alignment = TextAlignmentOptions.Center;
        Stretch(number.rectTransform);

        SerializedObject so = new SerializedObject(slot);
        if (teamSlot)
        {
            TextMeshProUGUI plus = CreateChunkyText("CC_Empty", slot.transform, "+", 110, new Color(1, 1, 1, 0.4f));
            StretchLabel(plus, 180);
            plus.transform.SetSiblingIndex(0);
            so.FindProperty("emptyMarker").objectReferenceValue = plus.gameObject;
        }
        else
        {
            TextMeshProUGUI keeperName = CreateChunkyText("CC_Name", slot.transform, "", 36, Color.white);
            Place(keeperName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 86), new Vector2(200, 46));
            so.FindProperty("nameText").objectReferenceValue = keeperName;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // The slots are prefab instances: direct edits are only saved once recorded as overrides
        foreach (Component part in slot.GetComponentsInChildren<Component>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(part))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }
        }
    }

    // Drops persistent onClick calls whose target was deleted (e.g. guide objects from a previous build)
    static void RemoveMissingListeners(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentTarget(i) == null)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }
    }

    static void BuildPauseMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + GameScene + ".unity");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "PauseCanvas")
            {
                Object.DestroyImmediate(root);
            }
        }
        if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<EventSystem>(true) != null))
        {
            CreateEventSystem();
        }

        Transform canvas = CreateCanvas("PauseCanvas", new Vector2(1920, 1080), 1, 50);
        PauseMenu pause = canvas.gameObject.AddComponent<PauseMenu>();

        // Pause button: round chunky button in the top-right corner, inside the safe area
        Transform hudSafe = CreateSafeArea(canvas, "PauseButtonSafe");
        Button pauseButton = CreateRoundButton("PauseButton", hudSafe, "CC_CircleBlue", new Vector2(1, 1), new Vector2(-95, -95), 130);
        TextMeshProUGUI pauseLabel = CreateChunkyText("Text", pauseButton.transform, "II", 64, Color.white);
        StretchLabel(pauseLabel, 130);
        UnityEventTools.AddPersistentListener(pauseButton.onClick, pause.Pause);

        // Dimmed overlay that also blocks touches to the game behind it
        Image panel = CreatePanel("PausePanel", canvas, new Color(Ink.r, Ink.g, Ink.b, 0.7f));
        panel.sprite = null;
        Stretch(panel.rectTransform);
        Transform safe = CreateSafeArea(panel.transform);

        // Stone card with the blue ribbon across its top, like the select screens
        Image card = CreatePanel("Card", safe, Color.white);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(720, 700));
        ApplyChunky(card, "CC_Stone", 700);
        card.raycastTarget = false;

        Image banner = CreatePanel("Banner", safe, Color.white);
        banner.sprite = StyleSprite("CC_Banner");
        banner.pixelsPerUnitMultiplier = 200f / 190f;
        banner.raycastTarget = false;
        Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 320), new Vector2(880, 190));
        TextMeshProUGUI title = CreateChunkyText("Title", banner.transform, "Paused", 92, Color.white);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(600, 110));

        // Gold = main action (like Play / Done), blue = secondary, red = leave
        Vector2 center = new Vector2(0.5f, 0.5f);
        Button continueButton = CreateChunkyButton("ContinueButton", safe, "Continue", "CC_Gold", center, new Vector2(0, 140), new Vector2(500, 136), 68);
        UnityEventTools.AddPersistentListener(continueButton.onClick, pause.Continue);

        Button restartButton = CreateChunkyButton("RestartButton", safe, "Restart", "CC_Blue", center, new Vector2(0, -2), new Vector2(460, 120), 58);
        UnityEventTools.AddPersistentListener(restartButton.onClick, pause.Restart);

        Button giveUpButton = CreateChunkyButton("GiveUpButton", safe, "Give Up", "CC_Red", center, new Vector2(0, -136), new Vector2(460, 120), 58);
        UnityEventTools.AddPersistentListener(giveUpButton.onClick, pause.GiveUp);

        CreateAudioButton("MusicButton", safe, "CC_IconMusic", new Vector2(-90, -295), AudioChannel.Channel.Music);
        CreateAudioButton("SoundButton", safe, "CC_IconSound", new Vector2(90, -295), AudioChannel.Channel.Sound);

        SerializedObject so = new SerializedObject(pause);
        so.FindProperty("pausePanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton.gameObject;
        so.FindProperty("giveUpScene").stringValue = LevelSelectScene;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Round blue button with a white icon; a red slash covers it while muted
    static void CreateAudioButton(string name, Transform parent, string icon, Vector2 pos, AudioChannel.Channel channel)
    {
        Button button = CreateRoundButton(name, parent, "CC_CircleBlue", new Vector2(0.5f, 0.5f), pos, 140);

        Image iconImage = CreateImage("Icon", button.transform, StyleSprite(icon));
        iconImage.raycastTarget = false;
        Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(84, 84));

        Image slash = CreateImage("OffSlash", button.transform, StyleSprite("CC_Slash"));
        slash.raycastTarget = false;
        slash.preserveAspect = false;
        slash.type = Image.Type.Sliced;
        Place(slash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(150, 32));
        slash.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        slash.gameObject.SetActive(false);

        AudioToggleButton toggle = button.gameObject.AddComponent<AudioToggleButton>();
        toggle.channel = channel;
        toggle.offMarker = slash.gameObject;
    }

    #endregion

    #region Existing scene edits

    static void AddScenesToBuild()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int insertAt = scenes.FindIndex(s => s.path.EndsWith("/" + MainMenuScene + ".unity")) + 1;

        foreach (string name in new[] { SelectMenuScene, LevelSelectScene })
        {
            string path = ScenesFolder + name + ".unity";
            if (scenes.Any(s => s.path == path))
            {
                continue;
            }
            scenes.Insert(insertAt++, new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // Gives the main menu's "Tap to play" the same lettering as the pause menu
    static void StyleMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + MainMenuScene + ".unity");
        Button start = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Button>(true)).FirstOrDefault(b => b.name == "StartButton");
        if (start == null)
        {
            Debug.LogWarning("Menu Builder: no StartButton in " + MainMenuScene);
            return;
        }
        TextMeshProUGUI label = start.GetComponentInChildren<TextMeshProUGUI>(true);
        label.color = Color.white;
        label.fontSize = 34;
        ApplyFont(label, true);
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Points buttons that load `from` at `to` instead (optionally relabelling them)
    static void RetargetButtons(string sceneName, string from, string to, string newLabel)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + sceneName + ".unity");
        bool changed = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                SerializedObject so = new SerializedObject(button);
                SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                for (int i = 0; i < calls.arraySize; i++)
                {
                    SerializedProperty call = calls.GetArrayElementAtIndex(i);
                    SerializedProperty arg = call.FindPropertyRelative("m_Arguments.m_StringArgument");
                    if (call.FindPropertyRelative("m_MethodName").stringValue == "LoadScene" && arg.stringValue == from)
                    {
                        arg.stringValue = to;
                        changed = true;

                        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (newLabel != null && label)
                        {
                            Undo.RecordObject(label, "Relabel");
                            label.text = newLabel;
                            EditorUtility.SetDirty(label);
                        }
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            Debug.Log("Menu Builder: no button loading '" + from + "' in " + sceneName + " (already updated?)");
        }
    }

    #endregion

    #region UI helpers

    static void CreateCameraAndEventSystem()
    {
        GameObject cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        Camera camera = cam.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(40, 120, 200, 255);
        cam.transform.position = new Vector3(0, 0, -10);
        CreateEventSystem();
    }

    static void CreateEventSystem()
    {
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static Transform CreateCanvas(string name, Vector2 referenceResolution, float match, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = match;
        // Menus: always fit the whole layout, whether the screen is portrait or landscape
        if (referenceResolution.y > referenceResolution.x)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    // Blue = main action (white outlined text, like Continue); Gold = secondary (dark text, like Give Up)
    static Button CreateTextButton(string name, Transform parent, string text, Vector2 size, float fontSize, ButtonStyle style = ButtonStyle.Gold)
    {
        Image image = CreatePanel(name, parent, Color.white);
        image.rectTransform.sizeDelta = size;
        ApplyStyleSprite(image, "Button" + style, size.y);
        Button button = image.gameObject.AddComponent<Button>();

        bool outlined = style != ButtonStyle.Gold;
        TextMeshProUGUI label = CreateText("Text", image.transform, text, fontSize, outlined ? Color.white : DarkBrown, outlined);
        Stretch(label.rectTransform);
        // Keep the text off the thicker bottom edge of the button art
        label.rectTransform.offsetMin = new Vector2(24, size.y * 0.06f);
        label.rectTransform.offsetMax = new Vector2(-24, 0);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 20;
        label.fontSizeMax = fontSize;
        return button;
    }

    // cropBottom: fraction of the sprite's height to hide at the bottom (for art with stray pixels)
    static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 pos, float height, float cropBottom = 0)
    {
        Vector2 size = SizeFromHeight(sprite, height);
        Image image;
        Button button;
        if (cropBottom > 0)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            float visible = size.y * (1 - cropBottom);
            Place(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, new Vector2(size.x, visible));
            root.AddComponent<RectMask2D>();

            image = CreateImage("Art", root.transform, sprite);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            image.rectTransform.pivot = new Vector2(0.5f, 1f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = size;

            button = root.AddComponent<Button>();
            button.targetGraphic = image;
        }
        else
        {
            image = CreateImage(name, parent, sprite);
            Place(image.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            button = image.gameObject.AddComponent<Button>();
        }
        ColorBlock colors = button.colors;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;
        return button;
    }

    static Image CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, bool outlined = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        ApplyFont(tmp, outlined);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Vector2 SizeFromHeight(Sprite sprite, float height)
    {
        float aspect = sprite ? sprite.rect.width / sprite.rect.height : 4f;
        return new Vector2(height * aspect, height);
    }

    static Vector2 SizeFromWidth(Sprite sprite, float width)
    {
        float aspect = sprite ? sprite.rect.width / sprite.rect.height : 4f;
        return new Vector2(width, width / aspect);
    }

    static Sprite RoundedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    #endregion

    #region Style (matches the pause menu art)

    const string StyleFolder = "Assets/UserInterface/Generated/";
    const string FontFile = "Assets/UserInterface/Fonts/LilitaOne-Regular.ttf";

    enum ButtonStyle { Blue, Gold, Gray }

    static readonly Color Navy = new Color32(27, 37, 89, 255);
    static readonly Color Purple = new Color32(91, 67, 201, 255);
    static readonly Color LightGold = new Color32(255, 224, 138, 255);

    static TMP_FontAsset styleFont;
    static Material outlineMaterial;

    // Generates the button/panel sprites, the Lilita One font asset and its outlined material
    static void EnsureStyleAssets()
    {
        if (!AssetDatabase.IsValidFolder(StyleFolder.TrimEnd('/')))
        {
            AssetDatabase.CreateFolder("Assets/UserInterface", "Generated");
        }

        // Same palette as the pause menu: blue / gold buttons, purple banner, round gold + purple icon button
        WriteRoundedRect("ButtonBlue", 256, 128, 34, 6, 8, new Color32(71, 151, 242, 255), new Color32(32, 102, 214, 255), new Color32(20, 48, 110, 255), 0.18f);
        WriteRoundedRect("ButtonGold", 256, 128, 34, 6, 8, new Color32(255, 213, 79, 255), new Color32(245, 163, 26, 255), new Color32(138, 74, 14, 255), 0.18f);
        WriteRoundedRect("ButtonGray", 256, 128, 34, 6, 8, new Color32(185, 185, 198, 255), new Color32(138, 138, 154, 255), new Color32(74, 74, 92, 255), 0.12f);
        WriteRoundedRect("PanelPurple", 256, 128, 34, 6, 8, new Color32(114, 88, 226, 255), new Color32(78, 54, 190, 255), new Color32(42, 28, 110, 255), 0.1f);
        WriteRoundedRect("PanelCream", 256, 128, 34, 6, 0, new Color32(255, 247, 228, 255), new Color32(243, 224, 184, 255), Purple, 0);
        WriteCircleButton("ButtonRound");

        styleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StyleFolder + "LilitaOne SDF.asset");
        if (!styleFont)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontFile);
            styleFont = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            styleFont.name = "LilitaOne SDF";
            AssetDatabase.CreateAsset(styleFont, StyleFolder + "LilitaOne SDF.asset");
            styleFont.atlasTextures[0].name = "LilitaOne SDF Atlas";
            AssetDatabase.AddObjectToAsset(styleFont.atlasTextures[0], styleFont);
            styleFont.material.name = "LilitaOne SDF Material";
            AssetDatabase.AddObjectToAsset(styleFont.material, styleFont);
            styleFont.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            EditorUtility.SetDirty(styleFont);
        }

        // White text with a navy outline and soft drop shadow, like the lettering on the pause buttons
        outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Outline.mat");
        if (!outlineMaterial)
        {
            outlineMaterial = new Material(styleFont.material) { name = "LilitaOne SDF Outline" };
            AssetDatabase.CreateAsset(outlineMaterial, StyleFolder + "LilitaOne SDF Outline.mat");
        }
        outlineMaterial.CopyPropertiesFromMaterial(styleFont.material);
        outlineMaterial.EnableKeyword("OUTLINE_ON");
        outlineMaterial.SetFloat("_OutlineWidth", 0.24f);
        outlineMaterial.SetColor("_OutlineColor", Navy);
        outlineMaterial.SetFloat("_FaceDilate", 0.2f);
        outlineMaterial.EnableKeyword("UNDERLAY_ON");
        outlineMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.45f));
        outlineMaterial.SetFloat("_UnderlayOffsetY", -0.7f);
        outlineMaterial.SetFloat("_UnderlayDilate", 0.2f);
        outlineMaterial.SetFloat("_UnderlaySoftness", 0.15f);
        EditorUtility.SetDirty(outlineMaterial);

        AssetDatabase.SaveAssets();
    }

    static Sprite StyleSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(StyleFolder + name + ".png");
    }

    // 9-sliced style sprite; the border scales with the element's height so small and big buttons look alike
    static void ApplyStyleSprite(Image image, string spriteName, float height)
    {
        image.sprite = StyleSprite(spriteName);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = Mathf.Clamp(130f / height, 0.6f, 3f);
    }

    static void ApplyFont(TextMeshProUGUI tmp, bool outlined)
    {
        tmp.font = styleFont;
        tmp.fontSharedMaterial = outlined ? outlineMaterial : styleFont.material;
        tmp.fontStyle = FontStyles.Normal;
    }

    static float RoundedRectDistance(float px, float py, float cx, float cy, float hx, float hy, float r)
    {
        float qx = Mathf.Abs(px - cx) - hx + r;
        float qy = Mathf.Abs(py - cy) - hy + r;
        float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    static Color Over(Color under, Color over, float alpha)
    {
        return Color.Lerp(under, over, Mathf.Clamp01(alpha));
    }

    // Rounded button: dark border, gradient fill with a soft top shine and a thicker darker bottom edge
    static void WriteRoundedRect(string name, int w, int h, float radius, float border, float depth,
        Color top, Color bottom, Color borderColor, float gloss)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float cx = w / 2f, cy = h / 2f, hx = w / 2f - 2, hy = h / 2f - 2;
        Color shade = Color.Lerp(bottom, Color.black, 0.3f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float outer = RoundedRectDistance(px, py, cx, cy, hx, hy, radius);
                float body = RoundedRectDistance(px, py, cx, cy, hx - border, hy - border, radius - border);
                float face = RoundedRectDistance(px, py, cx, cy + depth / 2, hx - border, hy - border - depth / 2, radius - border);

                float t = Mathf.InverseLerp(cy + depth / 2 - (hy - border - depth / 2), cy + depth / 2 + (hy - border - depth / 2), py);
                Color fill = Color.Lerp(bottom, top, t);
                fill = Color.Lerp(fill, Color.white, gloss * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 0.7f, t)));

                Color c = borderColor;
                c = Over(c, shade, 0.5f - body);
                c = Over(c, fill, 0.5f - face);
                c.a = Mathf.Clamp01(0.5f - outer);
                tex.SetPixel(x, y, c);
            }
        }
        SaveStyleTexture(tex, name, Mathf.CeilToInt(radius + border + depth + 2));
    }

    // Round gold button with a purple ring, like the music / sound buttons
    static void WriteCircleButton(string name)
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color outline = new Color32(42, 28, 110, 255);
        Color ringTop = new Color32(122, 91, 230, 255), ringBottom = new Color32(75, 51, 184, 255);
        Color goldTop = new Color32(255, 213, 79, 255), goldBottom = new Color32(245, 163, 26, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude;
                float t = (y + 0.5f) / size;
                Color c = outline;
                c = Over(c, Color.Lerp(ringBottom, ringTop, t), 118 - d + 0.5f);
                c = Over(c, outline, 92 - d + 0.5f);
                Color gold = Color.Lerp(goldBottom, goldTop, t);
                gold = Color.Lerp(gold, Color.white, 0.18f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 0.75f, t)));
                c = Over(c, gold, 87 - d + 0.5f);
                c.a = Mathf.Clamp01(124 - d + 0.5f);
                tex.SetPixel(x, y, c);
            }
        }
        SaveStyleTexture(tex, name, 0);
    }

    static void SaveStyleTexture(Texture2D tex, string name, int slice)
    {
        string path = StyleFolder + name + ".png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = new Vector4(slice, slice, slice, slice);
        importer.SaveAndReimport();
    }

    // Textures here are sliced into several sprites; the biggest slice is the actual button art
    static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(UIFolder + fileName + ".png")
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height)
            .FirstOrDefault();
        if (!sprite)
        {
            Debug.LogWarning("Menu Builder: no sprite found in " + UIFolder + fileName + ".png");
        }
        return sprite;
    }

    #endregion

    #region Critter style (chunky cartoon UI, like the main menu art)

    // Thick dark outlines, glossy bevelled buttons with a darker bottom lip and a hard drop shadow,
    // blue ribbon banner, stone panels: the same look as the main menu's logo and characters.

    static readonly Color Ink = new Color32(29, 21, 51, 255);
    static readonly Color TextGold = new Color32(255, 222, 99, 255);
    static readonly Color WarnOrange = new Color32(255, 186, 74, 255);
    static readonly Color OkGreen = new Color32(150, 240, 110, 255);

    const float ChunkyLip = 12, ChunkyShadow = 8;

    static Material chunkyMaterial;

    static void EnsureCritterAssets()
    {
        Color blueTop = new Color32(96, 178, 255, 255), blueBottom = new Color32(36, 108, 232, 255), blueLip = new Color32(22, 62, 158, 255);
        Color goldTop = new Color32(255, 220, 92, 255), goldBottom = new Color32(244, 152, 22, 255), goldLip = new Color32(176, 90, 10, 255);
        Color grayTop = new Color32(172, 172, 186, 255), grayBottom = new Color32(124, 124, 140, 255), grayLip = new Color32(82, 82, 98, 255);

        WriteChunkyRect("CC_Gold", goldTop, goldBottom, goldLip, Ink, 0.28f);
        WriteChunkyRect("CC_Blue", blueTop, blueBottom, blueLip, Ink, 0.24f);
        WriteChunkyRect("CC_Gray", grayTop, grayBottom, grayLip, Ink, 0.15f);
        WriteChunkyRect("CC_Stone", new Color32(158, 162, 206, 255), new Color32(110, 114, 160, 255), new Color32(68, 70, 108, 255), Ink, 0.08f);
        WriteChunkyRect("CC_Dark", new Color(0.13f, 0.11f, 0.29f, 0.84f), new Color(0.09f, 0.08f, 0.22f, 0.84f), new Color(0.05f, 0.04f, 0.14f, 0.9f), Ink, 0);
        WriteChunkyRect("CC_Slot", new Color(0.07f, 0.06f, 0.18f, 0.7f), new Color(0.1f, 0.09f, 0.24f, 0.7f), new Color(0.04f, 0.03f, 0.12f, 0.8f), new Color32(132, 118, 214, 255), 0);
        WriteChunkyCircle("CC_CircleGold", goldTop, goldBottom, goldLip);
        WriteChunkyCircle("CC_CircleGray", grayTop, grayBottom, grayLip);
        WriteChunkyCircle("CC_CircleBlue", blueTop, blueBottom, blueLip);
        Color redTop = new Color32(255, 108, 92, 255), redBottom = new Color32(222, 46, 46, 255), redLip = new Color32(140, 22, 30, 255);
        WriteChunkyRect("CC_Red", redTop, redBottom, redLip, Ink, 0.24f);
        WriteBar("CC_Slash", redTop, redBottom, 4);
        WriteMusicIcon("CC_IconMusic");
        WriteSoundIcon("CC_IconSound");
        WriteBanner("CC_Banner", blueTop, blueBottom, blueLip);
        WriteLock("CC_Lock");
        WriteBar("CC_BarBg", new Color32(24, 20, 52, 255), new Color32(24, 20, 52, 255), 3);
        WriteBar("CC_BarFill", goldTop, goldBottom, 0);

        // White lettering with a thick dark outline and a hard dark drop, like the cartoon art
        chunkyMaterial = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Chunky.mat");
        if (!chunkyMaterial)
        {
            chunkyMaterial = new Material(styleFont.material) { name = "LilitaOne SDF Chunky" };
            AssetDatabase.CreateAsset(chunkyMaterial, StyleFolder + "LilitaOne SDF Chunky.mat");
        }
        chunkyMaterial.CopyPropertiesFromMaterial(styleFont.material);
        chunkyMaterial.EnableKeyword("OUTLINE_ON");
        chunkyMaterial.SetFloat("_OutlineWidth", 0.28f);
        chunkyMaterial.SetColor("_OutlineColor", Ink);
        chunkyMaterial.SetFloat("_FaceDilate", 0.28f);
        chunkyMaterial.EnableKeyword("UNDERLAY_ON");
        chunkyMaterial.SetColor("_UnderlayColor", Ink);
        chunkyMaterial.SetFloat("_UnderlayOffsetX", 0);
        chunkyMaterial.SetFloat("_UnderlayOffsetY", -0.85f);
        chunkyMaterial.SetFloat("_UnderlayDilate", 0.28f);
        chunkyMaterial.SetFloat("_UnderlaySoftness", 0);
        EditorUtility.SetDirty(chunkyMaterial);

        AssetDatabase.SaveAssets();
    }

    static Transform CreateMenuCanvas(string name)
    {
        Transform canvas = CreateCanvas(name, new Vector2(1920, 1080), 0.5f, 0);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return canvas;
    }

    // Same castle-wall scenery as the main menu, slightly dimmed so the UI stands out
    static void CreateMenuBackground(Transform canvas, string prefix = "")
    {
        Sprite sprite = LoadSprite("Main-Background");
        Image bg = CreateImage(prefix + "Background", canvas, sprite);
        bg.raycastTarget = false;
        bg.preserveAspect = false;
        Place(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
        AspectRatioFitter fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite ? sprite.rect.width / sprite.rect.height : 16f / 9f;
        bg.transform.SetSiblingIndex(0);

        Image dim = CreatePanel(prefix + "Dim", canvas, new Color(Ink.r, Ink.g, Ink.b, 0.22f));
        dim.sprite = null;
        dim.raycastTarget = false;
        Stretch(dim.rectTransform);
        dim.transform.SetSiblingIndex(1);
    }

    static Transform CreateSafeArea(Transform canvas, string name = "Safe")
    {
        GameObject safe = new GameObject(name, typeof(RectTransform));
        safe.transform.SetParent(canvas, false);
        Stretch((RectTransform)safe.transform);
        safe.AddComponent<SafeArea>();
        return safe.transform;
    }

    static void CreateBanner(Transform parent, string title, string name = "Banner")
    {
        Image banner = CreatePanel(name, parent, Color.white);
        banner.sprite = StyleSprite("CC_Banner");
        banner.type = Image.Type.Sliced;
        banner.pixelsPerUnitMultiplier = 200f / 190f;
        banner.raycastTarget = false;
        Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(960, 190));

        TextMeshProUGUI text = CreateChunkyText("Title", banner.transform, title, 88, Color.white);
        Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(680, 110));
    }

    static void CreateBackButton(Transform parent, SceneLoader loader, string target)
    {
        Button back = CreateChunkyButton("BackButton", parent, "Back", "CC_Blue", new Vector2(0f, 1f), new Vector2(160, -100), new Vector2(250, 116), 58);
        UnityEventTools.AddStringPersistentListener(back.onClick, loader.LoadScene, target);
    }

    static Button CreateChunkyButton(string name, Transform parent, string text, string sprite, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
    {
        Image image = CreatePanel(name, parent, Color.white);
        Place(image.rectTransform, anchor, pos, size);
        ApplyChunky(image, sprite, size.y);
        Button button = image.gameObject.AddComponent<Button>();
        SetPressedTint(button);
        TextMeshProUGUI label = CreateChunkyText("Text", image.transform, text, fontSize, Color.white);
        StretchLabel(label, size.y);
        return button;
    }

    static Button CreateRoundButton(string name, Transform parent, string sprite, Vector2 anchor, Vector2 pos, float size)
    {
        Image image = CreateImage(name, parent, StyleSprite(sprite));
        Place(image.rectTransform, anchor, pos, new Vector2(size, size));
        Button button = image.gameObject.AddComponent<Button>();
        SetPressedTint(button);
        return button;
    }

    static TextMeshProUGUI CreateChunkyText(string name, Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI tmp = CreateText(name, parent, text, size, color);
        tmp.fontSharedMaterial = chunkyMaterial;
        // Shrink long text to fit on one line instead of wrapping
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Round(size * 0.5f);
        tmp.fontSizeMax = size;
        return tmp;
    }

    // Fills a chunky button/pill, keeping the text off the thick bottom lip
    static void StretchLabel(TextMeshProUGUI label, float height)
    {
        Stretch(label.rectTransform);
        float lip = (ChunkyLip + ChunkyShadow) / ChunkyScale(height);
        label.rectTransform.offsetMin = new Vector2(16, lip);
        label.rectTransform.offsetMax = new Vector2(-16, -4);
    }

    static float ChunkyScale(float height)
    {
        return Mathf.Clamp(160f / height, 0.9f, 2f);
    }

    // 9-sliced chunky sprite; small elements get a scaled-down border so every button looks alike
    static void ApplyChunky(Image image, string spriteName, float height)
    {
        image.sprite = StyleSprite(spriteName);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = ChunkyScale(height);
    }

    static void SetPressedTint(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        button.colors = colors;
    }

    // Draws a sprite with 4x4 supersampling; shade returns the colour at a point (crisp edges)
    static void RenderSprite(string name, int w, int h, System.Func<float, float, Color> shade, Vector4 border)
    {
        const int samples = 4;
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Color c = shade(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                        r += c.r * c.a;
                        g += c.g * c.a;
                        b += c.b * c.a;
                        a += c.a;
                    }
                }
                pixels[y * w + x] = a > 0 ? new Color(r / a, g / a, b / a, a / (samples * samples)) : Color.clear;
            }
        }
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();

        string path = StyleFolder + name + ".png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    static Color Lighten(Color c, float amount)
    {
        return new Color(Mathf.Lerp(c.r, 1, amount), Mathf.Lerp(c.g, 1, amount), Mathf.Lerp(c.b, 1, amount), c.a);
    }

    // Rounded button/panel: outline, gradient face with a glossy top half, darker bottom lip, hard drop shadow
    static void WriteChunkyRect(string name, Color top, Color bottom, Color lip, Color outlineColor, float gloss)
    {
        const int w = 256, h = 160;
        const float radius = 34, outline = 7;
        float cx = w / 2f, hx = w / 2f - 1;
        float bodyBottom = ChunkyShadow + 1, bodyTop = h - 1;
        float cy = (bodyBottom + bodyTop) / 2, hy = (bodyTop - bodyBottom) / 2;
        float faceBottom = bodyBottom + outline + ChunkyLip, faceTop = bodyTop - outline;
        float fcy = (faceBottom + faceTop) / 2, fhy = (faceTop - faceBottom) / 2;
        Color shadow = new Color(0, 0, 0, 0.35f);

        RenderSprite(name, w, h, (px, py) =>
        {
            if (RoundedRectDistance(px, py, cx, fcy, hx - outline, fhy, radius - outline) < 0)
            {
                float t = Mathf.InverseLerp(faceBottom, faceTop, py);
                Color c = Color.Lerp(bottom, top, t);
                if (gloss > 0 && t > 0.5f && RoundedRectDistance(px, py, cx, fcy, hx - outline - 7, fhy - 6, radius - outline - 7) < 0)
                {
                    c = Lighten(c, gloss);
                }
                return c;
            }
            if (RoundedRectDistance(px, py, cx, cy, hx - outline, hy - outline, radius - outline) < 0)
            {
                return lip;
            }
            if (RoundedRectDistance(px, py, cx, cy, hx, hy, radius) < 0)
            {
                return outlineColor;
            }
            if (RoundedRectDistance(px, py, cx, cy - ChunkyShadow, hx, hy, radius) < 0)
            {
                return shadow;
            }
            return Color.clear;
        }, new Vector4(radius + 2, radius + ChunkyLip + ChunkyShadow + 2, radius + 2, radius + 2));
    }

    // Round medal: same layers as the buttons
    static void WriteChunkyCircle(string name, Color top, Color bottom, Color lip)
    {
        const int size = 256;
        const float outline = 9;
        float cx = size / 2f, cy = size / 2f + ChunkyShadow / 2, radius = size / 2f - 1 - ChunkyShadow / 2;
        float faceRadius = radius - outline - ChunkyLip / 2, fcy = cy + ChunkyLip / 2;
        Color shadow = new Color(0, 0, 0, 0.35f);

        RenderSprite(name, size, size, (px, py) =>
        {
            float face = new Vector2(px - cx, py - fcy).magnitude;
            if (face < faceRadius)
            {
                float t = Mathf.InverseLerp(fcy - faceRadius, fcy + faceRadius, py);
                Color c = Color.Lerp(bottom, top, t);
                if (t > 0.5f && face < faceRadius - 8)
                {
                    c = Lighten(c, 0.25f);
                }
                return c;
            }
            float d = new Vector2(px - cx, py - cy).magnitude;
            if (d < radius - outline)
            {
                return lip;
            }
            if (d < radius)
            {
                return Ink;
            }
            if (new Vector2(px - cx, py - cy + ChunkyShadow).magnitude < radius)
            {
                return shadow;
            }
            return Color.clear;
        }, Vector4.zero);
    }

    // Blue ribbon: chunky plate with folded tails behind both ends
    static void WriteBanner(string name, Color top, Color bottom, Color lip)
    {
        const int w = 640, h = 200;
        const float radius = 28, outline = 7, tailOutline = 6;
        float cx = w / 2f, hx = w / 2f - 70;
        float bodyBottom = 52, bodyTop = h - 2;
        float cy = (bodyBottom + bodyTop) / 2, hy = (bodyTop - bodyBottom) / 2;
        float faceBottom = bodyBottom + outline + ChunkyLip, faceTop = bodyTop - outline;
        float fcy = (faceBottom + faceTop) / 2, fhy = (faceTop - faceBottom) / 2;
        Color tail = new Color32(30, 82, 196, 255), tailFold = new Color32(16, 46, 126, 255);
        Color shadow = new Color(0, 0, 0, 0.35f);

        System.Func<float, float, bool> inTail = (px, py) =>
        {
            float x = px < w / 2f ? px : w - px;
            const float left = 3, right = 150, low = 14, high = 120, notch = 44;
            float mid = (low + high) / 2, half = (high - low) / 2;
            return x >= left && x <= right && py >= low && py <= high && x - left >= notch * (1 - Mathf.Abs(py - mid) / half);
        };

        RenderSprite(name, w, h, (px, py) =>
        {
            if (RoundedRectDistance(px, py, cx, fcy, hx - outline, fhy, radius - outline) < 0)
            {
                float t = Mathf.InverseLerp(faceBottom, faceTop, py);
                Color c = Color.Lerp(bottom, top, t);
                if (t > 0.52f && RoundedRectDistance(px, py, cx, fcy, hx - outline - 8, fhy - 6, radius - outline - 8) < 0)
                {
                    c = Lighten(c, 0.22f);
                }
                return c;
            }
            if (RoundedRectDistance(px, py, cx, cy, hx - outline, hy - outline, radius - outline) < 0)
            {
                return lip;
            }
            if (RoundedRectDistance(px, py, cx, cy, hx, hy, radius) < 0)
            {
                return Ink;
            }
            if (inTail(px, py))
            {
                bool inner = inTail(px - tailOutline, py) && inTail(px + tailOutline, py) && inTail(px, py - tailOutline) && inTail(px, py + tailOutline);
                if (!inner)
                {
                    return Ink;
                }
                // Darker fold where the tail tucks behind the plate
                float x = px < w / 2f ? px : w - px;
                return x > 112 ? tailFold : tail;
            }
            if (RoundedRectDistance(px, py, cx, cy - ChunkyShadow, hx, hy, radius) < 0 || inTail(px, py + ChunkyShadow))
            {
                return shadow;
            }
            return Color.clear;
        }, new Vector4(170, 0, 170, 0));
    }

    static void WriteLock(string name)
    {
        const int size = 128;
        Color body = new Color32(255, 214, 80, 255), bodyDark = new Color32(222, 150, 24, 255), shackle = new Color32(196, 196, 214, 255);
        RenderSprite(name, size, size, (px, py) =>
        {
            float bodyD = RoundedRectDistance(px, py, 64, 42, 40, 32, 12);
            if (bodyD < 0)
            {
                if (bodyD > -6)
                {
                    return Ink;
                }
                // Keyhole
                if (new Vector2(px - 64, py - 48).magnitude < 8 || (Mathf.Abs(px - 64) < 4 && py > 28 && py < 48))
                {
                    return Ink;
                }
                return py > 46 ? body : bodyDark;
            }
            // Shackle: half ring with two legs
            float ringD;
            if (py >= 80)
            {
                float d = new Vector2(px - 64, py - 80).magnitude;
                ringD = Mathf.Max(d - 33, 15 - d);
            }
            else
            {
                float leg = Mathf.Min(Mathf.Abs(px - 40), Mathf.Abs(px - 88));
                ringD = Mathf.Max(leg - 9, Mathf.Max(60 - py, py - 80));
            }
            if (ringD < 0)
            {
                return ringD > -5 ? Ink : shackle;
            }
            return Color.clear;
        }, Vector4.zero);
    }

    // White icon with a thick dark outline; inside says whether a point is part of the shape
    static void WriteIcon(string name, System.Func<float, float, bool> inside)
    {
        const int size = 128;
        const float outline = 7;
        RenderSprite(name, size, size, (px, py) =>
        {
            if (inside(px, py))
            {
                return Color.white;
            }
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                float dx = Mathf.Cos(angle), dy = Mathf.Sin(angle);
                if (inside(px + dx * outline, py + dy * outline) || inside(px + dx * outline / 2, py + dy * outline / 2))
                {
                    return Ink;
                }
            }
            return Color.clear;
        }, Vector4.zero);
    }

    static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
    {
        Vector2 p = new Vector2(px - ax, py - ay), ab = new Vector2(bx - ax, by - ay);
        float t = Mathf.Clamp01(Vector2.Dot(p, ab) / ab.sqrMagnitude);
        return (p - ab * t).magnitude;
    }

    // Two beamed notes
    static void WriteMusicIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool head1 = Mathf.Pow((px - 42) / 19, 2) + Mathf.Pow((py - 34) / 14, 2) < 1;
            bool head2 = Mathf.Pow((px - 92) / 19, 2) + Mathf.Pow((py - 46) / 14, 2) < 1;
            bool stem1 = px > 51 && px < 61 && py > 34 && py < 102;
            bool stem2 = px > 101 && px < 111 && py > 46 && py < 114;
            bool beam = SegmentDistance(px, py, 56, 98, 106, 110) < 9;
            return head1 || head2 || stem1 || stem2 || beam;
        });
    }

    // Speaker with two sound waves
    static void WriteSoundIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool box = px > 14 && px < 40 && Mathf.Abs(py - 64) < 17;
            bool cone = px >= 36 && px < 64 && Mathf.Abs(py - 64) < 17 + (px - 36) * 0.9f;
            float d = new Vector2(px - 62, py - 64).magnitude;
            bool facing = px > 62 && Mathf.Abs(py - 64) < (px - 62) * 1.1f;
            bool wave1 = facing && Mathf.Abs(d - 24) < 6;
            bool wave2 = facing && Mathf.Abs(d - 44) < 6;
            return box || cone || wave1 || wave2;
        });
    }

    // Small rounded bar for the upgrade progress sliders
    static void WriteBar(string name, Color top, Color bottom, float outline)
    {
        const int w = 64, h = 32;
        const float radius = 14;
        RenderSprite(name, w, h, (px, py) =>
        {
            float d = RoundedRectDistance(px, py, w / 2f, h / 2f, w / 2f - 1, h / 2f - 1, radius);
            if (d >= 0)
            {
                return Color.clear;
            }
            if (d > -outline)
            {
                return Ink;
            }
            float t = py / h;
            Color c = Color.Lerp(bottom, top, t);
            return t > 0.55f ? Lighten(c, 0.2f) : c;
        }, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    #endregion
}
