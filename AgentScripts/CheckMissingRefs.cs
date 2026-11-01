using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

// Scans every scene / prefab / asset under Assets for GUID refs that no longer resolve to an asset
// (ignores built-in GUIDs 0000...e/f). Also checks Resources.Load("LevelEndScreen") and missing MonoScripts.
public static class CheckMissingRefs
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var rx = new Regex(@"guid: ([0-9a-f]{32})");
        string repo = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        var files = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset") || p.EndsWith(".mat") || p.EndsWith(".controller")))
            .Where(p => !p.StartsWith("Assets/TextMesh Pro"));
        int bad = 0;
        foreach (var f in files)
        {
            string text = File.ReadAllText(Path.Combine(repo, f));
            foreach (var g in rx.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value).Distinct())
            {
                if (g.StartsWith("0000000000000000")) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g))) continue;
                bad++;
                sb.AppendLine("MISSING " + g + " in " + f);
            }
        }
        var les = UnityEngine.Resources.Load<UnityEngine.GameObject>("LevelEndScreen");
        sb.AppendLine("Resources LevelEndScreen: " + (les ? "ok" : "NOT FOUND"));
        sb.AppendLine("missing refs: " + bad);
        return sb.ToString();
    }
}
