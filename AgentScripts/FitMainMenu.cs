using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Main menu screen fitting: the background keeps filling the screen (envelope), while the
// logo, characters and "Tap to play" move into a fixed-size Stage that ScaleToFit keeps
// centred and fully visible on any aspect (wide phones, Fold, tablets)
public static class FitMainMenu
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    // textY = "Tap to play" centre as a fraction of the stage height
    public static string Run(float textY, float fontSize)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = (RectTransform)GameObject.Find("MainMenuCanvas").transform;
        var background = (RectTransform)canvas.Find("Background");
        var bgFitter = background.GetComponent<UnityEngine.UI.AspectRatioFitter>();

        var stageT = background.Find("Stage");
        if (stageT == null)
        {
            var go = new GameObject("Stage", typeof(RectTransform));
            go.layer = background.gameObject.layer;
            stageT = go.transform;
            stageT.SetParent(background, false);
        }
        var stage = (RectTransform)stageT;
        stage.SetAsLastSibling();

        // Stage = the background's own shape at canvas width, so child anchors keep their meaning
        float w = canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution.x;
        var fit = stage.GetComponent<ScaleToFit>();
        if (fit == null) fit = stage.gameObject.AddComponent<ScaleToFit>();
        fit.size = new Vector2(w, w / bgFitter.aspectRatio);
        fit.minScale = 0.2f;
        fit.maxScale = 1f;
        fit.area = canvas;

        foreach (string n in new[] { "Characters", "Logo" })
        {
            var t = background.Find(n);
            if (t != null) t.SetParent(stage, false);
        }
        // Children were stretched over Background; stretch them over the stage instead
        var chars = (RectTransform)stage.Find("Characters");
        chars.anchorMin = Vector2.zero; chars.anchorMax = Vector2.one;
        chars.offsetMin = chars.offsetMax = Vector2.zero;

        var text = canvas.Find("StartButton/Text (TMP)") ?? stage.Find("Text (TMP)");
        text.SetParent(stage, false);
        var tr = (RectTransform)text;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, textY);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = Vector2.zero;
        tr.sizeDelta = new Vector2(400, 70);
        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.raycastTarget = false; // full-screen StartButton stays the tap target
        text.SetAsLastSibling();

        // Logo shake should still move the whole backdrop
        var logo = stage.Find("Logo").GetComponent<LogoIntro>();
        var so = new SerializedObject(logo);
        so.FindProperty("shakeTarget").objectReferenceValue = background;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"stage {fit.size}";
    }
}
