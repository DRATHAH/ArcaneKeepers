using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Play Mode, start in TowerSelect (Unity focused): squad of 4 -> DEPLOY -> loads `level`, then shots of the
// MOVE tab: dealing in, settled, armed (tap), cancelled (tap again), recharging. Run(outDir, "Level2")
public static class PreviewMoveButton
{
    public static async Task<string> Run(string outDir, string level)
    {
        var log = new StringBuilder();
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (screen)
        {
            for (int i = 0; i < 200 && screen.cards.Count == 0; i++) await Awaitable.NextFrameAsync();
            await Wait(0.8f);
            for (int i = 0; screen.Count < 4 && i < screen.cards.Count; i++) screen.Toggle(i);
            await Wait(0.6f);
            screen.Play();
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "LevelSelect"; i++) await Awaitable.NextFrameAsync();
            await Wait(0.3f);
        }
        if (SceneManager.GetActiveScene().name != level) SceneManager.LoadScene(level);
        for (int i = 0; i < 600 && Object.FindAnyObjectByType<MoveButton>() == null; i++) await Awaitable.NextFrameAsync();
        await Wait(0.55f);
        log.AppendLine(Shot(outDir + "/1_dealing.png"));
        await Wait(1.2f);
        log.AppendLine(Shot(outDir + "/2_ready.png"));

        var move = Object.FindAnyObjectByType<MoveButton>();
        var rm = RelocationManager.instance;
        Tap(move.gameObject);
        await Wait(0.3f);
        log.AppendLine($"after tap: canRelocate={rm.canRelocate} label='{move.label.text}'");
        log.AppendLine(Shot(outDir + "/3_armed.png"));
        Tap(move.gameObject);
        await Wait(0.3f);
        log.AppendLine($"after 2nd tap: canRelocate={rm.canRelocate} label='{move.label.text}'");
        log.AppendLine(Shot(outDir + "/4_cancelled.png"));

        rm.ClearObject(); // as after a move: full cooldown
        await Wait(0.15f);
        log.AppendLine($"cooldown: interactable={move.GetComponent<Button>().interactable} slider={move.cooldownSlider.value:0.00} shade={move.shade.fillAmount:0.00}");
        log.AppendLine(Shot(outDir + "/5_recharging.png"));
        await Wait(0.8f);
        log.AppendLine($"after cooldown: interactable={move.GetComponent<Button>().interactable} shade={move.shade.fillAmount:0.00}");
        return log.ToString();
    }

    static void Tap(GameObject go)
    {
        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
    }

    static string Shot(string file) => ShotNowLocal(file, 1920, 1080);

    static string ShotNowLocal(string file, int w, int h)
    {
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.05f + (100 - c.sortingOrder) * 0.001f; }
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
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
        System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.Destroy(rt);
        Object.Destroy(tex);
        return "shot " + file;
    }

    static async Task Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
    }
}
