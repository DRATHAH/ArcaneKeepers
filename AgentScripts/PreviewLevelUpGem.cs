using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Play Mode in TowerSelect: focuses a ready keeper, checks a tap on the arrow gem's outer edge reaches the card
// bar's Button, then taps it and grabs a dense run of close-up frames of the level-up. Unity must be focused.
public static class PreviewLevelUpGem
{
    public static async Task<string> Run(string outDir, string keeperName)
    {
        Directory.CreateDirectory(outDir);
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "enter Play Mode in TowerSelect";
        for (int i = 0; i < 200 && screen.cards.Count == 0; i++) await Awaitable.NextFrameAsync();
        await Wait(0.5f);
        int keeper = screen.cards.FindIndex(c => c.tower.tower.towerName == keeperName);
        screen.scroller.ScrollTo(keeper, true);
        await Wait(1.2f);
        var info = screen.intelInfo;
        var bar = info.progress;
        string log = "keeper=" + keeper + " ready=" + bar.Ready + "\n";

        // Hit test the gem's right edge (outside the bar's own rect) while still in overlay mode
        var gemCorners = new Vector3[4];
        bar.upGem.GetWorldCorners(gemCorners);
        Vector2 edge = new Vector2(gemCorners[2].x - 8, (gemCorners[0].y + gemCorners[2].y) / 2);
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = edge }, hits);
        GameObject handler = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) : null;
        log += "gem edge hit=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") + " clickHandler=" + (handler ? handler.name : "none") + "\n";

        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.5f; }
        Shot(outDir + "/00_idle.png");
        // Hold (squash), then release + click through the real handler chain
        if (handler) ExecuteEvents.Execute(handler, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
        await Wait(0.15f);
        Shot(outDir + "/01_held.png");
        if (handler)
        {
            ExecuteEvents.Execute(handler, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(handler, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
        float start = Time.unscaledTime;
        float[] times = { 0.06f, 0.12f, 0.2f, 0.28f, 0.38f, 0.5f, 0.65f, 0.9f, 1.3f };
        for (int i = 0; i < times.Length; i++)
        {
            while (Time.unscaledTime - start < times[i]) await Awaitable.NextFrameAsync();
            Shot(outDir + "/" + (i + 2).ToString("00") + "_t" + Mathf.RoundToInt(times[i] * 100) + ".png");
        }
        log += "after: lvl=" + bar.levelText.text + " ready=" + bar.Ready + " hint=" + info.cardsHint.text;
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
        // Bar strip only
        var tex = new Texture2D(560, 200, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(70, h - 400, 560, 200), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = old;
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.Destroy(rt);
    }
}
