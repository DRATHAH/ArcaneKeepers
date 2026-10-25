using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lays out the MainMenu effect layers (safe to re-run; replaces previous ones) and saves the scene:
//   MainMenuCanvas
//     Background
//       SkyFx            ShootingStars   (behind the Stage: comets pass behind logo + characters)
//       Stage
//         Logo
//           Glory        LogoGlory       (sunburst behind the title)
//           Title
//         Text (TMP)     PromptPulse
//     AmbientFx          MagicMotes      (over the art)
//     TapFx              TapSparkle
//     StartButton                        (full-screen tap target, stays last)
// Usage: run_script AgentScripts/SetupMenuFx.cs SetupMenuFx.Run
public static class SetupMenuFx
{
    public static string Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        var canvas = scene.GetRootGameObjects().First(g => g.name == "MainMenuCanvas").transform;
        var bg = canvas.Find("Background");
        var logo = bg.Find("Stage/Logo");
        var title = logo.Find("Title");
        var prompt = bg.Find("Stage/Text (TMP)");
        var start = canvas.Find("StartButton");

        // Clear earlier attempts (incl. the old all-in-one MenuMagicFx and the removed logo shine).
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bg.gameObject);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(title.gameObject);
        foreach (var t in new[] { bg.Find("SkyFx"), logo.Find("Glory"), canvas.Find("AmbientFx"), canvas.Find("TapFx") })
            if (t) Object.DestroyImmediate(t.gameObject);

        Layer<ShootingStars>("SkyFx", bg).SetAsFirstSibling();

        var glory = Layer<LogoGlory>("Glory", logo);
        glory.SetAsFirstSibling();
        Set(glory.GetComponent<LogoGlory>(), "appearDelay", 1.5f); // once the logo has landed

        Ensure<PromptPulse>(prompt.gameObject);

        Layer<MagicMotes>("AmbientFx", canvas).SetSiblingIndex(bg.GetSiblingIndex() + 1);
        Layer<TapSparkle>("TapFx", canvas);
        start.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return string.Join(", ", canvas.Cast<Transform>().Select(t => t.name)) + " | bg: "
             + string.Join(", ", bg.Cast<Transform>().Select(t => t.name)) + " | logo: "
             + string.Join(", ", logo.Cast<Transform>().Select(t => t.name));
    }

    static RectTransform Layer<T>(string name, Transform parent) where T : Component
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.gameObject.AddComponent<T>();
        return rt;
    }

    // Re-adds the component so it picks up the current script defaults.
    static T Ensure<T>(GameObject go) where T : Component
    {
        var old = go.GetComponent<T>();
        if (old) Object.DestroyImmediate(old);
        return go.AddComponent<T>();
    }

    static void Set(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
