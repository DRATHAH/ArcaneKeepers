using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause menu builder. Makes Assets/UserInterface/PauseMenu/Prefabs/PauseCanvas.prefab in the Clash of Critters style
// (stone card, blue ribbon, chunky blue/gold/red buttons, Lilita One) and puts an instance of it in every level,
// replacing any older PauseCanvas. Menu: Arcane Keepers > Rebuild Pause Menu.
public static partial class MenuBuilder
{
    const string PauseFolder = "Assets/UserInterface/PauseMenu/";
    const string PausePrefabPath = PauseFolder + "Prefabs/PauseCanvas.prefab";
    const string SoundManagerPrefabPath = "Assets/Prefabs/Effects/SoundManager.prefab";

    // Scenes / prefabs that get the pause menu
    static readonly string[] PauseScenes =
    {
        "Assets/Scenes/DemoLevel.unity",
        "Assets/Scenes/Levels/World1/Level2.unity",
        "Assets/Scenes/Levels/World1/Level3.unity",
        "Assets/Scenes/Levels/World1/Level4.unity",
        "Assets/Scenes/EnemyTester.unity",
    };
    const string LevelTemplatePath = "Assets/Prefabs/LevelTemplate.prefab";

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
        Debug.Log("Menu Builder: pause menu rebuilt and placed in every level.");
    }

    static void BuildPauseMenu()
    {
        EnsurePauseIcons();
        BuildPausePrefab();
        foreach (string path in PauseScenes)
        {
            PlacePauseMenu(path);
        }
        PlacePauseMenuInTemplate();
    }

    // ---- prefab ----

    static void BuildPausePrefab()
    {
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform canvas = CreateCanvas("PauseCanvas", new Vector2(1920, 1080), 1, 50);
        PauseMenu pause = canvas.gameObject.AddComponent<PauseMenu>();
        PauseMenuAnimator animator = canvas.gameObject.AddComponent<PauseMenuAnimator>();

        // Pause button: round blue chunky button with a pause icon, top-right inside the safe area
        Transform hudSafe = CreateSafeArea(canvas, "PauseButtonSafe");
        Button pauseButton = CreateRoundButton("PauseButton", hudSafe, "CC_CircleBlue", new Vector2(1, 1), new Vector2(-95, -95), 130);
        AddIcon(pauseButton.transform, PauseIcon("Pause_IconPause"), new Vector2(0, 6), 68);
        UnityEventTools.AddPersistentListener(pauseButton.onClick, pause.Pause);

        // Dim backdrop that also blocks touches to the game behind it
        Image panel = CreatePanel("PausePanel", canvas, new Color(Ink.r, Ink.g, Ink.b, 0.72f));
        panel.sprite = null;
        panel.type = Image.Type.Simple;
        Stretch(panel.rectTransform);
        Transform safe = CreateSafeArea(panel.transform);
        Vector2 center = new Vector2(0.5f, 0.5f);

        // Stone card with the blue ribbon across its top, like the select screens
        Image card = CreatePanel("Card", safe, Color.white);
        Place(card.rectTransform, center, new Vector2(0, -30), new Vector2(760, 700));
        ApplyChunky(card, "CC_Stone", 700);
        card.raycastTarget = false;

        Image banner = CreatePanel("Banner", safe, Color.white);
        banner.sprite = StyleSprite("CC_Banner");
        banner.type = Image.Type.Sliced;
        banner.pixelsPerUnitMultiplier = 200f / 190f;
        banner.raycastTarget = false;
        Place(banner.rectTransform, center, new Vector2(0, 330), new Vector2(900, 190));
        TextMeshProUGUI title = CreateChunkyText("Title", banner.transform, "PAUSED", 96, Color.white);
        Place(title.rectTransform, center, new Vector2(0, 24), new Vector2(600, 110));

        // Blue = main action, gold = secondary, red = leave
        Button continueButton = CreateIconButton("ContinueButton", safe, "CONTINUE", "CC_Blue", "Pause_IconPlay", new Vector2(0, 150), new Vector2(560, 140), 70);
        UnityEventTools.AddPersistentListener(continueButton.onClick, pause.Continue);

        Button restartButton = CreateIconButton("RestartButton", safe, "RESTART", "CC_Gold", "Pause_IconRestart", new Vector2(0, 5), new Vector2(500, 118), 58);
        UnityEventTools.AddPersistentListener(restartButton.onClick, pause.Restart);

        Button giveUpButton = CreateIconButton("GiveUpButton", safe, "GIVE UP", "CC_Red", "Pause_IconFlag", new Vector2(0, -130), new Vector2(500, 118), 58);
        UnityEventTools.AddPersistentListener(giveUpButton.onClick, pause.GiveUp);

        Button music = CreatePauseAudioButton("MusicButton", safe, "CC_IconMusic", new Vector2(-85, -272), AudioChannel.Channel.Music);
        Button sound = CreatePauseAudioButton("SoundButton", safe, "CC_IconSound", new Vector2(85, -272), AudioChannel.Channel.Sound);

        // Teammates' click sound on every button
        Component soundManager = AssetDatabase.LoadAssetAtPath<GameObject>(SoundManagerPrefabPath)?.GetComponent("SoundManager");
        foreach (Button b in new[] { pauseButton, continueButton, restartButton, giveUpButton, music, sound })
        {
            if (soundManager is SoundManager manager)
            {
                UnityEventTools.AddStringPersistentListener(b.onClick, manager.PlaySound, "ButtonClick");
            }
        }

        SerializedObject so = new SerializedObject(pause);
        so.FindProperty("pausePanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton.gameObject;
        so.FindProperty("giveUpScene").stringValue = MainMenuScene;
        so.ApplyModifiedPropertiesWithoutUndo();

        animator.overlay = panel;
        animator.card = card.rectTransform;
        animator.banner = banner.rectTransform;
        animator.buttons = new[]
        {
            (RectTransform)continueButton.transform, (RectTransform)restartButton.transform, (RectTransform)giveUpButton.transform,
            (RectTransform)music.transform, (RectTransform)sound.transform,
        };

        panel.gameObject.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PausePrefabPath);
        Object.DestroyImmediate(canvas.gameObject);
    }

    // Chunky button with a white icon on the left and the label beside it
    static Button CreateIconButton(string name, Transform parent, string text, string sprite, string icon, Vector2 pos, Vector2 size, float fontSize)
    {
        Button button = CreateChunkyButton(name, parent, text, sprite, new Vector2(0.5f, 0.5f), pos, size, fontSize);
        float lip = (ChunkyLip + ChunkyShadow) / ChunkyScale(size.y);
        float iconSize = size.y * 0.52f;

        Image iconImage = AddIcon(button.transform, PauseIcon(icon), Vector2.zero, iconSize);
        RectTransform rt = iconImage.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(30 + iconSize * 0.5f, lip * 0.5f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.rectTransform.offsetMin = new Vector2(40 + iconSize, label.rectTransform.offsetMin.y);
        label.rectTransform.offsetMax = new Vector2(-30, label.rectTransform.offsetMax.y);
        return button;
    }

    static Image AddIcon(Transform parent, Sprite sprite, Vector2 pos, float size)
    {
        Image image = CreateImage("Icon", parent, sprite);
        image.raycastTarget = false;
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
        return image;
    }

    // Round blue button with a white icon; a red slash covers it while muted
    static Button CreatePauseAudioButton(string name, Transform parent, string icon, Vector2 pos, AudioChannel.Channel channel)
    {
        Button button = CreateRoundButton(name, parent, "CC_CircleBlue", new Vector2(0.5f, 0.5f), pos, 130);
        AddIcon(button.transform, StyleSprite(icon), new Vector2(0, 5), 76);

        Image slash = CreateImage("OffSlash", button.transform, StyleSprite("CC_Slash"));
        slash.raycastTarget = false;
        slash.preserveAspect = false;
        slash.type = Image.Type.Sliced;
        Place(slash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(136, 30));
        slash.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        slash.gameObject.SetActive(false);

        AudioToggleButton toggle = button.gameObject.AddComponent<AudioToggleButton>();
        toggle.channel = channel;
        toggle.offMarker = slash.gameObject;
        return button;
    }

    // ---- placing it in levels ----

    static void PlacePauseMenu(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogWarning("Menu Builder: no scene at " + scenePath);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);

        int index = -1;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "PauseCanvas" || root.GetComponentInChildren<PauseMenu>(true))
            {
                if (index < 0) index = root.transform.GetSiblingIndex();
                Object.DestroyImmediate(root);
            }
        }
        if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<EventSystem>(true)))
        {
            CreateEventSystem();
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (index >= 0)
        {
            instance.transform.SetSiblingIndex(index);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // The teammates' level template gets the same prefab nested inside it
    static void PlacePauseMenuInTemplate()
    {
        if (!System.IO.File.Exists(LevelTemplatePath))
        {
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(LevelTemplatePath);
        int index = -1;
        foreach (Transform child in root.transform.Cast<Transform>().ToList())
        {
            if (child.name == "PauseCanvas" || child.GetComponentInChildren<PauseMenu>(true))
            {
                if (index < 0) index = child.GetSiblingIndex();
                Object.DestroyImmediate(child.gameObject);
            }
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        if (index >= 0)
        {
            instance.transform.SetSiblingIndex(index);
        }
        PrefabUtility.SaveAsPrefabAsset(root, LevelTemplatePath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---- icons (white with the Ink outline, like the other CC icons) ----

    static Sprite PauseIcon(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PauseFolder + "Art/" + name + ".png");
    }

    static void EnsurePauseIcons()
    {
        string art = PauseFolder + "Art/";
        WritePauseBarsIcon(art + "Pause_IconPause");
        WritePlayIcon(art + "Pause_IconPlay");
        WriteRestartIcon(art + "Pause_IconRestart");
        WriteFlagIcon(art + "Pause_IconFlag");
        AssetDatabase.Refresh();
    }

    // Two rounded bars
    static void WritePauseBarsIcon(string path)
    {
        WriteIcon(path, (px, py) =>
            RoundedRectDistance(px, py, 45, 64, 13, 38, 6) < 0 || RoundedRectDistance(px, py, 83, 64, 13, 38, 6) < 0);
    }

    // Right-pointing triangle with soft corners
    static void WritePlayIcon(string path)
    {
        WriteIcon(path, (px, py) =>
        {
            float x = px - 40;
            return x > 0 && x < 64 && Mathf.Abs(py - 64) < (64 - x) * 0.72f;
        });
    }

    // Circular arrow (restart)
    static void WriteRestartIcon(string path)
    {
        WriteIcon(path, (px, py) =>
        {
            float dx = px - 64, dy = py - 64;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;
            bool ring = r > 30 && r < 45 && !(angle > 35 && angle < 105);

            // Arrowhead at the end of the arc, pointing along it
            float a = 35 * Mathf.Deg2Rad;
            Vector2 n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 t = new Vector2(-n.y, n.x);
            Vector2 p = new Vector2(64, 64) + n * 37.5f - t * 4;
            bool head = InTriangle(new Vector2(px, py), p + t * 26, p - n * 25, p + n * 25);
            return ring || head;
        });
    }

    // Waving white flag on a pole (give up)
    static void WriteFlagIcon(string path)
    {
        WriteIcon(path, (px, py) =>
        {
            bool pole = RoundedRectDistance(px, py, 34, 64, 6, 50, 5) < 0;
            float wave = Mathf.Sin((px - 40) / 64f * Mathf.PI * 2) * 5;
            bool cloth = px > 38 && px < 108 && py < 110 + wave && py > 64 + wave;
            return pole || cloth;
        });
    }

    static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0;
        bool pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    static float Cross(Vector2 p, Vector2 a, Vector2 b)
    {
        return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }
}
