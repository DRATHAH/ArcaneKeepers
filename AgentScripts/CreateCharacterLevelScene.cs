using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Creates an empty Assets/Scenes/CharacterLevel.unity with the same base as the landscape menus:
// camera, EventSystem (Input System), 1920x1080 Expand canvas, enveloping Main-Background, SafeArea "Safe".
public static class CreateCharacterLevelScene
{
    public static string Run()
    {
        const string scenePath = "Assets/Scenes/CharacterLevel.unity";
        if (System.IO.File.Exists(scenePath)) return "already exists: " + scenePath;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = LayerMask.NameToLayer("UI");
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UserInterface/Main-Background.png");
        var bg = Child("Background", canvasGo.transform);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.raycastTarget = false;
        var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = bgSprite ? bgSprite.rect.width / bgSprite.rect.height : 16f / 9f;

        var safe = Child("Safe", canvasGo.transform);
        safe.gameObject.AddComponent<SafeArea>();

        EditorSceneManager.SaveScene(scene, scenePath);
        return "created " + scenePath;
    }

    static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var t = go.GetComponent<RectTransform>();
        t.SetParent(parent, false);
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.sizeDelta = Vector2.zero;
        return t;
    }
}
