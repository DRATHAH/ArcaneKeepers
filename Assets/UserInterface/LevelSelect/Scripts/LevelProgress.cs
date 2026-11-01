using UnityEngine;

// Which levels the player has beaten. Levels unlock in order: beating level N opens level N+1.
// Saved in PlayerPrefs as the highest level number beaten (0 = none yet, so only level 1 is open).
public static class LevelProgress
{
    // Level number (1-based) -> scene name
    public static readonly string[] Scenes = { "DemoLevel", "Level2", "Level3" };

    const string Key = "LevelsBeaten";

    public static int Beaten => PlayerPrefs.GetInt(Key, 0);

    // 1-based level number of a scene, or 0 if it isn't one of the levels
    public static int NumberOf(string sceneName)
    {
        return System.Array.IndexOf(Scenes, sceneName) + 1;
    }

    public static bool IsUnlocked(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= Beaten + 1;
    }

    // Call when a level is won; never lowers progress (replaying an old level changes nothing)
    public static void MarkBeaten(string sceneName)
    {
        int number = NumberOf(sceneName);
        if (number > Beaten)
        {
            PlayerPrefs.SetInt(Key, number);
            PlayerPrefs.Save();
        }
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
