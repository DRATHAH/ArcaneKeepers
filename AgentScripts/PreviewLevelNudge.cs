using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Play-mode frames of the level select effects (enter Play Mode with LevelSelect open, Unity in the foreground,
// then run straight away so the entrance is caught).
// Usage: run_script AgentScripts/PreviewLevelNudge.cs PreviewLevelNudge.Run(outDir)
public static class PreviewLevelNudge
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var fx = Object.FindAnyObjectByType<LevelSelectFx>();
        Canvas canvas = fx.GetComponentInParent<Canvas>().rootCanvas;
        var rt = new RenderTexture(1170, 540, 24);
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

        // Wait for the guide tile to be idle, then force a nudge and capture it
        var guide = Object.FindObjectsByType<LevelTile>(FindObjectsSortMode.None).First(t => t.guide);
        var f = typeof(LevelTile).GetField("nextNudge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await Seconds(2.5f);
        Shot(cam, rt, outDir, "0_rest");
        f.SetValue(guide, 0f);
        float[] at = { 0.03f, 0.08f, 0.15f, 0.25f, 0.4f, 0.6f, 0.9f };
        float last = 0;
        string log = "";
        for (int i = 0; i < at.Length; i++)
        {
            await Seconds(at[i] - last);
            last = at[i];
            log += i + " scale=" + guide.transform.localScale + "|";
            Shot(cam, rt, outDir, "1_nudge_" + i);
        }
        File.WriteAllText(Path.Combine(outDir, "log.txt"), log);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.Destroy(cam.gameObject);
        rt.Release();
        return outDir;
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
