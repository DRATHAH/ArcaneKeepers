using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Play Mode in TowerSelect: shows the Archer (ready to level up), taps the card bar and grabs frames of
// the level-up. Crops to the name block. Unity must be focused DURING the call.
public static class PreviewLevelUpBar
{
    public static async Task<string> Run(string outDir, int keeper)
    {
        Directory.CreateDirectory(outDir);
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "enter Play Mode in TowerSelect";
        for (int i = 0; i < 200 && screen.cards.Count == 0; i++) await Awaitable.NextFrameAsync();
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.5f; }
        screen.scroller.ScrollTo(keeper, true);
        await Wait(1.2f);
        var info = screen.intelInfo;
        string log = "ready=" + info.progress.Ready + " interactable=" + info.upgradeButton.interactable + "\n";
        Shot(outDir + "/0_ready.png");
        await Wait(0.3f);
        Shot(outDir + "/1_ready.png");
        info.upgradeButton.onClick.Invoke();
        float[] times = { 0.08f, 0.15f, 0.25f, 0.6f };
        for (int i = 0; i < times.Length; i++) { await Wait(times[i]); Shot(outDir + "/" + (i + 2) + "_tap.png"); }
        log += "after: lvl=" + info.progress.levelText.text + " ready=" + info.progress.Ready + " hint=" + info.cardsHint.text;
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        return log;
    }

    static async Task Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
    }

    static void Shot(string file)
    {
        const int w = 1950, h = 900;
        var cam = Camera.main;
        var rt = new RenderTexture(w, h, 24);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        // Left column only, rows 120..660 from the top
        var tex = new Texture2D(700, 560, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(60, h - 680, 700, 560), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = old;
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.Destroy(rt);
    }
}
