using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dumps grid tile bounds + big renderers (bridge, castle, legs) per level so vine placement can be compared.
// run_script AgentScripts/LevelGeomInfo.cs LevelGeomInfo.Run("a.unity,b.unity")
public static class LevelGeomInfo
{
    public static string Run(string scenes)
    {
        var sb = new StringBuilder();
        foreach (var path in scenes.Split(','))
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("=== " + path);
            var tiles = Object.FindObjectsByType<Tile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (tiles.Length > 0)
            {
                var xs = tiles.Select(t => t.transform.position.x); var zs = tiles.Select(t => t.transform.position.z);
                sb.AppendLine($"  tiles n={tiles.Length} x {xs.Min():F2}..{xs.Max():F2} z {zs.Min():F2}..{zs.Max():F2} y {tiles[0].transform.position.y:F2} parent={tiles[0].transform.parent?.name}");
            }
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r is SpriteRenderer || r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                if (b.size.magnitude < 3f) continue;
                string p = r.name; var t = r.transform.parent; while (t) { p = t.name + "/" + p; t = t.parent; }
                var mats = string.Join(",", r.sharedMaterials.Where(m => m).Select(m => m.name));
                sb.AppendLine($"  {p} [{(r.gameObject.activeInHierarchy ? "on" : "off")}] c={b.center.ToString("F2")} s={b.size.ToString("F2")} mats={mats}");
            }
            var bv = GameObject.Find("BridgeVines");
            sb.AppendLine("  BridgeVines: " + (bv ? bv.transform.parent?.name : "none"));
            var rs = RenderSettings.ambientLight; sb.AppendLine($"  ambient={rs} fog={RenderSettings.fog} {RenderSettings.fogColor}");
            var sun = GameObject.Find("Sun"); if (sun) { var l = sun.GetComponent<Light>(); sb.AppendLine($"  sun rot={sun.transform.eulerAngles} col={l?.color} int={l?.intensity}"); }
        }
        return sb.ToString();
    }
}
