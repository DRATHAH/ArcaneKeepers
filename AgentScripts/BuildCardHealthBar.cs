using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Replaces the plain Slider health bar on TowerIcon.prefab (Card/Art/HealthBar) with CardHealthBar.
// Art first: python AgentScripts/GenerateCardHealthArt.py
// run_script AgentScripts/BuildCardHealthBar.cs BuildCardHealthBar.Run
public static class BuildCardHealthBar
{
    const string PrefabPath = "Assets/UserInterface/LevelHUD/Prefabs/TowerIcon.prefab";
    const string ArtDir = "Assets/UserInterface/LevelHUD/Art/Cards/HealthBar/";
    public static readonly Vector2 Size = new Vector2(276f, 58f);

    public static string Run()
    {
        Sprite frame = ImportSprite("CardHP_Frame");
        Sprite fillSprite = ImportSprite("CardHP_Fill");
        Sprite heartSprite = ImportSprite("CardHP_Heart");

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var button = root.GetComponent<TowerButton>();
            var old = root.transform.Find("Card/Art/HealthBar");
            Transform parent = old ? old.parent : root.transform.Find("Card/Art");
            int index = old ? old.GetSiblingIndex() : parent.childCount;
            if (old) Object.DestroyImmediate(old.gameObject);

            var bar = new GameObject("HealthBar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.SetSiblingIndex(index);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = Size;
            bar.anchoredPosition = new Vector2(10f, 26f);

            var body = Child("Body", bar);
            Stretch(body);
            Img("Frame", body, frame, Color.white, false);
            var ghost = Img("Ghost", body, fillSprite, new Color(1f, 0.93f, 0.7f), true);
            var fill = Img("Fill", body, fillSprite, new Color(0.47f, 0.86f, 0.25f), true);
            var flash = Img("Flash", body, fillSprite, new Color(1f, 1f, 1f, 0f), true);

            var heart = Img("Heart", body, heartSprite, Color.white, false).rectTransform;
            heart.anchorMin = heart.anchorMax = new Vector2(0f, 0.5f);
            heart.sizeDelta = new Vector2(74f, 74f);
            heart.anchoredPosition = new Vector2(20f, 3f);
            heart.pivot = new Vector2(0.5f, 0.45f);

            var hb = bar.gameObject.AddComponent<CardHealthBar>();
            hb.body = body;
            hb.fill = fill;
            hb.ghost = ghost;
            hb.flash = flash;
            hb.heart = heart;
            bar.gameObject.SetActive(false);

            var so = new SerializedObject(button);
            so.FindProperty("towerHealthBar").objectReferenceValue = null;
            so.FindProperty("cardHealthBar").objectReferenceValue = hb;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return "ok";
    }

    static Sprite ImportSprite(string name)
    {
        string path = ArtDir + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static RectTransform Child(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Color color, bool filled)
    {
        var rt = Child(name, parent);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (filled)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        return img;
    }
}
