using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Places the hero cluster (MainMenu-HeroCluster.png, cut out of Cluster-Heros.png) on the left of the main
// menu, facing the skeletons, with HeroesShadow under its feet. Rects are fractions of Background/Stage.
public static class PlaceHeroCluster
{
    public static string Run(float x0, float y0, float x1, float y1, float sx0, float sy0, float sx1, float sy1)
    {
        const string path = "Assets/UserInterface/MainMenu-HeroCluster.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        RectTransform characters = null, shadow = null, skeletons = null, heroes = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == "Characters") characters = t;
                if (t.name == "HeroesShadow") shadow = t;
                if (t.name == "Skeletons") skeletons = t;
                if (t.name == "Heroes") heroes = t;
            }

        if (heroes == null)
        {
            var go = new GameObject("Heroes", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Add heroes");
            heroes = go.GetComponent<RectTransform>();
            heroes.SetParent(characters, false);
            go.layer = characters.gameObject.layer;
        }
        // Draw above both shadows.
        heroes.SetAsLastSibling();

        var img = heroes.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Same idle motion as the skeletons, if they have one.
        foreach (var c in skeletons.GetComponents<MonoBehaviour>())
        {
            if (c is Image || heroes.GetComponent(c.GetType()) != null) continue;
            var copy = heroes.gameObject.AddComponent(c.GetType());
            EditorUtility.CopySerialized(c, copy);
        }

        Pin(heroes, x0, y0, x1, y1);
        Pin(shadow, sx0, sy0, sx1, sy1);
        EditorUtility.SetDirty(img);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "heroes " + heroes.rect + " shadow " + shadow.rect;
    }

    static void Pin(RectTransform t, float x0, float y0, float x1, float y1)
    {
        Undo.RecordObject(t, "Place heroes");
        t.anchorMin = new Vector2(x0, y0);
        t.anchorMax = new Vector2(x1, y1);
        t.anchoredPosition = Vector2.zero;
        t.sizeDelta = Vector2.zero;
        t.pivot = new Vector2(0.5f, 0.5f);
        t.localScale = Vector3.one;
        EditorUtility.SetDirty(t);
    }
}
