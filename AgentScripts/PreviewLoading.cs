using System.IO;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders the Loading scene with the bar at the given progress (changes are discarded)
public static class PreviewLoading
{
    public static string Run(string outFile, float amount, int width, int height)
    {
        const string scenePath = "Assets/Scenes/Loading.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        LoadingScreen screen = Object.FindAnyObjectByType<LoadingScreen>();
        screen.barFill.anchorMax = new Vector2(amount, screen.barFill.anchorMax.y);
        screen.percentText.text = Mathf.RoundToInt(amount * 100) + "%";

        var rt = new RenderTexture(width, height, 24);
        var camGo = new GameObject("__RenderCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(0, 0, -1000);
        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10;

        RenderTexture.active = rt;
        for (int i = 0; i < 3; i++)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            scaler.enabled = true;
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
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return outFile;
    }
}
