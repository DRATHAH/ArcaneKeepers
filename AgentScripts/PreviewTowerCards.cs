using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Still poses of the in-level tower cards in DemoLevel (edit mode, scene reopened afterwards).
// run_script AgentScripts/PreviewTowerCards.cs PreviewTowerCards.Run ["outDir"]
//   cards_settled.png: Knight placed (dim + health bar), Archer recharging half way, Cleric ready glow, Assassin held
//   cards_dealing.png: mid deal-in
public static class PreviewTowerCards
{
    const string ScenePath = "Assets/Scenes/DemoLevel.unity";

    public static string Run(string outDir)
    {
        Render(outDir + "/cards_settled.png", false);
        Render(outDir + "/cards_dealing.png", true);
        return "ok";
    }

    static void Render(string outFile, bool dealing)
    {
        const int width = 1170, height = 540;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var gm = Object.FindFirstObjectByType<GameManager>();
        string[] names = { "Knight", "Archer", "Cleric", "Assassin" };
        for (int i = 0; i < names.Length; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(gm.towerButtonPrefab, gm.towerButtonParent);
            var button = go.GetComponent<TowerButton>();
            var card = go.GetComponent<TowerCard>();
            button.Initialize(AssetDatabase.LoadAssetAtPath<Tower>("Assets/Towers/" + names[i] + "Tower.asset"));
            var art = button.icon;
            card.readyGlow.color = new Color(1f, 0.85f, 0.3f, 0f);
            go.transform.Find("Card/Art/HealthBar").gameObject.SetActive(false);
            if (dealing)
            {
                float t = Mathf.Clamp01(1f - i * 0.35f);
                card.card.anchoredPosition = new Vector2(Mathf.Lerp(-420f, 0f, t), 0f);
                card.card.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-10f, 0f, t));
                continue;
            }
            if (i == 0)
            {
                art.color = card.dimmed;
                var hb = go.transform.Find("Card/Art/HealthBar");
                hb.gameObject.SetActive(true);
                hb.GetComponent<Slider>().value = 0.7f;
            }
            if (i == 1) { art.color = card.dimmed; card.shade.fillAmount = 0.5f; }
            if (i == 2) { card.readyGlow.color = new Color(1f, 0.85f, 0.3f, 1f); card.card.localScale = Vector3.one * 1.1f; }
            if (i == 3) { art.color = card.held; card.card.localScale = Vector3.one * 0.94f; }
        }

        var rt = new RenderTexture(width, height, 24);
        var cam = Camera.main;
        cam.targetTexture = rt;
        var canvases = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(false)).Where(c => c.isRootCanvas).ToList();
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + 0.5f;
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
            foreach (var f in Object.FindObjectsByType<AspectRatioFitter>(FindObjectsSortMode.None)) { f.enabled = false; f.enabled = true; }
            Canvas.ForceUpdateCanvases();
        }
        cam.Render();
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(outFile, tex.EncodeToPNG());
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
