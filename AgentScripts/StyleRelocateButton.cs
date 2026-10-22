using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Restyles the in-level relocate button to the chunky CC_ look (blue circle + white outlined icon)
// Run: StyleRelocateButton.Run("Assets/Scenes/DemoLevel.unity,Assets/Scenes/Levels/World1/Level2.unity")
public static class StyleRelocateButton
{
    const string Folder = "Assets/UserInterface/Fonts/Generated/";
    static readonly Color Ink = new Color32(29, 21, 51, 255);

    public static string Run(string scenes)
    {
        var builder = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MenuBuilder")).First(t => t != null);
        builder.GetMethod("WriteRelocateIcon", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "CC_IconRelocate" });
        AssetDatabase.Refresh();

        string log = "";
        foreach (string path in scenes.Split(','))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            var buttonT = all.FirstOrDefault(t => t.name == "RelocateButton");
            if (!buttonT) { log += path + ": no RelocateButton; "; continue; }

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "CC_CircleBlue.png");
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "CC_IconRelocate.png");

            var button = buttonT.GetComponent<Button>();
            ((Image)button.targetGraphic).sprite = circle;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.75f, 1f);
            button.colors = colors;

            // Icon sits a little high so it clears the circle's bottom lip, like the pause "II"
            var iconT = buttonT.GetComponentsInChildren<Transform>(true).First(t => t.name == "RelocationIcon");
            var iconImage = iconT.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            var rt = (RectTransform)iconT;
            rt.anchorMin = new Vector2(0.17f, 0.21f);
            rt.anchorMax = new Vector2(0.83f, 0.87f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Cooldown sweep: dark ink wash instead of flat black
            var fill = buttonT.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Fill");
            if (fill) fill.GetComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.6f);

            foreach (var g in new Graphic[] { iconImage }) EditorUtility.SetDirty(g);
            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log += path + ": ok; ";
        }
        return log;
    }
}
