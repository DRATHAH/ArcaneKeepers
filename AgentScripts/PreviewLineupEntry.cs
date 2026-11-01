using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play Mode in TowerSelect: frames + card positions right after the scene starts, then with the first
// and last keeper focused. Unity must be focused DURING the call.
public static class PreviewLineupEntry
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var log = new StringBuilder();
        // Reload the scene so the entry frames really are the first ones after it opens
        UnityEngine.SceneManagement.SceneManager.LoadScene("TowerSelect");
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "enter Play Mode in TowerSelect";
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.5f; }
        // Scene time, so it matches what a player sees right after the transition
        float[] times = { 0.05f, 0.2f, 0.5f, 1.2f };
        foreach (float t in times)
        {
            while (Time.timeSinceLevelLoad < t) await Awaitable.NextFrameAsync();
            Shot(outDir + "/enter_" + t.ToString("0.00") + ".png");
            Dump(log, screen, "t=" + Time.timeSinceLevelLoad.ToString("0.00"));
        }
        screen.scroller.ScrollTo(0, true);
        await Wait(0.6f);
        Shot(outDir + "/first.png");
        Dump(log, screen, "first");
        screen.scroller.ScrollTo(2);
        await Wait(0.8f);
        Shot(outDir + "/third.png");
        Dump(log, screen, "third");
        screen.scroller.ScrollTo(screen.cards.Count - 1);
        await Wait(0.8f);
        Shot(outDir + "/last.png");
        Dump(log, screen, "last");
        screen.scroller.ScrollTo(3);
        await Wait(0.8f);
        Shot(outDir + "/fourth.png");
        Dump(log, screen, "fourth");
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;

        // Simulated finger swipes (overlay canvas, so screen pixels): from the first keeper, swipe left
        // slowly 5 times, then flick right once
        screen.scroller.ScrollTo(0, true);
        await Wait(0.3f);
        for (int i = 0; i < 5; i++)
        {
            await Swipe(screen.scroller, -260, 8);
            await Wait(0.5f);
            log.AppendLine("swipe left -> focus " + screen.scroller.FocusIndex);
        }
        await Swipe(screen.scroller, 900, 5);
        await Wait(0.7f);
        log.AppendLine("flick right -> focus " + screen.scroller.FocusIndex);
        return log.ToString();
    }

    static async Task Swipe(LineupScroller scroller, float pixels, int frames)
    {
        var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        Vector2 pos = new Vector2(Screen.width * 0.6f, Screen.height * 0.5f);
        ev.position = pos;
        scroller.OnBeginDrag(ev);
        for (int f = 0; f < frames; f++)
        {
            await Wait(0.016f); // phone-like frame pacing
            ev.delta = new Vector2(pixels / frames, 0);
            ev.position += ev.delta;
            scroller.OnDrag(ev);
        }
        scroller.OnEndDrag(ev);
    }

    static void Dump(StringBuilder log, CharacterSelectScreen screen, string label)
    {
        var sc = screen.scroller;
        var sr = sc.GetComponent<ScrollRect>();
        var content = sr.content;
        var vp = sr.viewport ? sr.viewport : (RectTransform)sc.transform;
        var hl = content.GetComponent<HorizontalLayoutGroup>();
        log.AppendLine($"[{label}] cards={screen.cards.Count} scrollerCards={sc.cards.Count} focus={sc.FocusIndex} contentX={content.anchoredPosition.x:0} contentW={content.rect.width:0} vpW={vp.rect.width:0} pad={hl.padding.left}/{hl.padding.right} children={content.childCount}");
        for (int i = 0; i < content.childCount; i++)
        {
            var t = (RectTransform)content.GetChild(i);
            var card = t.GetComponent<CharacterCard>();
            float x = vp.InverseTransformPoint(t.TransformPoint(t.rect.center)).x;
            log.AppendLine($"   {i} {t.name} active={t.gameObject.activeSelf} x={x:0} w={t.rect.width:0} fitScale={(card && card.fit ? card.fit.localScale.x : 0):0.00} alpha={(card ? card.group.alpha : 0):0.00} edge={(card ? card.edgeFade : 0):0.00}");
        }
    }

    static async Task Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
    }

    static void Shot(string file)
    {
        // Same shape as the game view, so the canvas doesn't change size (which re-centres the lineup)
        int h = 540, w = Mathf.RoundToInt(h * (float)Screen.width / Screen.height);
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
