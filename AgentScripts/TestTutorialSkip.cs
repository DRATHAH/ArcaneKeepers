using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcaneKeepers.Tutorial;
using UnityEngine;

// Play Mode in DemoLevel (after TutorialDirector.ResetProgress): presses SKIP a few seconds in, checks the
// waves are released, places one Keeper, then fast-forwards the battle and logs Dog_Hero's battle quips
// and the wave meter label. Unity must be focused while it runs.
//   run_script AgentScripts/TestTutorialSkip.cs  Run(outDir)
public static class TestTutorialSkip
{
    public static async Task<string> Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var log = new System.Text.StringBuilder();
        var d = Object.FindAnyObjectByType<TutorialDirector>();
        if (!d || !d.enabled) return "no running TutorialDirector";
        var gm = GameManager.instance;
        if (gm.towerButtonParent.childCount == 0)
            foreach (var t in gm.towerLoot.Select(e => e.tower).Where(t => t).Distinct().Take(4))
                Object.Instantiate(gm.towerButtonPrefab, gm.towerButtonParent).GetComponent<TowerButton>().Initialize(t);

        await Secs(3f);
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        log.AppendLine($"before skip: waves enabled={spawner.enabled} done={TutorialDirector.IsDone}");
        d.GetType().GetMethod("Skip", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(d, null);
        await Secs(1f);
        var bubble = Object.FindAnyObjectByType<SpeechBubble>();
        var text = bubble.GetComponentInChildren<TMPro.TMP_Text>();
        log.AppendLine($"after skip: waves enabled={spawner.enabled} done={TutorialDirector.IsDone} says='{text.text}'");

        // skipping turns the battle quips off; start them by hand to test them too
        await Secs(2.5f);
        d.StartCoroutine((System.Collections.IEnumerator)d.GetType().GetMethod("BattleReactions", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(d, null));

        // a Keeper in the middle lane so something gets defeated
        var tiles = gm.lanes.GetComponentsInChildren<Tile>();
        var tile = tiles[5 * 3 + 2];
        var btn = gm.towerButtonParent.GetComponentsInChildren<TowerButton>()[0];
        TouchManager.instance.hoveringTile = tile;
        var tower = ObjectPool.pool.Create(btn.tower.towerPrefab, tile.anchorPos.position, btn.tower.towerPrefab.transform.rotation);
        btn.createdObject = tower;
        if (tower.TryGetComponent(out TowerHealth th)) th.parentButton = btn;
        tower.transform.parent = tile.anchorPos;
        tile.SetTileObject(tower);
        TouchManager.instance.hoveringTile = null;

        var meter = WaveProgressMeter.Instance;
        var label = meter.GetComponentsInChildren<TMPro.TMP_Text>().First();
        Time.timeScale = 3f;
        string lastSaid = "";
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < 38f && !(WinLossManager.instance.winner || WinLossManager.instance.loser))
        {
            if (text.text != lastSaid && bubble.IsOpen)
            {
                lastSaid = text.text;
                log.AppendLine($"{Time.unscaledTime - start:0.0}s Dog_Hero: '{lastSaid}' meter='{label.text}' wave={spawner.currentWave}");
            }
            await Awaitable.NextFrameAsync();
        }
        Time.timeScale = 1f;
        log.AppendLine($"end: meter='{label.text}' wave={spawner.currentWave} alive={spawner.enemiesSpawned.Count} win={WinLossManager.instance.winner} lose={WinLossManager.instance.loser}");
        File.WriteAllText(outDir + "/skiplog.txt", log.ToString());
        return log.ToString();
    }

    static async Task Secs(float s)
    {
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < s) await Awaitable.NextFrameAsync();
    }
}
