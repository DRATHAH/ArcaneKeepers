using System.IO;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Opens a scene (without saving changes), renders its canvases to a PNG at the given size
public static class RenderMenu
{
    public static string All(string outDir)
    {
        string[] scenes = { "LevelSelect", "TowerSelect" };
        int[,] sizes = { { 2340, 1080 }, { 1920, 1080 }, { 2048, 1536 } };
        string[] tags = { "phone", "16x9", "tablet" };
        foreach (string s in scenes)
            for (int i = 0; i < 3; i++)
                Run("Assets/Scenes/" + s + ".unity", sizes[i, 0] / 2, sizes[i, 1] / 2, outDir + "/" + s + "_" + tags[i] + ".png");
        return "ok";
    }

    public static string Run(string scenePath, int width, int height, string outFile)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var rt = new RenderTexture(width, height, 24);
        var camGo = new GameObject("__RenderCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(0, 0, -1000);

        var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(false)).Where(c => c.isRootCanvas).ToList();
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 10;
        }
        // Character screen preview: fill the slots like the game does
        var ui = Object.FindAnyObjectByType<InventoryUI>();
        var pm = Object.FindAnyObjectByType<PlayerManager>();
        if (ui && pm)
        {
            var keepers = ui.towersParent.GetComponentsInChildren<InventorySlot>(true);
            var team = ui.selectedTowersParent.GetComponentsInChildren<InventorySlot>(true);
            for (int i = 0; i < keepers.Length; i++) { if (i < pm.cheatedTowers.Count) keepers[i].AddItem(pm.cheatedTowers[i]); else keepers[i].ClearSlot(); }
            for (int i = 0; i < team.Length; i++) { if (i < 2) team[i].AddItem(pm.cheatedTowers[i]); else team[i].ClearSlot(); }
        }
        RenderTexture.active = rt;
        for (int i = 0; i < 3; i++)
        {
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler) { scaler.enabled = false; scaler.enabled = true; }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var t in Object.FindObjectsByType<TMP_Text>()) t.ForceMeshUpdate();
        }
        cam.Render();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        // Discard the temporary changes
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return outFile;
    }
}
