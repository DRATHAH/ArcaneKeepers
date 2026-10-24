using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play Mode check of the MainMenu effects (enter Play Mode on MainMenu, Unity in the foreground).
// Waits for the intro, then triggers each effect and renders frames to outDir/menufx_*.png.
// Usage: run_script AgentScripts/PreviewMenuFx.cs PreviewMenuFx.Run(outDir)  (timeout ~20s)
public static class PreviewMenuFx
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<string> Run(string outDir)
    {
        if (!Application.isPlaying) return "enter Play Mode first";
        await Seconds(3.5f);
        Shot(outDir + "/menufx_idle.png");

        var star = Object.FindAnyObjectByType<ShootingStars>();
        typeof(ShootingStars).GetField("timer", F).SetValue(star, 0f);
        var tap = Object.FindAnyObjectByType<TapSparkle>();
        var layer = (RectTransform)tap.transform;
        tap.Burst(new Vector2(layer.rect.width * 0.5f, layer.rect.height * 0.3f));
        await Seconds(0.15f);
        Shot(outDir + "/menufx_burst.png");
        await Seconds(0.35f);
        Shot(outDir + "/menufx_comet.png");
        return "ok";
    }

    static async Task Seconds(float s)
    {
        float end = Time.unscaledTime + s;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
    }

    static void Shot(string file)
    {
        const int w = 1170, h = 540;
        var rtex = new RenderTexture(w, h, 24);
        var cam = new GameObject("__ShotCam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.orthographic = true; cam.targetTexture = rtex; cam.transform.position = new Vector3(0, 0, -1000);
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 10; }
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rtex;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(file, tex.EncodeToPNG());
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        Object.Destroy(cam.gameObject);
        rtex.Release();
    }
}
