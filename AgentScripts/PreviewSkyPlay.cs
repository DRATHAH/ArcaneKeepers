using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play-mode frames of the TowerSelect background (clouds, birds, river glints) over a few seconds.
// TowerSelect must be playing with Unity in the foreground.
// Usage: run_script AgentScripts/PreviewSkyPlay.cs PreviewSkyPlay.Run(outDir, frames, gap)
public static class PreviewSkyPlay
{
    public static async Task<string> Run(string outDir, int frames, float gap)
    {
        Directory.CreateDirectory(outDir);
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        Canvas canvas = screen.GetComponent<Canvas>();
        var rt = new RenderTexture(1920, 1080, 24);
        var cam = new GameObject("__RenderCam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(0, 0, -1000);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.enabled = false;
        scaler.enabled = true;

        string log = "";
        for (int i = 0; i < frames; i++)
        {
            await Seconds(gap);
            Shot(cam, rt, outDir, "play_" + i);
            var sky = Object.FindAnyObjectByType<SkyDrift>();
            if (sky)
            {
                log += i + ":";
                foreach (Transform c in sky.transform)
                    if (c.name == "Cloud") log += " " + ((RectTransform)c).anchoredPosition.ToString("F0");
                log += "\n";
            }
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.Destroy(cam.gameObject);
        rt.Release();
        return log;
    }

    static async Task Seconds(float s)
    {
        float until = Time.unscaledTime + s;
        while (Time.unscaledTime < until) await Awaitable.NextFrameAsync();
    }

    static void Shot(Camera cam, RenderTexture rt, string dir, string name)
    {
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.Destroy(tex);
    }
}
