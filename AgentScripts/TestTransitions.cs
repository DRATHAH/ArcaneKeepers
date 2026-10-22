using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Play mode: taps a button by name, logs the slide per frame until it finishes, and saves a
// mid-slide picture (both screen textures composited at their current offsets) to outFile
public static class TestTransitions
{
    public static async Task<string> Tap(string buttonName, string outFile)
    {
        var log = new StringBuilder();
        var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.name == buttonName);
        if (!button) return "no button " + buttonName + " in " + SceneManager.GetActiveScene().name;
        string from = SceneManager.GetActiveScene().name;
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        float start = Time.realtimeSinceStartup;
        bool seen = false, saved = false;
        while (Time.realtimeSinceStartup - start < 8)
        {
            await Awaitable.NextFrameAsync();
            var go = GameObject.Find("SceneTransition");
            var canvas = go ? go.GetComponent<Canvas>() : null;
            if (canvas && canvas.enabled)
            {
                seen = true;
                var o = go.transform.Find("OldScreen").GetComponent<RawImage>();
                var n = go.transform.Find("NewScreen").GetComponent<RawImage>();
                float w = ((RectTransform)go.transform).rect.width;
                log.Append($"{Time.realtimeSinceStartup - start:0.00}s {SceneManager.GetActiveScene().name} old.x={o.rectTransform.anchoredPosition.x / w:0.00} new.x={(n.enabled ? (n.rectTransform.anchoredPosition.x / w).ToString("0.00") : "-")} top={go.transform.GetChild(1).name}\n");
                if (!saved && n.enabled && n.texture && Mathf.Abs(n.rectTransform.anchoredPosition.x / w) is > 0.2f and < 0.6f)
                {
                    saved = true;
                    Save(o, n, w, go.transform.GetChild(1).name, outFile);
                    log.Insert(0, "old " + Stats(o.texture) + " | new " + Stats(n.texture) + "\n");
                }
            }
            else if (seen)
            {
                break;
            }
        }
        return $"{from} -> {SceneManager.GetActiveScene().name} in {Time.realtimeSinceStartup - start:0.00}s saved={saved}\n" + log;
    }

    // Average brightness and share of pure-white pixels, to catch blank captures
    static string Stats(Texture t)
    {
        if (!t) return "none";
        var rt = RenderTexture.GetTemporary(128, 72);
        Graphics.Blit(t, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(128, 72, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 128, 72), 0, 0);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        float avg = px.Average(c => c.grayscale);
        float white = px.Count(c => c.r > 0.97f && c.g > 0.97f && c.b > 0.97f) / (float)px.Length;
        return $"avg={avg:0.00} white={white:P0}";
    }

    static void Save(RawImage o, RawImage n, float w, string top, string outFile)
    {
        int width = 960, height = Mathf.RoundToInt(960f * Screen.height / Screen.width);
        var rt = RenderTexture.GetTemporary(width, height);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, width, height, 0);
        void Draw(RawImage img)
        {
            float x = img.rectTransform.anchoredPosition.x / w * width;
            Graphics.DrawTexture(new Rect(x, 0, width, height), img.texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, img.color * 0.5f);
        }
        if (top == "NewScreen") { Draw(o); Draw(n); } else { Draw(n); Draw(o); }
        GL.PopMatrix();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
    }
}
