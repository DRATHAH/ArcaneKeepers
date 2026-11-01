using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Play Mode in TowerSelect: shots of the info block under the name for the Archer, an upgrade (mid-burst
// and settled), then every other keeper. Writes log.txt. Unity must be focused DURING the call.
public static class PreviewTowerInfo
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
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + 0.5f;
        }

        var info = screen.intelInfo;
        // PlayerManager loads the saved towers a moment after the first cards spawn; wait for the rebuild
        await Wait(1.5f);
        int archer = screen.cards.FindIndex(c => c.tower.tower.name == "ArcherTower");
        screen.scroller.ScrollTo(archer, true);
        await Wait(0.6f);
        log.AppendLine("cards=" + screen.cards.Count + " archer=" + archer + " " + Entry(screen.cards[archer].tower) + " upgradeShown=" + info.upgradeButton.gameObject.activeSelf + " power=" + info.powerValue.text);
        Shot(outDir + "/1_archer.png");

        info.upgradeButton.onClick.Invoke();
        await Wait(0.12f);
        Shot(outDir + "/2_upgrade_burst.png");
        await Wait(0.9f);
        Shot(outDir + "/3_upgraded.png");
        log.AppendLine("after upgrade " + Entry(screen.cards[archer].tower) + " power=" + info.powerValue.text + " upgradeShown=" + info.upgradeButton.gameObject.activeSelf + " hint=" + info.cardsHint.text);

        for (int i = 0; i < screen.cards.Count; i++)
        {
            if (i == archer) continue;
            screen.scroller.ScrollTo(i, true);
            await Wait(0.5f);
            Shot(outDir + "/4_" + screen.cards[i].tower.tower.name + ".png");
            log.AppendLine(Entry(screen.cards[i].tower) + " " + info.powerLabel.text + "=" + info.powerValue.text + " range=" + info.rangeValue.text + " hint=" + info.cardsHint.text);
        }

        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        File.WriteAllText(outDir + "/log.txt", log.ToString());
        return log.ToString();
    }

    static string Entry(TowerEntry e) => $"{e.tower.name} lvl{e.level} x{e.amount}";

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
