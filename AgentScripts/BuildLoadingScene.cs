using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class BuildLoadingScene
{
    const string ScenePath = "Assets/Scenes/Loading.unity";
    const string FramesFolder = "Assets/UserInterface/LoadingFrames";
    static readonly Color Bg = new Color32(5, 5, 5, 255);
    static readonly Color TextGold = new Color32(255, 222, 99, 255);

    public static string Run()
    {
        // Frames: plain single sprites
        var framePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { FramesFolder })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
        foreach (string p in framePaths)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        Sprite[] frames = framePaths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();

        TMP_FontAsset font = Style<TMP_FontAsset>("LilitaOne SDF");
        Material chunky = Style<Material>("LilitaOne SDF Chunky");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
        camGo.transform.position = new Vector3(0, 0, -10);

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        GameObject canvasGo = new GameObject("LoadingCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform root = canvasGo.transform;

        Image bg = NewImage("Background", root, null);
        bg.color = Bg;
        Stretch(bg.rectTransform);

        // Cat animation in the middle
        Image anim = NewImage("Animation", root, frames.Length > 0 ? frames[0] : null);
        anim.preserveAspect = true;
        Place(anim.rectTransform, new Vector2(0, 90), new Vector2(480, 428));

        // Loading bar beneath it, in the chunky "critter" style of the level/character select screens:
        // stone frame with a dark ink outline and lip, a dark inset well, and a glossy gold fill
        const float barWidth = 920, barHeight = 104;
        RectTransform bar = NewRect("LoadingBar", root);
        Place(bar, new Vector2(0, -250), new Vector2(barWidth, barHeight));

        Image frame = NewImage("Frame", bar, Style<Sprite>("CC_Stone"));
        Chunky(frame, barHeight);
        Stretch(frame.rectTransform);

        // Inset well, kept off the frame's bottom lip
        Image track = NewImage("Track", bar, Style<Sprite>("CC_BarBg"));
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 0.8f;
        Stretch(track.rectTransform);
        track.rectTransform.offsetMin = new Vector2(18, 30);
        track.rectTransform.offsetMax = new Vector2(-18, -14);

        RectTransform fillArea = NewRect("FillArea", track.transform);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(6, 6);
        fillArea.offsetMax = new Vector2(-6, -6);

        // The fill's right anchor follows progress; sliced so the leading edge stays rounded
        Image fill = NewImage("Fill", fillArea, Style<Sprite>("CC_BarFill"));
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 0.9f;
        Stretch(fill.rectTransform);
        fill.rectTransform.anchorMax = new Vector2(0, 1);

        TextMeshProUGUI percent = NewText("Percent", track.transform, "0%", 40, font, chunky);
        Stretch(percent.rectTransform);
        percent.rectTransform.offsetMin = new Vector2(0, 2);

        TextMeshProUGUI label = NewText("LoadingLabel", root, "Loading...", 64, font, chunky);
        label.color = TextGold;
        Place(label.rectTransform, new Vector2(0, -145), new Vector2(900, 80));

        LoadingScreen screen = canvasGo.AddComponent<LoadingScreen>();
        screen.animationImage = anim;
        screen.frames = frames;
        screen.frameTime = 0.1f;
        screen.barFill = fill.rectTransform;
        screen.percentText = percent;

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);

        // Add to build settings
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        return "Built " + ScenePath + " with " + frames.Length + " frames";
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TMP_FontAsset font, Material mat)
    {
        TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        if (font) t.font = font;
        if (mat) t.fontSharedMaterial = mat;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // The generated style assets (MenuBuilder) live under UserInterface/.../Generated; find them by name
    static T Style<T>(string name) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:" + typeof(T).Name, new[] { "Assets/UserInterface" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        Debug.LogWarning("BuildLoadingScene: style asset not found: " + name);
        return null;
    }

    // Same 9-slice scale as MenuBuilder.ApplyChunky so the frame matches the menu buttons/panels
    static void Chunky(Image image, float height)
    {
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = Mathf.Clamp(160f / height, 0.9f, 2f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
