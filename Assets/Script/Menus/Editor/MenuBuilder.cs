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
        BuildSelectMenu();
        BuildLevelSelect();
        AddScenesToBuild();
        RetargetButtons(MainMenuScene, CharacterScene, SelectMenuScene, null);
        RetargetButtons(CharacterScene, GameScene, SelectMenuScene, "Done");
        AddCharacterGuide();
        StyleMainMenu();
        BuildPauseMenu();

        if (!string.IsNullOrEmpty(startScene))
        {
            EditorSceneManager.OpenScene(startScene);
        }
        Debug.Log("Menu Builder: SelectMenu, LevelSelect and the DemoLevel pause menu are ready.");
    }

    #region Scenes

    static void BuildSelectMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndEventSystem();
        Transform canvas = CreateCanvas("Canvas", new Vector2(1080, 2340), 0, 0);
        SceneLoader loader = canvas.gameObject.AddComponent<SceneLoader>();

        CreateBackground(canvas);

        CreateHeader(canvas, "Get Ready!", "2 quick steps");

        Button characters = CreateStepCard(canvas, "Step1", 1, new Vector2(0, 420), "Characters",
            "Pick your keepers",
            "Start here!", "Team: {0}/{1}", GuidePulse.Condition.WhenNoTeam);
        UnityEventTools.AddStringPersistentListener(characters.onClick, loader.LoadScene, CharacterScene);

        Button levels = CreateStepCard(canvas, "Step2", 2, new Vector2(0, -150), "Level Select",
            "Choose a level",
            "Pick a team first", "Ready!", GuidePulse.Condition.WhenTeamReady);
        UnityEventTools.AddStringPersistentListener(levels.onClick, loader.LoadScene, LevelSelectScene);

        CreateBackButton(canvas, loader, MainMenuScene);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenesFolder + SelectMenuScene + ".unity");
    }

    static void BuildLevelSelect()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndEventSystem();
        Transform canvas = CreateCanvas("Canvas", new Vector2(1080, 2340), 0, 0);
        SceneLoader loader = canvas.gameObject.AddComponent<SceneLoader>();

        CreateBackground(canvas);

        CreateHeader(canvas, "Select Level", "Step 2 of 2");

        // Hint bar that changes once the player has a team
        Image hint = CreatePanel("Hint", canvas, Color.white);
        Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -470), new Vector2(900, 130));
        ApplyStyleSprite(hint, "PanelCream", 130);
        TextMeshProUGUI hintText = CreateText("HintText", hint.transform, "", 44, Navy);
        Stretch(hintText.rectTransform);
        hintText.rectTransform.offsetMin = new Vector2(30, 10);
        hintText.rectTransform.offsetMax = new Vector2(-30, -10);
        TeamStatusText hintStatus = hintText.gameObject.AddComponent<TeamStatusText>();
        hintStatus.emptyText = "Pick a team first!";
        hintStatus.readyText = "Tap Level 1 to play!";

        // Level grid
        GameObject grid = new GameObject("LevelGrid", typeof(RectTransform));
        grid.transform.SetParent(canvas, false);
        Place(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 1100));
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(420, 300);
        layout.spacing = new Vector2(60, 60);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        layout.childAlignment = TextAnchor.UpperCenter;

        for (int i = 0; i < LevelScenes.Length; i++)
        {
            CreateLevelButton(grid.transform, i + 1, LevelScenes[i]);
        }
        // Point the player at the first level
        grid.transform.GetChild(0).gameObject.AddComponent<GuidePulse>().amount = 0.04f;

        CreateBackButton(canvas, loader, SelectMenuScene);

        Button changeTeam = CreateTextButton("ChangeTeamButton", canvas, "Change Team", new Vector2(400, 130), 58);
        Place(changeTeam.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-260, 170), new Vector2(400, 130));
        UnityEventTools.AddStringPersistentListener(changeTeam.onClick, loader.LoadScene, CharacterScene);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenesFolder + LevelSelectScene + ".unity");
    }

    static void CreateHeader(Transform canvas, string title, string subtitle)
    {
        // Purple, like the PAUSED banner
        Image header = CreatePanel("Header", canvas, Color.white);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(900, 220));
        ApplyStyleSprite(header, "PanelPurple", 220);

        TextMeshProUGUI titleText = CreateText("Title", header.transform, title, 96, Color.white, true);
        Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(860, 120));
        // Shrink long titles to one line instead of wrapping into the subtitle
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 40;
        titleText.fontSizeMax = 96;

        TextMeshProUGUI subtitleText = CreateText("Subtitle", header.transform, subtitle, 44, LightGold, true);
        Place(subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -65), new Vector2(880, 70));
    }

    // A numbered step: label, big button, one-line explanation and a live team status line
    static Button CreateStepCard(Transform canvas, string name, int number, Vector2 pos, string buttonLabel,
        string caption, string emptyText, string readyText, GuidePulse.Condition pulse)
    {
        Image card = CreatePanel(name, canvas, Color.white);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(900, 500));
        ApplyStyleSprite(card, "PanelCream", 220);

        TextMeshProUGUI stepLabel = CreateText("StepLabel", card.transform, "STEP " + number, 54, Purple);
        Place(stepLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(820, 70));

        Button button = CreateTextButton(buttonLabel.Replace(" ", "") + "Button", card.transform, buttonLabel, new Vector2(640, 170), 88, ButtonStyle.Blue);
        Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(640, 170));
        button.gameObject.AddComponent<GuidePulse>().condition = pulse;

        TextMeshProUGUI captionText = CreateText("Caption", card.transform, caption, 40, Navy);
        Place(captionText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -85), new Vector2(840, 70));

        TextMeshProUGUI statusText = CreateText("Status", card.transform, emptyText, 46, Navy);
        Place(statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -165), new Vector2(840, 70));
        TeamStatusText status = statusText.gameObject.AddComponent<TeamStatusText>();
        status.emptyText = emptyText;
        status.readyText = readyText;

        return button;
    }

    // Adds titles, hints and labels to the existing character (tower select) screen
    static void AddCharacterGuide()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + CharacterScene + ".unity");
        Transform canvas = scene.GetRootGameObjects().First(r => r.name == "Canvas").transform;

        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Guide_")).ToList())
        {
            Object.DestroyImmediate(t.gameObject);
        }

        Transform picker = canvas.Find("TowerSelectUI");
        Color lightOrange = new Color32(255, 190, 90, 255);
        Color lightGreen = new Color32(150, 235, 130, 255);

        // Front view guide, hidden while the picker is open (drawn under the picker)
        GameObject frontView = new GameObject("Guide_FrontView", typeof(RectTransform));
        frontView.transform.SetParent(canvas, false);
        Stretch((RectTransform)frontView.transform);

        Button openPicker = canvas.GetComponentsInChildren<Button>(true).First(b => b.name == "TowerSelectButton");
        Button closePicker = picker.GetComponentsInChildren<Button>(true).First(b => b.name == "CloseButton");
        RemoveMissingListeners(openPicker);
        RemoveMissingListeners(closePicker);
        UnityEventTools.AddBoolPersistentListener(openPicker.onClick, frontView.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(closePicker.onClick, frontView.SetActive, true);

        TextMeshProUGUI title = CreateText("Title", frontView.transform, "Your Team", 70, Color.white, true);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1000, 90));

        TextMeshProUGUI team = CreateText("TeamStatus", frontView.transform, "", 40, Color.white, true);
        Place(team.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 185), new Vector2(1000, 60));
        TeamStatusText teamStatus = team.gameObject.AddComponent<TeamStatusText>();
        teamStatus.emptyText = "No keepers yet";
        teamStatus.readyText = "{0}/{1} keepers";
        teamStatus.emptyColor = lightOrange;
        teamStatus.readyColor = lightGreen;

        TextMeshProUGUI tapHint = CreateText("TapHint", frontView.transform,"Tap the keeper", 38, Color.white, true);
        Place(tapHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -128), new Vector2(950, 60));
        tapHint.gameObject.AddComponent<GuidePulse>().amount = 0.05f;

        // Bigger, clearer Done button. The whole front view is one compact column around the keeper
        // (+295..-283) so it fits even when the screen is landscape (canvas only ~600 units tall)
        Transform done = canvas.GetComponentsInChildren<Button>(true).First(b => b.name == "PlayButton").transform;
        done.SetParent(canvas, false);
        Place((RectTransform)done, new Vector2(0.5f, 0.5f), new Vector2(0, -238), new Vector2(360, 90));
        // Blue main-action button, like Continue
        ApplyStyleSprite(done.GetComponent<Image>(), "ButtonBlue", 90);
        Outline oldBorder = done.GetComponent<Outline>();
        if (oldBorder)
        {
            Object.DestroyImmediate(oldBorder);
        }
        TextMeshProUGUI doneLabel = done.GetComponentInChildren<TextMeshProUGUI>(true);
        doneLabel.text = "Done";
        doneLabel.fontSize = 54;
        doneLabel.color = Color.white;
        ApplyFont(doneLabel, true);
        doneLabel.rectTransform.offsetMin = new Vector2(0, 5);
        doneLabel.rectTransform.offsetMax = Vector2.zero;

        // Round gold + purple close button in the picker, matching the pause menu's round buttons
        Image closeImage = closePicker.GetComponent<Image>();
        closeImage.sprite = StyleSprite("ButtonRound");
        closeImage.type = Image.Type.Simple;
        closeImage.color = Color.white;
        TextMeshProUGUI closeLabel = closePicker.GetComponentInChildren<TextMeshProUGUI>(true);
        closeLabel.color = DarkBrown;
        ApplyFont(closeLabel, false);
        closeLabel.fontSize = 36;
        if (!done.GetComponent<GuidePulse>())
        {
            done.gameObject.AddComponent<GuidePulse>().condition = GuidePulse.Condition.WhenTeamReady;
        }

        TextMeshProUGUI doneHint = CreateText("Guide_DoneHint", canvas, "", 32, Color.white, true);
        Place(doneHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -178), new Vector2(1000, 50));
        TeamStatusText doneStatus = doneHint.gameObject.AddComponent<TeamStatusText>();
        doneStatus.emptyText = "Pick at least 1";
        doneStatus.readyText = "All set? Tap Done";
        doneStatus.emptyColor = lightOrange;
        doneStatus.readyColor = lightGreen;

        // Keep the front view under the picker
        int pickerIndex = picker.GetSiblingIndex();
        foreach (Transform t in new Transform[] { frontView.transform, doneHint.transform })
        {
            t.SetSiblingIndex(pickerIndex);
            pickerIndex = picker.GetSiblingIndex();
        }
        // Done stays usable (and uncovered) while the picker is open
        done.SetAsLastSibling();

        // Picker panels and slot counters in the same style (cream = your team, purple = all keepers)
        ApplyStyleSprite(picker.Find("EquippedTowers").GetComponent<Image>(), "PanelCream", 150);
        ApplyStyleSprite(picker.Find("UnlockedTowers").GetComponent<Image>(), "PanelPurple", 350);
        foreach (TextMeshProUGUI counter in picker.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t => t.name == "UpgradeText"))
        {
            ApplyFont(counter, false);
        }

        // Picker labels (ignored by the slot grids' layout)
        TextMeshProUGUI teamLabel = CreateText("Guide_TeamLabel", picker.Find("EquippedTowers"), "YOUR TEAM\n<size=75%>tap to remove</size>", 32, Purple);
        Place(teamLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-125, 0), new Vector2(230, 130));
        teamLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        TextMeshProUGUI addLabel = CreateText("Guide_AddLabel", picker.Find("UnlockedTowers"), "Tap to add (max 4)", 38, Color.white, true);
        Place(addLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 45), new Vector2(1000, 70));
        addLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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

    static void CreateLevelButton(Transform parent, int number, string sceneName)
    {
        GameObject go = new GameObject("Level" + number, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        // Levels that exist look like the blue pause buttons; ones not made yet are grey
        bool exists = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenesFolder + sceneName + ".unity");
        ApplyStyleSprite(image, exists ? "ButtonBlue" : "ButtonGray", 300);

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white; // the grey art already shows it's locked
        button.colors = colors;

        TextMeshProUGUI label = CreateText("Label", go.transform, "Level " + number, 80, Color.white, true);
        Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(400, 110));

        TextMeshProUGUI status = CreateText("Status", go.transform, "Play", 50, exists ? LightGold : new Color32(230, 230, 238, 255), true);
        Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(400, 80));

        LevelButton levelButton = go.AddComponent<LevelButton>();
        levelButton.sceneName = sceneName;
        levelButton.statusText = status;
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

        // Pause button (top right of the HUD)
        // Round gold + purple button, same look as the Music / Sound buttons
        Button pauseButton = CreateTextButton("PauseButton", canvas, "II", new Vector2(130, 130), 64);
        Place(pauseButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-95, -95), new Vector2(130, 130));
        Image pauseImage = pauseButton.GetComponent<Image>();
        pauseImage.sprite = StyleSprite("ButtonRound");
        pauseImage.type = Image.Type.Simple;
        RectTransform pauseLabel = pauseButton.GetComponentInChildren<TextMeshProUGUI>().rectTransform;
        pauseLabel.offsetMin = new Vector2(0, 4);
        pauseLabel.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(pauseButton.onClick, pause.Pause);

        // Dimmed overlay that also blocks touches to the game behind it
        Image panel = CreatePanel("PausePanel", canvas, new Color(0, 0, 0, 0.65f));
        Stretch(panel.rectTransform);

        Sprite banner = LoadSprite("Pausedbanner");
        Image bannerImage = CreateImage("PausedBanner", panel.transform, banner);
        bannerImage.raycastTarget = false;
        Place(bannerImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 340), SizeFromWidth(banner, 1000));

        Button continueButton = CreateSpriteButton("ContinueButton", panel.transform, LoadSprite("continuebutton"), new Vector2(0, 160), 125);
        UnityEventTools.AddPersistentListener(continueButton.onClick, pause.Continue);

        // RestartButton.png has the top of another button cropped into its bottom edge, so mask it off
        Button restartButton = CreateSpriteButton("RestartButton", panel.transform, LoadSprite("RestartButton"), new Vector2(0, 10), 125, 20f / 134f);
        UnityEventTools.AddPersistentListener(restartButton.onClick, pause.Restart);

        Button giveUpButton = CreateSpriteButton("GiveUpButton", panel.transform, LoadSprite("GiveUpButton"), new Vector2(0, -140), 125);
        UnityEventTools.AddPersistentListener(giveUpButton.onClick, pause.GiveUp);

        CreateAudioButton("MusicButton", panel.transform, LoadSprite("musicbutton"), new Vector2(-110, -320), AudioChannel.Channel.Music);
        CreateAudioButton("SoundButton", panel.transform, LoadSprite("SoundButton"), new Vector2(110, -320), AudioChannel.Channel.Sound);

        SerializedObject so = new SerializedObject(pause);
        so.FindProperty("pausePanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton.gameObject;
        so.FindProperty("giveUpScene").stringValue = LevelSelectScene;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void CreateAudioButton(string name, Transform parent, Sprite sprite, Vector2 pos, AudioChannel.Channel channel)
    {
        Button button = CreateSpriteButton(name, parent, sprite, pos, 140);

        // Red slash shown while muted
        Image slash = CreatePanel("OffSlash", button.transform, new Color32(220, 40, 40, 255));
        slash.raycastTarget = false;
        Place(slash.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 18));
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

    static void CreateBackground(Transform canvas)
    {
        Sprite sprite = LoadSprite("LevelBackground");
        Image bg = CreateImage("Background", canvas, sprite);
        bg.raycastTarget = false;
        Place(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 2340));
        if (sprite)
        {
            AspectRatioFitter fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }
    }

    static void CreateBackButton(Transform canvas, SceneLoader loader, string target)
    {
        Button back = CreateTextButton("BackButton", canvas, "Back", new Vector2(300, 130), 64);
        Place(back.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(210, 170), new Vector2(300, 130));
        UnityEventTools.AddStringPersistentListener(back.onClick, loader.LoadScene, target);
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
}
