using System.IO;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders menu scenes at common phone/tablet shapes with a simulated notch + home bar,
// applying each SafeArea the way it would on device (symmetric insets)
public static class RenderDevices
{
    // name, width, height, left, right, top, bottom insets in pixels
    static readonly (string tag, int w, int h, int l, int r, int t, int b)[] Devices =
    {
        ("iphone15", 2556, 1179, 177, 177, 0, 63),
        ("android-punch", 2400, 1080, 110, 0, 0, 0),
        ("galaxyFold-inner", 2176, 1812, 0, 0, 0, 0),
        ("16x9", 1920, 1080, 0, 0, 0, 0),
        ("ipad", 2048, 1536, 0, 0, 0, 40),
        ("ultrawide21x9", 2520, 1080, 120, 0, 0, 0),
    };

    public static string All(string outDir, string scenesCsv)
    {
        foreach (string s in scenesCsv.Split(','))
            foreach (var d in Devices)
                Run("Assets/Scenes/" + s + ".unity", d, outDir + "/" + Path.GetFileNameWithoutExtension(s) + "_" + d.tag + ".png");
        return "ok";
    }

    static void Run(string scenePath, (string tag, int w, int h, int l, int r, int t, int b) d, string outFile)
    {
        int width = d.w / 2, height = d.h / 2;
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var rt = new RenderTexture(width, height, 24);
        var camGo = new GameObject("__RenderCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(0, 0, -1000);

        var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas).ToList();
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 10;
        }
        // Symmetric safe area, like SafeArea does on device
        float x = Mathf.Max(d.l, d.r) / (float)d.w, y = Mathf.Max(d.t, d.b) / (float)d.h;
        foreach (var sa in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SafeArea>(true)))
        {
            var rect = (RectTransform)sa.transform;
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(1 - x, 1 - y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        RenderTexture.active = rt;
        for (int i = 0; i < 4; i++)
        {
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler) { scaler.enabled = false; scaler.enabled = true; }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var f in Object.FindObjectsByType<ScaleToFit>()) { f.enabled = false; f.enabled = true; }
            foreach (var f in Object.FindObjectsByType<CharacterSelectLayout>()) { f.enabled = false; f.enabled = true; }
            foreach (var t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate();
        }
        cam.Render();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        // Mark the notch so it's visible in the preview
        var red = Color.red;
        for (int px = 0; px < d.l / 2; px++) for (int py = height / 2 - 60; py < height / 2 + 60; py++) tex.SetPixel(px, py, red);
        for (int px = width - d.r / 2; px < width; px++) for (int py = height / 2 - 60; py < height / 2 + 60; py++) tex.SetPixel(px, py, red);
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }

}
