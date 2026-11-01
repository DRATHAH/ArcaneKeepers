using UnityEditor;
using UnityEngine;

// Win/lose screen icons. The screen itself is built at runtime by LevelEndScreen; this only makes the
// button icons (same white + Ink outline style as the pause menu icons) and hooks them up on
// Assets/UserInterface/LevelEnd/Resources/LevelEndScreen.prefab.
// Menu: Arcane Keepers > Rebuild Win-Lose Icons  (or eval MenuBuilder.BuildLevelEndIcons())
public static partial class MenuBuilder
{
    const string LevelEndFolder = "Assets/UserInterface/LevelEnd/";
    const string LevelEndPrefabPath = LevelEndFolder + "Resources/LevelEndScreen.prefab";

    [MenuItem("Arcane Keepers/Rebuild Win-Lose Icons")]
    public static void BuildLevelEndIcons()
    {
        // Play/restart are shared with the pause menu so both screens use the exact same icons
        EnsurePauseIcons();
        string levels = LevelEndFolder + "Art/LevelEnd_IconLevels";
        WriteLevelsIcon(levels);
        AssetDatabase.Refresh();

        GameObject root = PrefabUtility.LoadPrefabContents(LevelEndPrefabPath);
        LevelEndScreen screen = root.GetComponent<LevelEndScreen>();
        screen.playIcon = PauseIcon("Pause_IconPlay");
        screen.restartIcon = PauseIcon("Pause_IconRestart");
        screen.levelsIcon = AssetDatabase.LoadAssetAtPath<Sprite>(levels + ".png");
        PrefabUtility.SaveAsPrefabAsset(root, LevelEndPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("Win/lose icons rebuilt");
    }

    // 2x2 grid of rounded tiles (the level select)
    static void WriteLevelsIcon(string path)
    {
        WriteIcon(path, (px, py) =>
            RoundedRectDistance(px, py, 44, 44, 17, 17, 6) < 0 || RoundedRectDistance(px, py, 84, 44, 17, 17, 6) < 0 ||
            RoundedRectDistance(px, py, 44, 84, 17, 17, 6) < 0 || RoundedRectDistance(px, py, 84, 84, 17, 17, 6) < 0);
    }
}
