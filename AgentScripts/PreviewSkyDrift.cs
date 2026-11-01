using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Edit-mode stills of TowerSelect's SkyDrift (clouds + birds): drives Awake/Tick by reflection,
// renders the canvas at 16:9, then reopens the scene so nothing is saved.
// run_script AgentScripts/PreviewSkyDrift.cs  Run(outDir)
public static class PreviewSkyDrift
{
    public static string Run(string outDir)
    {
        const string path = "Assets/Scenes/TowerSelect.unity";
        Directory.CreateDirectory(outDir);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sky = Object.FindObjectsByType<SkyDrift>().FirstOrDefault();
        if (!sky) return "no SkyDrift in scene";

        int width = 1920, height = 1080;
        var rt = new RenderTexture(width, height, 24);
        var camGo = new GameObject("__RenderCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(0, 0, -1000);
        var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas).ToList();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 10; }
        Settle(canvases);

        string log0;
        var bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var type = typeof(SkyDrift);
        type.GetMethod("Awake", bf).Invoke(sky, null);
        sky.GetComponent<CanvasGroup>().alpha = 1f;
        var tick = type.GetMethod("Tick", bf);
        var elapsed = typeof(MenuFxLayer).GetProperty("Elapsed", bf);
        var launch = type.GetMethod("LaunchFlock", bf);

        // The other background layers (sun rays, river glints) are driven the same way
        var others = Object.FindObjectsByType<MenuFxLayer>().Where(l => l != sky).ToList();
        foreach (var l in others)
        {
            l.GetType().GetMethod("Awake", bf).Invoke(l, null);
            l.GetComponent<CanvasGroup>().alpha = 1f;
        }
        log0 = "layers: sky + " + string.Join(",", others.Select(o => o.GetType().Name)) + "\n";

        float t = 0f;
        void Advance(float seconds)
        {
            for (float s = 0; s < seconds; s += 0.05f)
            {
                t += 0.05f;
                elapsed.SetValue(sky, t); tick.Invoke(sky, new object[] { 0.05f });
                foreach (var l in others)
                {
                    elapsed.SetValue(l, t);
                    l.GetType().GetMethod("Tick", bf).Invoke(l, new object[] { 0.05f });
                }
            }
        }

        string log = log0;
        float[] shots = { 0f, 6f, 12f };
        for (int i = 0; i < shots.Length; i++)
        {
            if (i == 0) { Advance(0.05f); launch.Invoke(sky, null); Advance(3f); }
            else Advance(shots[i] - shots[i - 1]);
            Settle(canvases);
            RenderTexture.active = rt;
            cam.Render();
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string file = Path.Combine(outDir, "sky_" + i + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            log += file + "\n";
        }
        log += "children=" + sky.transform.childCount;
        Object.DestroyImmediate(camGo);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        return log;
    }

    static void Settle(System.Collections.Generic.List<Canvas> canvases)
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler) { scaler.enabled = false; scaler.enabled = true; }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var f in Object.FindObjectsByType<CharacterSelectLayout>()) { f.enabled = false; f.enabled = true; }
            foreach (var x in Object.FindObjectsByType<TMP_Text>()) x.ForceMeshUpdate();
        }
    }
}
