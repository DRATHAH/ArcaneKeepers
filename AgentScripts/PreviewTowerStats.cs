using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Play Mode in TowerSelect: one shot of the info block per keeper (no upgrade). Unity must be focused DURING the call.
public static class PreviewTowerStats
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var log = new StringBuilder();
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "enter Play Mode in TowerSelect";
        for (int i = 0; i < 200 && screen.cards.Count == 0; i++) await Awaitable.NextFrameAsync();
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.5f; }
        await Wait(1.5f);
        var info = screen.intelInfo;
        for (int i = 0; i < screen.cards.Count; i++)
        {
            screen.scroller.ScrollTo(i, true);
            await Wait(0.5f);
            Shot(outDir + "/" + i + "_" + screen.cards[i].tower.tower.name + ".png");
            log.AppendLine(screen.cards[i].tower.tower.name + " hp=" + info.healthValue.text + " " + info.powerLabel.text + "=" + info.powerValue.text + " range=" + info.rangeValue.text);
        }
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        return log.ToString();
    }

    static async Task Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
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
