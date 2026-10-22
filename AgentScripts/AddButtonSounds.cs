using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Re-adds the teammates' "ButtonClick" sound (SoundManager.PlaySound on the SoundManager prefab)
// to every Button in the builder-made menu scenes. Re-run after MenuBuilder rebuilds a scene.
public static class AddButtonSounds
{
    const string SoundManagerPrefab = "Assets/Prefabs/Effects/SoundManager.prefab";

    public static string Run(string scenes)
    {
        string log = "";
        foreach (string name in scenes.Split(','))
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name.Trim() + ".unity");
            // Loaded after OpenScene (which unloads it) and by type name (run_script's
            // in-memory assembly can't match SoundManager by type).
            var sound = AssetDatabase.LoadAssetAtPath<GameObject>(SoundManagerPrefab).GetComponents<Component>()
                .First(c => c && c.GetType().Name == "SoundManager");
            foreach (var button in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Button>(true)))
            {
                bool has = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(i => button.onClick.GetPersistentMethodName(i) == "PlaySound");
                if (has)
                {
                    // Repair entries saved with a missing target
                    var fix = new SerializedObject(button);
                    var list = fix.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var t = list.GetArrayElementAtIndex(i);
                        if (t.FindPropertyRelative("m_MethodName").stringValue == "PlaySound" && t.FindPropertyRelative("m_Target").objectReferenceValue == null)
                            t.FindPropertyRelative("m_Target").objectReferenceValue = sound;
                    }
                    fix.ApplyModifiedPropertiesWithoutUndo();
                    continue;
                }
                var so = new SerializedObject(button);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                calls.InsertArrayElementAtIndex(calls.arraySize);
                var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
                call.FindPropertyRelative("m_Target").objectReferenceValue = sound;
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "SoundManager, Assembly-CSharp";
                call.FindPropertyRelative("m_MethodName").stringValue = "PlaySound";
                call.FindPropertyRelative("m_Mode").intValue = (int)PersistentListenerMode.String;
                call.FindPropertyRelative("m_CallState").intValue = (int)UnityEventCallState.RuntimeOnly;
                var args = call.FindPropertyRelative("m_Arguments");
                args.FindPropertyRelative("m_StringArgument").stringValue = "ButtonClick";
                args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";
                so.ApplyModifiedPropertiesWithoutUndo();
                log += name + "/" + button.name + " ";
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return log;
    }

    // Teammate's upgrade-testing cheat on main: 500 of one tower in PlayerManager.cheatedTowers.
    public static string SetCheatAmount(string towerGuid, int amount)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerSelect.unity");
        var pm = Object.FindAnyObjectByType<PlayerManager>();
        var tower = AssetDatabase.LoadAssetAtPath<Tower>(AssetDatabase.GUIDToAssetPath(towerGuid));
        var entry = pm.cheatedTowers.FirstOrDefault(e => e.tower == tower);
        if (entry == null) return "tower not in cheatedTowers";
        entry.amount = amount;
        EditorUtility.SetDirty(pm);
        EditorSceneManager.SaveScene(scene);
        return tower.name + " = " + amount;
    }
}
