using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Edit mode: open a level scene and render it with its overlay canvases through the main camera.
// run_script AgentScripts/ShotLevel.cs Run("Assets/Scenes/DemoLevel.unity", "C:/.../out.png", 1920, 1080)
// Info(scenes) lists root objects, grid cell sizes and RelocationManager refs.
public static class ShotLevel
{
    public static string Run(string scenePath, string file, int w, int h)
    {
        if (!string.IsNullOrEmpty(scenePath) && EditorSceneManager.GetActiveScene().path != scenePath)
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return Shot(file, w, h);
    }

    public static string Shot(string file, int w, int h)
    {
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.05f + (100 - c.sortingOrder) * 0.001f; }
        var rt = new RenderTexture(w, h, 24);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = old;
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        return "ok " + file;
    }

    public static string Info(string scenes)
    {
        var sb = new StringBuilder();
        foreach (var path in scenes.Split(','))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("=== " + path);
            foreach (var r in scene.GetRootGameObjects())
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(r);
                sb.AppendLine("  root " + r.name + (r.activeSelf ? "" : " [off]") + (src ? " <" + AssetDatabase.GetAssetPath(src) + ">" : ""));
            }
            foreach (var g in Object.FindObjectsByType<GridLayoutGroup>(FindObjectsInactive.Include))
                sb.AppendLine($"  grid {g.name} cell={g.cellSize} spacing={g.spacing} pad={g.padding} align={g.childAlignment}");
            foreach (var m in Object.FindObjectsByType<RelocationManager>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(m);
                sb.AppendLine($"  RelocationManager on {m.name}: slider={so.FindProperty("cooldownSlider").objectReferenceValue} button={so.FindProperty("relocateButton").objectReferenceValue} cd={so.FindProperty("maxRelocateCooldown").floatValue}");
            }
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Where(b => b.name == "RelocateButton"))
            {
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    sb.AppendLine($"  RelocateButton onClick: {b.onClick.GetPersistentTarget(i)}.{b.onClick.GetPersistentMethodName(i)}");
            }
            var cam = Camera.main;
            if (cam) sb.AppendLine($"  cam pos={cam.transform.position} rot={cam.transform.eulerAngles} ortho={cam.orthographic} size={cam.orthographicSize} fov={cam.fieldOfView}");
        }
        return sb.ToString();
    }
}
