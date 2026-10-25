using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Play-mode frames of the level select effects (enter Play Mode with LevelSelect open, Unity in the foreground,
// then run straight away so the entrance is caught).
// Usage: run_script AgentScripts/PreviewLevelSelectFx.cs PreviewLevelSelectFx.Run(outDir)
public static class PreviewLevelSelectFx
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

        // Replay the entrance from the start
        var tiles = Object.FindObjectsByType<LevelTile>(FindObjectsSortMode.None).OrderBy(t => t.transform.GetSiblingIndex()).ToArray();
        var startMethod = typeof(LevelSelectFx).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (Transform glow in tiles[0].transform.parent) if (glow.name == "GuideGlow") Object.Destroy(glow.gameObject);
        fx.StopAllCoroutines();
        fx.StartCoroutine((System.Collections.IEnumerator)startMethod.Invoke(fx, null));

        float[] at = { 0.12f, 0.22f, 0.32f, 0.45f, 0.7f, 1.4f };
        float last = 0;
        for (int i = 0; i < at.Length; i++)
        {
            await Seconds(at[i] - last);
            last = at[i];
            Shot(cam, rt, outDir, "0_enter_" + i);
        }

        fx.Burst(tiles[0].medal);
        await Seconds(0.08f); Shot(cam, rt, outDir, "1_burst_a");
        await Seconds(0.12f); Shot(cam, rt, outDir, "1_burst_b");
        await Seconds(0.6f);

        ExecuteEvents.Execute(tiles[3].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        await Seconds(0.05f); Shot(cam, rt, outDir, "2_locked_shake");
        await Seconds(2.5f); Shot(cam, rt, outDir, "3_idle");

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
