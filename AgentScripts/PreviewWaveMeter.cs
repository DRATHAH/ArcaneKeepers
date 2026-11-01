using System.IO;
using System.Reflection;
using ArcaneKeepers.Tutorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// Edit-mode still of the tutorial wave meter (at a few fill levels) + the wand, over the open scene.
/// run_script AgentScripts/PreviewWaveMeter.cs  PreviewWaveMeter.Run(outPng, progress)
public static class PreviewWaveMeter
{
    public static string Run(string outPng, float progress)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UserInterface/Tutorial/Prefabs/WaveMeter.prefab");
        var theme = AssetDatabase.LoadAssetAtPath<TutorialTheme>("Assets/UserInterface/Tutorial/Data/TutorialTheme.asset");
        GameObject go = Object.Instantiate(prefab);
        var camGo = new GameObject("PreviewCam");
        try
        {
            var meter = go.GetComponent<WaveProgressMeter>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(WaveProgressMeter).GetMethod("Build", flags).Invoke(meter, null);

            // fake flags for 3 waves (BuildFlags needs WaveManager)
            var bar = meter.Bar;
            float fmin = meter.fillMin, fmax = meter.fillMax;
            for (int i = 1; i <= 3; i++)
            {
                Image f = TutorialKit.Img("Flag" + i, bar, theme.waveFlag, Color.white, new Vector2(54f, 78f));
                f.preserveAspect = true;
                RectTransform r = f.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(Mathf.Lerp(fmax, fmin, i / 3f), 0.5f);
                r.pivot = new Vector2(0.18f, 0.08f);
                r.anchoredPosition = new Vector2(0f, -14f);
                r.SetSiblingIndex(bar.Find("Skull").GetSiblingIndex());
                r.localScale = Vector3.one * (progress >= i / 3f ? 1.1f : 0.7f);
            }

            typeof(WaveProgressMeter).GetField("progress", flags).SetValue(meter, progress);
            var label = bar.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();

            var canvas = go.GetComponent<Canvas>();
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.58f, 0.32f);
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;

            // wand pointing at the bar
            Image wand = TutorialKit.Img("Wand", go.transform, theme.wand, Color.white, new Vector2(theme.wand.rect.width, theme.wand.rect.height) * 0.82f);
            Image glow = TutorialKit.Img("Glow", go.transform, theme.softGlow, TutorialKit.A(theme.wandGlow, 0.7f), new Vector2(150, 150));
            glow.transform.SetSiblingIndex(wand.transform.GetSiblingIndex());
            wand.rectTransform.pivot = theme.wandTip;
            wand.rectTransform.anchoredPosition = glow.rectTransform.anchoredPosition = new Vector2(300f, 180f);
            wand.rectTransform.localEulerAngles = new Vector3(0, 0, 28f);

            Canvas.ForceUpdateCanvases();
            // Update positions the fill / skull; WaveSpawner is absent so progress stays
            typeof(WaveProgressMeter).GetMethod("Update", flags).Invoke(meter, null);
            label.text = progress <= 0f ? "GET READY" : "WAVE 2/3";
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(outPng));
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            return "fill=" + bar.Find("Fill").GetComponent<Image>().fillAmount + " bar=" + bar.rect.size;
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(camGo);
        }
    }
}
