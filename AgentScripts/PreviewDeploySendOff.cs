using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play mode, TowerSelect open, Unity in the foreground: fills the squad, taps DEPLOY and saves frames of
// the send-off (button spring + squad hop) up to the moment the scene change freezes the screen.
// Usage: run_script AgentScripts/PreviewDeploySendOff.cs PreviewDeploySendOff.Run(outDir)
// (it really leaves for LevelSelect afterwards; frames only show the character select part)
public static class PreviewDeploySendOff
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "no CharacterSelectScreen";
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

        for (int i = 0; i < screen.cards.Count && screen.Count < CharacterSelectScreen.SquadSize; i++) screen.Toggle(i);
        await Seconds(1.6f);
        Shot(cam, rt, outDir, "0_ready");

        var log = new StringBuilder();
        float start = Time.unscaledTime;
        screen.Play();
        float[] at = { 0.05f, 0.12f, 0.2f, 0.28f, 0.38f };
        int next = 0;
        while (Time.unscaledTime - start < 0.41f)
        {
            await Awaitable.NextFrameAsync();
            float t = Time.unscaledTime - start;
            log.Append($"{t:0.000} btn={screen.playButton.transform.localScale.x:0.000}");
            foreach (var s in screen.slots) log.Append($" {s.content.anchoredPosition.y:0}");
            log.Append('\n');
            if (next < at.Length && t >= at[next])
            {
                Shot(cam, rt, outDir, $"{next + 1}_{at[next]:0.00}s");
                next++;
            }
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.Destroy(cam.gameObject);
        rt.Release();
        return log.ToString();
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
