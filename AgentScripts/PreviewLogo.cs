using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders still frames of the MainMenu LogoIntro slam effect (edit mode, changes discarded).
public static class PreviewLogo
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

    // Frames: 0 = mid-drop, 1 = impact (flash + ring + burst + squash), 2 = settled with shine.
    public static string Run(string outDir)
    {
        const string path = "Assets/Scenes/MainMenu.unity";
        float[] scale = { 1.9f, 1f, 1f };
        float[] tilt = { -6f, 0f, 0f };
        Vector2[] squash = { Vector2.one, new Vector2(1.13f, 0.87f), Vector2.one };
        float[] alpha = { 0.6f, 1f, 1f };
        float[] flashA = { 0f, 0.6f, 0f };
        float[] ringA = { 0f, 0.7f, 0f };
        float[] gleamPos = { -2f, -2f, 0.1f };
        for (int f = 0; f < 3; f++)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            var li = Object.FindAnyObjectByType<LogoIntro>();
            typeof(LogoIntro).GetMethod("BuildMagicLayers", F).Invoke(li, null);
            var title = (RectTransform)typeof(LogoIntro).GetField("title", F).GetValue(li);
            var ring = (Image)typeof(LogoIntro).GetField("ring", F).GetValue(li);
            var flash = (Image)typeof(LogoIntro).GetField("flash", F).GetValue(li);
            var gleam = (RectTransform)typeof(LogoIntro).GetField("gleam", F).GetValue(li);
            var layer = (RectTransform)typeof(LogoIntro).GetField("sparkleLayer", F).GetValue(li);
            var spark = (Sprite)typeof(LogoIntro).GetField("sparkleSprite", F).GetValue(null);
            var ringColor = (Color)typeof(LogoIntro).GetField("ringColor", F).GetValue(li);
            var sparkColor = (Color)typeof(LogoIntro).GetField("sparkleColor", F).GetValue(li);

            title.GetComponent<CanvasGroup>().alpha = alpha[f];
            title.localScale = new Vector3(scale[f] * squash[f].x, scale[f] * squash[f].y, 1);
            title.localRotation = Quaternion.Euler(0, 0, tilt[f]);
            flash.color = new Color(1, 1, 1, flashA[f]);
            ringColor.a = ringA[f]; ring.color = ringColor; ring.rectTransform.localScale = Vector3.one * 1.1f;
            Canvas.ForceUpdateCanvases();
            Rect r = title.rect;
            gleam.sizeDelta = new Vector2(r.height * 0.45f, r.height * 1.8f);
            gleam.anchoredPosition = new Vector2(gleamPos[f] * r.width, 0);

            Random.InitState(7 + f);
            int n = f == 1 ? 14 : f == 2 ? 3 : 0;
            Rect lr = layer.rect;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("S", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform; rt.SetParent(layer, false);
                var img = go.GetComponent<Image>(); img.sprite = spark;
                float a = i * Mathf.PI * 2f / n;
                rt.anchoredPosition = f == 1
                    ? new Vector2(Mathf.Cos(a) * lr.width * 0.6f, Mathf.Sin(a) * lr.height * 0.75f) * 0.75f
                    : new Vector2(Random.Range(lr.xMin, lr.xMax), Random.Range(lr.yMin, lr.yMax));
                float size = Mathf.Min(lr.width, lr.height) * Random.Range(0.14f, 0.26f);
                rt.sizeDelta = new Vector2(size, size);
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-45f, 45f));
                img.color = sparkColor;
            }
            Render(scene, 1170, 540, outDir + "/logo_" + f + ".png");
            // Undo the temporary changes in place (scene has unsaved user edits, so don't reload it)
            title.localScale = Vector3.one;
            title.localRotation = Quaternion.identity;
            Object.DestroyImmediate(ring.gameObject);
            Object.DestroyImmediate(layer.gameObject);
            Object.DestroyImmediate(gleam.gameObject);
            Object.DestroyImmediate(flash.gameObject);
            Object.DestroyImmediate(title.GetComponent<Mask>());
            Object.DestroyImmediate(title.GetComponent<CanvasGroup>());
        }
        return "ok";
    }

    static void Render(UnityEngine.SceneManagement.Scene scene, int width, int height, string outFile)
    {
        var rtex = new RenderTexture(width, height, 24);
        var cam = new GameObject("__RenderCam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.orthographic = true; cam.targetTexture = rtex; cam.transform.position = new Vector3(0, 0, -1000);
        var canvases = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(false)).Where(c => c.isRootCanvas).ToList();
        var modes = canvases.Select(c => (c.renderMode, c.worldCamera, c.planeDistance)).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 10; }
        RenderTexture.active = rtex;
        for (int i = 0; i < 3; i++)
        {
            foreach (var c in canvases)
            {
                var sc = c.GetComponent<CanvasScaler>(); if (sc) { sc.enabled = false; sc.enabled = true; }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate();
        }
        cam.Render();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        for (int i = 0; i < canvases.Count; i++) { canvases[i].renderMode = modes[i].Item1; canvases[i].worldCamera = modes[i].Item2; canvases[i].planeDistance = modes[i].Item3; }
        Object.DestroyImmediate(cam.gameObject);
    }
}
