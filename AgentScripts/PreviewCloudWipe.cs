using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// Edit mode: renders the CloudWipe over the open MainMenu at a few cover amounts (no Play Mode needed)
public static class PreviewCloudWipe
{
    public static string Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        int w = 1280, h = 590;
        var rt = new RenderTexture(w, h, 24);
        var camGo = new GameObject("PreviewCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        camGo.transform.position = new Vector3(0, 0, -100);

        // Scene canvases into this camera for the render
        var moved = new System.Collections.Generic.List<Canvas>();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 50; moved.Add(c);
            }
        }

        var go = new GameObject("SceneTransition");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10;
        var wipe = go.AddComponent<CloudWipe>();
        Canvas.ForceUpdateCanvases();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CloudWipe).GetMethod("Build", flags).Invoke(wipe, null);
        var setCover = typeof(CloudWipe).GetMethod("SetCover", flags);
        string result = "";
        foreach (float cover in new[] { 0.3f, 0.6f, 0.85f, 1f })
        {
            setCover.Invoke(wipe, new object[] { cover });
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string file = Path.Combine(outDir, $"clouds_{cover:0.00}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            // How much of the screen is cloud-white (cover 1 must hide everything)
            int bright = 0; var px = tex.GetPixels32();
            foreach (var p in px) if (p.r > 170 && p.g > 190 && p.b > 220) bright++;
            result += $"{cover:0.00}: cloud {bright * 100f / px.Length:0}%  {file}\n";
            Object.DestroyImmediate(tex);
        }
        foreach (var c in moved) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(camGo);
        rt.Release();
        return EditorSceneManager.GetActiveScene().name + "\n" + result;
    }
}
