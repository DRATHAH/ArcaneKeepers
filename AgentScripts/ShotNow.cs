using System.IO;
using System.Linq;
using UnityEngine;

// Play Mode: one shot of the current screen (canvases rendered through the main camera). Run(file, w, h).
public static class ShotNow
{
    public static string Run(string file, int w, int h)
    {
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.5f; }
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
        Object.Destroy(rt);
        return "ok " + Screen.width + "x" + Screen.height;
    }
}
