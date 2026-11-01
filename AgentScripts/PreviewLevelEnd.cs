using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Play Mode in a level (DemoLevel): triggers the lose ("lose") or win ("win") finale and saves
// frames at the given times (seconds). Unity must be in the foreground while it runs.
//   run_script AgentScripts/PreviewLevelEnd.cs  Run(outDir, "lose", "0.2,0.6,1.2,2,2.4,3,3.6,5")
public static class PreviewLevelEnd
{
    public static async Task<string> Run(string outDir, string mode, string times)
    {
        Directory.CreateDirectory(outDir);
        var wlm = WinLossManager.instance;
        if (!wlm) return "no WinLossManager (enter Play Mode in a level)";
        var log = new System.Text.StringBuilder();
        var cam = Camera.main;

        // Overlay canvases don't reach a RenderTexture: draw them through the camera instead
        foreach (var c in Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
            ToCamera(c, cam);
        await Awaitable.NextFrameAsync();

        if (mode == "win")
        {
            var gm = GameManager.instance;
            if (gm)
            {
                gm.lastRewards.Clear();
                foreach (var loot in gm.towerLoot.Take(3))
                    gm.lastRewards.Add(new TowerEntry { tower = loot.tower, amount = Random.Range(3, 12) });
            }
            wlm.SetFocus(new Vector3(0, 1.25f, 0));
            wlm.winner = true;
        }
        else
        {
            var trigger = GameObject.Find("LossTrigger");
            wlm.SetFocus(trigger ? trigger.transform.position + new Vector3(1.5f, 0.5f, 2f) : Vector3.zero);
            wlm.loser = true;
        }

        LevelEndScreen screen = null;
        while (!screen) { await Awaitable.NextFrameAsync(); screen = Object.FindAnyObjectByType<LevelEndScreen>(); }
        ToCamera(screen.GetComponent<Canvas>(), cam);
        float start = Time.unscaledTime;

        foreach (float at in times.Split(',').Select(float.Parse))
        {
            while (Time.unscaledTime - start < at) await Awaitable.NextFrameAsync();
            log.AppendLine($"{mode} {at:0.00} timeScale={Time.timeScale:0.00} cam={cam.transform.position}");
            Shot($"{outDir}/{mode}_{(int)(at * 100):0000}.png");
        }
        File.WriteAllText(outDir + "/log_" + mode + ".txt", log.ToString());
        return log.ToString();
    }

    static void ToCamera(Canvas c, Camera cam)
    {
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = cam;
        c.planeDistance = cam.nearClipPlane + 0.5f;
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
        Object.Destroy(tex);
    }
}
