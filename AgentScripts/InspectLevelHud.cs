using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Dumps every Canvas (+ UI children, 3 levels deep) in the given scenes, plus RelocateButton details.
// run_script AgentScripts/InspectLevelHud.cs Run("Assets/Scenes/DemoLevel.unity,...")
public static class InspectLevelHud
{
    public static string Run(string scenes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BUILD: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));
        foreach (var path in scenes.Split(','))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("=== " + path);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas))
                {
                    var scaler = c.GetComponent<CanvasScaler>();
                    var pr = PrefabUtility.GetNearestPrefabInstanceRoot(c.gameObject);
                    sb.AppendLine($"Canvas {Path(c.transform)} mode={c.renderMode} order={c.sortingOrder} ref={(scaler ? scaler.referenceResolution.ToString() : "-")} match={(scaler ? scaler.matchWidthOrHeight : -1)} prefab={(pr ? AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(pr)) : "none")}");
                    Dump(c.transform, 1, 4, sb);
                }
            }
        }
        return sb.ToString();
    }

    static void Dump(Transform t, int depth, int max, StringBuilder sb)
    {
        if (depth > max) return;
        foreach (Transform ch in t)
        {
            var rt = ch as RectTransform;
            string comps = string.Join(",", ch.GetComponents<Component>().Where(x => x && !(x is Transform) && !(x is CanvasRenderer)).Select(x => x.GetType().Name));
            string r = rt ? $" a={rt.anchorMin}-{rt.anchorMax} p={rt.anchoredPosition} s={rt.sizeDelta} sc={rt.localScale.x:0.##}" : "";
            var img = ch.GetComponent<Image>();
            string spr = img && img.sprite ? " spr=" + img.sprite.name : "";
            sb.AppendLine(new string(' ', depth * 2) + (ch.gameObject.activeSelf ? "" : "[off] ") + ch.name + r + spr + " [" + comps + "]");
            Dump(ch, depth + 1, max, sb);
        }
    }

    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
}
