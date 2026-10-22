using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Play-mode helpers for the character select screen: tap keepers / slots and render the canvas to a PNG.
// Run with run_script while the TowerSelect scene is playing (Unity must be foreground for frames to tick).
public static class PreviewCharacterSelect
{
    static CharacterSelectScreen Screen => Object.FindAnyObjectByType<CharacterSelectScreen>();

    public static string Tap(int index)
    {
        Screen.Toggle(index);
        return "tapped " + index + " count=" + Screen.Count;
    }

    public static string TapSlot(int slot)
    {
        Screen.RemoveFromSlot(slot);
        return "removed slot " + slot + " count=" + Screen.Count;
    }

    public static string State()
    {
        var s = Screen;
        return "count=" + s.Count + " play=" + s.playButton.interactable + " counter=" + s.counterText.text + " hint=" + s.hintText.text
            + " focus=" + s.scroller.FocusIndex + " intel=" + s.intelName.text
            + " pm=" + (PlayerManager.instance ? string.Join(",", PlayerManager.instance.equippedTowers.Select(t => t.tower ? t.tower.name : "null")) : "none");
    }

    // Waits `delayMs` of play time, then renders at width x height
    public static async Task<string> Shot(string outFile, int width, int height, int delayMs)
    {
        float until = Time.unscaledTime + delayMs / 1000f;
        while (Time.unscaledTime < until)
        {
            await Awaitable.NextFrameAsync();
        }

        Canvas canvas = Screen.GetComponent<Canvas>();
        var rt = new RenderTexture(width, height, 24);
        var camGo = new GameObject("__RenderCam");
        var cam = camGo.AddComponent<Camera>();
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
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();

        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.Destroy(camGo);
        rt.Release();
        return outFile;
    }
}
