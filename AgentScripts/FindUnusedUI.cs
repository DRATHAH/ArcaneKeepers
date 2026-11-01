using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

// Lists assets under the UI folders that no scene / prefab / asset references (by GUID).
// Scripts are reported separately: code-only use (static classes, AddComponent) is not a GUID ref.
public static class FindUnusedUI
{
    static readonly string[] Roots =
    {
        "Assets/UserInterface",
        "Assets/Sprites",
    };

    public static string Run(string outFile)
    {
        var all = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/")).ToArray();
        var holders = all.Where(p => !p.EndsWith(".cs") && !AssetDatabase.IsValidFolder(p)).ToArray();
        var refCount = new Dictionary<string, List<string>>();
        foreach (var h in holders)
            foreach (var d in AssetDatabase.GetDependencies(h, false))
            {
                if (d == h) continue;
                if (!refCount.TryGetValue(d, out var l)) refCount[d] = l = new List<string>();
                l.Add(h);
            }

        var sb = new StringBuilder();
        foreach (var root in Roots)
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;
            foreach (var p in all.Where(p => p.StartsWith(root + "/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p))
            {
                refCount.TryGetValue(p, out var users);
                var u = users == null ? "" : string.Join(" | ", users.Take(4));
                sb.AppendLine((users == null ? "UNUSED\t" : "used\t") + p + "\t" + (users?.Count ?? 0) + "\t" + u);
            }
        }
        File.WriteAllText(outFile, sb.ToString());
        return "ok " + outFile;
    }
}
