using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play-mode frames of the character select effects (TowerSelect playing, Unity in the foreground).
// The canvas is switched to a render camera FIRST, so effects spawn at the right spot in the shots.
// Usage: run_script AgentScripts/PreviewCharacterSelectFx.cs PreviewCharacterSelectFx.Run(outDir)
public static class PreviewCharacterSelectFx
{
    public static async Task<string> Run(string outDir)
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

        // Start from an empty squad
        for (int s = 0; s < CharacterSelectScreen.SquadSize; s++) screen.RemoveFromSlot(s);
        await Seconds(1.6f);
        Shot(cam, rt, outDir, "0_idle");

        int focus = screen.scroller.FocusIndex;
        screen.Toggle(focus);
        await Seconds(0.12f); Shot(cam, rt, outDir, "1_recruit");
        await Seconds(0.12f); Shot(cam, rt, outDir, "2_flight");
        await Seconds(0.14f); Shot(cam, rt, outDir, "3_land");
        await Seconds(0.6f);

        screen.RemoveFromSlot(0);
        await Seconds(0.1f); Shot(cam, rt, outDir, "4_puff");
        await Seconds(0.6f);

        int added = 0;
        for (int i = 0; i < screen.characters.Length && added < 4; i++, added++) screen.Toggle(i);
        await Seconds(0.48f); Shot(cam, rt, outDir, "5_deploy_burst");
        await Seconds(1.6f); Shot(cam, rt, outDir, "6_ready_idle");

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
