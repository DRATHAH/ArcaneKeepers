using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders still frames of the MainMenu LogoIntro magic effect (edit mode, changes discarded).
public static class PreviewLogo
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

    public static string Run(string outDir)
    {
        const string path = "Assets/Scenes/MainMenu.unity";
        float[] gleamPos = { -2f, 0.0f, 0.35f };
        float[] titleAlpha = { 0.55f, 1f, 1f };
        float[] titleScale = { 1.07f, 1f, 1f };
        float[] auraAlpha = { 1f, 0.6f, 0.65f };
        for (int f = 0; f < 3; f++)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            var li = Object.FindAnyObjectByType<LogoIntro>();
            typeof(LogoIntro).GetMethod("BuildMagicLayers", F).Invoke(li, null);
            var title = (RectTransform)typeof(LogoIntro).GetField("title", F).GetValue(li);
            var aura = (Image)typeof(LogoIntro).GetField("aura", F).GetValue(li);
            var gleam = (RectTransform)typeof(LogoIntro).GetField("gleam", F).GetValue(li);
            var layer = (RectTransform)typeof(LogoIntro).GetField("sparkleLayer", F).GetValue(li);
            var spark = (Sprite)typeof(LogoIntro).GetField("sparkleSprite", F).GetValue(null);
            var auraColor = (Color)typeof(LogoIntro).GetField("auraColor", F).GetValue(li);
            var sparkColor = (Color)typeof(LogoIntro).GetField("sparkleColor", F).GetValue(li);

            title.GetComponent<CanvasGroup>().alpha = titleAlpha[f];
            title.localScale = new Vector3(titleScale[f], titleScale[f], 1);
            auraColor.a = auraAlpha[f]; aura.color = auraColor;
            Canvas.ForceUpdateCanvases();
            Rect r = title.rect;
            gleam.sizeDelta = new Vector2(r.height * 0.5f, r.height * 1.8f);
            gleam.anchoredPosition = new Vector2(gleamPos[f] * r.width, 0);

            Random.InitState(7 + f);
            int n = f == 0 ? 10 : 3;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("S", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform; rt.SetParent(layer, false);
                var img = go.GetComponent<Image>(); img.sprite = spark;
                Rect lr = layer.rect;
                rt.anchoredPosition = new Vector2(Random.Range(lr.xMin, lr.xMax) * 1.05f, Random.Range(lr.yMin, lr.yMax) * 1.1f);
                float size = Mathf.Min(lr.width, lr.height) * Random.Range(0.2f, 0.34f);
                rt.sizeDelta = new Vector2(size, size);
                float s = Random.Range(0.5f, 1f); rt.localScale = new Vector3(s, s, 1);
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-45f, 45f));
                var c = sparkColor; c.a = s; img.color = c;
            }
            Render(scene, 1170, 540, outDir + "/logo_" + f + ".png");
            // Undo the temporary changes in place (scene has unsaved user edits, so don't reload it)
            title.localScale = Vector3.one;
            Object.DestroyImmediate(aura.gameObject);
            Object.DestroyImmediate(layer.gameObject);
            Object.DestroyImmediate(gleam.gameObject);
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
