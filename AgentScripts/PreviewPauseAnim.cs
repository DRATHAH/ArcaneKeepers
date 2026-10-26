using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play Mode (any level with a PauseCanvas): opens the pause menu, saves frames of the entrance,
// then presses Continue and saves frames of the close. Unity must be in the foreground.
public static class PreviewPauseAnim
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var pause = Object.FindAnyObjectByType<PauseMenu>();
        if (!pause) return "no PauseMenu (enter Play Mode in a level)";
        var log = new System.Text.StringBuilder();

        // Overlay canvases only reach the RenderTexture once they've been drawn by the camera for a frame
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + 0.5f;
        }
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();

        pause.Pause();
        float start = Time.unscaledTime;
        foreach (float at in new[] { 0.05f, 0.12f, 0.2f, 0.3f, 0.45f, 0.8f })
        {
            while (Time.unscaledTime - start < at) await Awaitable.NextFrameAsync();
            log.AppendLine($"open {at:0.00} timeScale={Time.timeScale} {State()}");
            Shot($"{outDir}/open_{(int)(at * 100):000}.png");
        }

        pause.Continue();
        start = Time.unscaledTime;
        foreach (float at in new[] { 0.06f, 0.12f, 0.25f, 0.4f })
        {
            while (Time.unscaledTime - start < at) await Awaitable.NextFrameAsync();
            log.AppendLine($"close {at:0.00} paused={PauseMenu.IsPaused} timeScale={Time.timeScale} {State()}");
            Shot($"{outDir}/close_{(int)(at * 100):000}.png");
        }
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        File.WriteAllText(outDir + "/log.txt", log.ToString());
        return log.ToString();
    }

    static string State()
    {
        var safe = Object.FindObjectsByType<RectTransform>().FirstOrDefault(t => t.name == "Safe" && t.parent && t.parent.name == "PausePanel");
        if (!safe) return "(panel off)";
        return string.Join(" ", safe.Cast<RectTransform>().Select(t => $"{t.name.Replace("Button", "")}:{t.localScale.x:0.00}/{t.GetComponent<CanvasGroup>()?.alpha:0.00}"));
    }

    static void Shot(string file)
    {
        const int w = 1170, h = 540;
        var cam = Camera.main;
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
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.Destroy(rt);
    }
}
