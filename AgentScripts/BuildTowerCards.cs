using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Turns the in-level TowerIcon prefab into a PvZ-style card (TowerCard) and gives each Tower its card art.
// run_script AgentScripts/BuildTowerCards.cs  BuildTowerCards.Run
// Card PNGs come from: python AgentScripts/CutoutCards.py
public static class BuildTowerCards
{
    const string PrefabPath = "Assets/Prefabs/UI/TowerIcon.prefab";
    const string CardFolder = "Assets/UserInterface/Cards/";

    public static string Run()
    {
        string log = "";
        foreach (string name in new[] { "Knight", "Archer", "Cleric", "Assassin", "Wizard" })
        {
            string path = CardFolder + name + "-Card.png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { log += "missing " + path + "\n"; continue; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.SaveAndReimport();

            var tower = AssetDatabase.LoadAssetAtPath<Tower>("Assets/Towers/" + name + "Tower.asset");
            tower.cardArt = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorUtility.SetDirty(tower);
            log += name + " -> " + tower.cardArt + "\n";
        }

        Sprite glow = null;
        foreach (string guid in AssetDatabase.FindAssets("CS_Glow t:Sprite"))
        {
            glow = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var button = root.GetComponent<TowerButton>();

        // Root keeps the layout cell + the raycast target TouchManager looks for, but draws nothing
        var hit = root.GetComponent<Image>();
        hit.sprite = null;
        hit.color = new Color(1, 1, 1, 0);
        hit.raycastTarget = true;

        DestroyChild(root.transform, "Card");
        var card = NewRect("Card", root.transform);
        Stretch(card, 0);

        var glowRt = NewRect("ReadyGlow", card);
        Stretch(glowRt, -40);
        var glowImg = glowRt.gameObject.AddComponent<Image>();
        glowImg.sprite = glow;
        glowImg.color = new Color(1f, 0.85f, 0.3f, 0f);
        glowImg.raycastTarget = false;

        var art = NewRect("Art", card);
        var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1.6f;
        var artImg = art.gameObject.AddComponent<Image>();
        artImg.raycastTarget = false;
        artImg.preserveAspect = false;

        var shade = NewRect("Shade", art);
        Stretch(shade, 0);
        var shadeImg = shade.gameObject.AddComponent<Image>();
        shadeImg.color = new Color(0.05f, 0.03f, 0.1f, 0.6f);
        shadeImg.type = Image.Type.Filled;
        shadeImg.fillMethod = Image.FillMethod.Vertical;
        shadeImg.fillOrigin = (int)Image.OriginVertical.Top;
        shadeImg.fillAmount = 0f;
        shadeImg.raycastTarget = false;

        // Health bar rides on the bottom of the card art
        var health = root.transform.Find("HealthBar") as RectTransform;
        if (health)
        {
            health.SetParent(art, false);
            health.anchorMin = new Vector2(0.1f, 0f);
            health.anchorMax = new Vector2(0.9f, 0f);
            health.pivot = new Vector2(0.5f, 0f);
            health.anchoredPosition = new Vector2(0f, 12f);
            health.sizeDelta = new Vector2(0f, 26f);
            foreach (var g in health.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
        }

        // The shade replaces the old cooldown slider
        DestroyChild(root.transform, "CooldownSlider");

        button.icon = artImg;
        var so = new SerializedObject(button);
        so.FindProperty("deathCooldownSlider").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        var tc = root.GetComponent<TowerCard>();
        if (tc == null) tc = root.AddComponent<TowerCard>();
        tc.card = card;
        tc.artFitter = fitter;
        tc.shade = shadeImg;
        tc.readyGlow = glowImg;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        return log + "glow " + glow + "\nprefab saved";
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static void DestroyChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t) Object.DestroyImmediate(t.gameObject);
    }
}
