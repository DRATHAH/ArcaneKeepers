using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders still poses of the MainMenu LogoWhimsy animation (edit mode, MainMenu opened additively, not saved).
// Frames: 0 = falling (stretched, high), 1 = landing splat + puffs, 2 = idle rock + stars.
public static class PreviewLogoWhimsy
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

    public static string Run(string outDir)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Additive);
        var others = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s => s != scene)
            .SelectMany(s => s.GetRootGameObjects()).Where(g => g.activeSelf).ToList();
        foreach (var g in others) g.SetActive(false);
        try
        {
            var lw = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LogoWhimsy>(true)).First();
            var T = typeof(LogoWhimsy);
            T.GetMethod("Awake", F).Invoke(lw, null);
            var fx = (RectTransform)T.GetField("fxLayer", F).GetValue(lw);
            var star = (Sprite)T.GetField("starSprite", F).GetValue(null);
            var puff = (Sprite)T.GetField("puffSprite", F).GetValue(null);
            float[] hop = { 0.7f, 0f, 0.012f };
            Vector2[] sq = { new Vector2(0.92f, 1.14f), new Vector2(1.24f, 0.76f), Vector2.one };
            float[] idle = { 0f, 0f, 1f };
            for (int f = 0; f < 3; f++)
            {
                T.GetField("hop", F).SetValue(lw, hop[f]);
                T.GetField("squash", F).SetValue(lw, sq[f]);
                T.GetField("idleWeight", F).SetValue(lw, idle[f]);
                T.GetField("idleTime", F).SetValue(lw, f == 2 ? 1.4f : 0f);
                Canvas.ForceUpdateCanvases();
                T.GetMethod("Apply", F).Invoke(lw, null);
                foreach (Transform c in fx) Object.DestroyImmediate(c.gameObject);
                Rect r = fx.rect;
                Random.InitState(3 + f);
                if (f == 1)
                    for (int i = 0; i < 6; i++)
                    {
                        float side = i % 2 == 0 ? -1 : 1;
                        Add(fx, puff, r.height * 0.15f, new Vector2(side * r.width * (0.3f + 0.08f * i), r.yMin + r.height * 0.08f), new Color(1, 1, 1, 0.7f));
                    }
                if (f == 2)
                    for (int i = 0; i < 4; i++)
                    {
                        float a = Random.Range(-0.25f, 1.25f) * Mathf.PI;
                        Add(fx, star, r.height * 0.15f, new Vector2(Mathf.Cos(a) * r.width * 0.5f, Mathf.Sin(a) * r.height * 0.48f), new Color(1f, 0.88f, 0.4f));
                    }
                Render(scene, 1170, 540, outDir + "/whimsy_" + f + ".png");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            foreach (var g in others) g.SetActive(true);
        }
        return "ok";
    }

    static void Add(RectTransform parent, Sprite s, float size, Vector2 pos, Color c)
    {
        var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>(); img.sprite = s; img.color = c;
    }

    static void Render(UnityEngine.SceneManagement.Scene scene, int width, int height, string outFile)
    {
        var rtex = new RenderTexture(width, height, 24);
        var cam = new GameObject("__RenderCam").AddComponent<Camera>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, scene);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.orthographic = true; cam.targetTexture = rtex; cam.transform.position = new Vector3(0, 0, -1000);
        var canvases = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(false)).Where(c => c.isRootCanvas).ToList();
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
        Object.DestroyImmediate(cam.gameObject);
    }
}
