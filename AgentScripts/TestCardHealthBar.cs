using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// Steps CardHealthBar by hand (edit mode) and logs fill/ghost/scale through show, hit, heal, hide.
// run_script AgentScripts/TestCardHealthBar.cs TestCardHealthBar.Run
public static class TestCardHealthBar
{
    public static string Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UserInterface/LevelHUD/Prefabs/TowerIcon.prefab");
        var go = Object.Instantiate(prefab);
        var sb = new StringBuilder();
        try
        {
            var hb = go.GetComponentInChildren<CardHealthBar>(true);
            var apply = typeof(CardHealthBar).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance);
            void Step(string label, float seconds)
            {
                for (float t = 0; t < seconds; t += 0.02f) apply.Invoke(hb, new object[] { 0.02f });
                sb.AppendLine($"{label}: active={hb.gameObject.activeSelf} fill={hb.fill.fillAmount:F3} ghost={hb.ghost.fillAmount:F3} scale={hb.body.localScale.x:F2} col={hb.fill.color}");
            }
            hb.SetValue(1f);
            hb.SetShown(true);
            Step("shown", 0.05f);
            Step("popped", 0.5f);
            hb.SetValue(0.6f);
            Step("hit 0.04s", 0.04f);
            Step("hit 0.6s", 0.56f);
            Step("hit 1.5s", 0.9f);
            hb.SetValue(0.2f);
            Step("low", 1f);
            hb.SetValue(0.5f);
            Step("heal 0.1s", 0.1f);
            Step("heal 1s", 0.9f);
            hb.SetShown(false);
            Step("hiding", 0.1f);
            Step("hidden", 0.3f);
            hb.SetValue(1f);
            hb.SetShown(true);
            Step("reshown", 0.6f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        return sb.ToString();
    }
}
