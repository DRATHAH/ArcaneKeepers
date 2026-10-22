using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Places the main-menu hero group (MainMenu-HeroSquad.png) and its shadow inside Background/Characters.
// Rects are given as fractions of the Background (1080x604 units at the reference size).
public static class PlaceMainMenuHeroes
{
    public static string Run(float x0, float y0, float x1, float y1, float sx0, float sy0, float sx1, float sy1)
    {
        const string path = "Assets/UserInterface/MainMenu-HeroSquad.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        RectTransform heroes = null, shadow = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == "Heroes") heroes = t;
                if (t.name == "HeroesShadow") shadow = t;
            }
        heroes.GetComponent<Image>().sprite = sprite;
        Pin(heroes, x0, y0, x1, y1);
        Pin(shadow, sx0, sy0, sx1, sy1);
        EditorUtility.SetDirty(heroes.GetComponent<Image>());
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
