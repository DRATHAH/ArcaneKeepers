using System.IO;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders DemoLevel (game camera + canvases) with the pause panel open, or closed
public static class PreviewPause
{
    public static string Run(string outDir)
    {
        Render(true, 1170, 540, outDir + "/pause_phone.png");
        Render(true, 1024, 768, outDir + "/pause_tablet.png");
        Render(false, 1170, 540, outDir + "/pause_hud.png");
        return "ok";
    }

    static void Render(bool open, int width, int height, string outFile)
    {
        const string path = "Assets/Scenes/DemoLevel.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var rt = new RenderTexture(width, height, 24);
        var cam = Camera.main;
        cam.targetTexture = rt;
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
        all.First(t => t.name == "PausePanel").gameObject.SetActive(open);
        all.First(t => t.name == "PauseButton").gameObject.SetActive(!open);
        var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(false)).Where(c => c.isRootCanvas).ToList();
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + 0.5f;
        }
        RenderTexture.active = rt;
        for (int i = 0; i < 3; i++)
        {
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler) { scaler.enabled = false; scaler.enabled = true; }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate();
        }
        cam.Render();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}
