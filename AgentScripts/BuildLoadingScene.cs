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
    const string StyleFolder = "Assets/UserInterface/Generated/";
    static readonly Color Bg = new Color32(5, 5, 5, 255);

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

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StyleFolder + "LilitaOne SDF.asset");
        Material outline = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Outline.mat");

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

        // Loading bar beneath it
        RectTransform bar = NewRect("LoadingBar", root);
        Place(bar, new Vector2(0, -230), new Vector2(900, 64));

        Image track = NewImage("Track", bar, AssetDatabase.LoadAssetAtPath<Sprite>(StyleFolder + "PanelPurple.png"));
        Sliced(track, 64);
        Stretch(track.rectTransform);

        RectTransform fillMask = NewRect("FillMask", bar);
        fillMask.gameObject.AddComponent<RectMask2D>();
        fillMask.anchorMin = new Vector2(0, 0);
        fillMask.anchorMax = new Vector2(0, 1);
        fillMask.pivot = new Vector2(0, 0.5f);
        fillMask.offsetMin = Vector2.zero;
        fillMask.offsetMax = Vector2.zero;

        Image fill = NewImage("Fill", fillMask, AssetDatabase.LoadAssetAtPath<Sprite>(StyleFolder + "ButtonGold.png"));
        Sliced(fill, 64);
        RectTransform fr = fill.rectTransform;
        fr.anchorMin = new Vector2(0, 0);
        fr.anchorMax = new Vector2(0, 1);
        fr.pivot = new Vector2(0, 0.5f);
        fr.anchoredPosition = Vector2.zero;
        fr.sizeDelta = new Vector2(900, 0);

        TextMeshProUGUI percent = NewText("Percent", bar, "0%", 40, font, outline);
        Stretch(percent.rectTransform);

        TextMeshProUGUI label = NewText("LoadingLabel", root, "Loading...", 56, font, outline);
        Place(label.rectTransform, new Vector2(0, -160), new Vector2(900, 70));

        LoadingScreen screen = canvasGo.AddComponent<LoadingScreen>();
        screen.animationImage = anim;
        screen.frames = frames;
        screen.frameTime = 0.1f;
        screen.barFill = fillMask;
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

    static void Sliced(Image image, float height)
    {
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = Mathf.Clamp(130f / height, 0.6f, 3f);
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
