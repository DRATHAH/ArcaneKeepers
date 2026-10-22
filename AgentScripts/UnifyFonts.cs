using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Switches every TMP text in all scenes + prefabs to Lilita One (the Clash of Critters font)
// and makes Lilita One the TMP default font for new text.
public static class UnifyFonts
{
    const string FontPath = "Assets/UserInterface/Fonts/Generated/LilitaOne SDF.asset";

    public static string Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) return "font not found";
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty) return "unsaved scene open: " + EditorSceneManager.GetSceneAt(i).path;
        string startScene = EditorSceneManager.GetActiveScene().path;
        var log = new List<string>();

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/TextMesh Pro")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            int n = Swap(root.GetComponentsInChildren<TMP_Text>(true), font);
            if (n > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); log.Add(path + ": " + n); }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var texts = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true));
            int n = Swap(texts, font);
            if (n > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); log.Add(path + ": " + n); }
        }

        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            log.Add("TMP Settings default font");
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
        return string.Join("\n", log);
    }

    static int Swap(IEnumerable<TMP_Text> texts, TMP_FontAsset font)
    {
        int n = 0;
        foreach (var t in texts)
        {
            if (t.font == font) continue;
            t.font = font;
            t.fontSharedMaterial = font.material;
            if (PrefabUtility.IsPartOfPrefabInstance(t)) PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            EditorUtility.SetDirty(t);
            n++;
        }
        return n;
    }
}
