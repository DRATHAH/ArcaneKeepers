using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcaneKeepers.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;

// Play Mode in DemoLevel (after TutorialDirector.ResetProgress): walks through the whole tutorial,
// standing in for the player (taps, placing Keepers, moving one) and saves a frame at each step.
// If the level was started directly (no PlayerManager) it deals 4 Keeper cards itself.
// Unity must be in the foreground while it runs (focus it in parallel).
//   run_script AgentScripts/PreviewTutorial.cs  Run(outDir)
public static class PreviewTutorial
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static System.Text.StringBuilder log;
    static string dir;
    static int shot;

    public static async Task<string> Run(string outDir)
    {
        dir = outDir;
        shot = 0;
        Directory.CreateDirectory(outDir);
        foreach (var f in Directory.GetFiles(outDir, "*.png")) File.Delete(f);
        log = new System.Text.StringBuilder();
        var director = Object.FindAnyObjectByType<TutorialDirector>();
        if (!director || !director.enabled) return "no running TutorialDirector (reset progress, enter Play Mode in DemoLevel)";
        var gm = GameManager.instance;

        // deal cards if the level was opened directly
        if (gm.towerButtonParent.childCount == 0)
        {
            foreach (var t in gm.towerLoot.Select(e => e.tower).Where(t => t).Distinct().Take(4))
                Object.Instantiate(gm.towerButtonPrefab, gm.towerButtonParent).GetComponent<TowerButton>().Initialize(t);
            Log("dealt cards");
        }
        Log("waves held: " + (!Object.FindAnyObjectByType<WaveSpawner>().enabled));

        await Until(() => Get<DogGuide>(director, "dog") != null && Get<DogGuide>(director, "dog").Visible, 5f);
        await Secs(0.25f);
        ToCamera();
        Shot("dog_entering");
        await Secs(0.9f);
        Shot("welcome_typing");
        await LineDone(director);
        Shot("welcome");

        await Tap(director); await LineDone(director); Shot("intro");
        await Tap(director); await Secs(1.6f); Shot("lanes_demo"); await LineDone(director);
        await Tap(director); await Secs(1.4f); Shot("castle"); await LineDone(director);
        await Tap(director); await Secs(0.8f); Shot("meter"); await LineDone(director);
        await Tap(director); await Secs(0.8f); Shot("cards"); await LineDone(director);

        // first Keeper: watch the wand demo, then "place" one
        await Tap(director);
        await Secs(1.1f); Shot("drag_demo_a");
        await Secs(0.7f); Shot("drag_demo_b");
        await Secs(0.6f); Shot("drag_demo_c");
        var first = Place(director, 0);
        await Secs(0.3f); Shot("placed_burst");
        await Line(director, "on guard"); Shot("placed_line");
        await Tap(director); await Secs(0.8f); Shot("card_rests"); await LineDone(director);

        // second Keeper
        await Tap(director);
        await Secs(1.5f); Shot("second_demo");
        Place(director, 1);
        await Line(director, "natural"); Shot("second_placed");

        // move: tap the button, then move the first Keeper
        await Tap(director);
        await Line(director, "move"); Shot("relocate_button");
        RelocationManager.instance.EnableRelocate();
        await Line(director, "new tile"); await Secs(0.9f); Shot("relocate_drag_demo");
        MoveTower(director, first);
        await Secs(0.3f); Shot("relocate_done_burst");
        await Line(director, "footwork"); Shot("relocate_done");

        await Tap(director); await Line(director, "pause"); Shot("pause");
        await Tap(director); await Line(director, "Here they come"); Shot("go");
        await Tap(director);
        await Secs(1.2f); Shot("battle_start");
        Log("waves released: " + Object.FindAnyObjectByType<WaveSpawner>().enabled + " done=" + TutorialDirector.IsDone);
        await Secs(4f); Shot("battle_4s");
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        Log("wave=" + spawner.currentWave + " enemies=" + spawner.enemiesSpawned.Count);

        File.WriteAllText(dir + "/log.txt", log.ToString());
        return log.ToString();
    }

    // ------------------------------------------------------------ player stand-in

    static async Task Tap(TutorialDirector d)
    {
        await Secs(0.35f);
        var blocker = Get<UnityEngine.UI.Image>(d, "blocker");
        var data = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
        ExecuteEvents.Execute(blocker.gameObject, data, ExecuteEvents.pointerDownHandler);
        Log("tap (blocking=" + blocker.raycastTarget + ")");
        await Secs(0.05f);
    }

    static GameObject Place(TutorialDirector d, int nth)
    {
        var tile = (Tile)d.GetType().GetMethod("SuggestTile", Any).Invoke(d, new object[] { nth });
        var btn = ((List<TowerButton>)d.GetType().GetMethod("ReadyCards", Any).Invoke(d, null))[0];
        var tower = btn.tower;
        TouchManager.instance.hoveringTile = tile; // a real drop sets this first (TowerRelocate reads it in OnEnable)
        GameObject newbie = ObjectPool.pool.Create(tower.towerPrefab, tile.anchorPos.position, tower.towerPrefab.transform.rotation);
        btn.createdObject = newbie;
        if (newbie.TryGetComponent(out TowerHealth th)) th.parentButton = btn;
        newbie.transform.parent = tile.anchorPos;
        tile.SetTileObject(newbie);
        TouchManager.instance.hoveringTile = null;
        Log("placed " + tower.towerName + " on " + tile.transform.GetSiblingIndex());
        return newbie;
    }

    static void MoveTower(TutorialDirector d, GameObject tower)
    {
        Tile from = tower.GetComponentInParent<Tile>();
        var to = (Tile)d.GetType().GetMethod("NeighbourTile", Any).Invoke(d, new object[] { from });
        from.tileObject = null;
        to.tileObject = tower;
        tower.transform.position = to.anchorPos.position;
        tower.transform.parent = to.anchorPos;
        RelocationManager.instance.ClearObject();
        Log("moved tower " + from.transform.GetSiblingIndex() + " -> " + to.transform.GetSiblingIndex());
    }

    // ------------------------------------------------------------ helpers

    static T Get<T>(object o, string field) where T : class => o.GetType().GetField(field, Any)?.GetValue(o) as T;

    static async Task LineDone(TutorialDirector d)
    {
        var b = Get<SpeechBubble>(d, "bubble");
        await Until(() => !b.IsTyping, 8f);
        await Secs(0.15f);
    }

    // waits until Dog_Hero is saying a line containing <paramref name="part"/> and has finished typing it
    static async Task Line(TutorialDirector d, string part)
    {
        var b = Get<SpeechBubble>(d, "bubble");
        var t = b.GetComponentInChildren<TMPro.TMP_Text>();
        await Until(() => t.text.Contains(part), 10f);
        if (!t.text.Contains(part)) Log("TIMEOUT waiting for line: " + part + " (have: " + t.text + ")");
        await Until(() => !b.IsTyping, 8f);
        await Secs(0.15f);
    }

    static async Task Until(System.Func<bool> cond, float timeout)
    {
        float start = Time.unscaledTime;
        while (!cond() && Time.unscaledTime - start < timeout) await Awaitable.NextFrameAsync();
    }

    static async Task Secs(float s)
    {
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < s) await Awaitable.NextFrameAsync();
    }

    static void Log(string s)
    {
        log.AppendLine($"{Time.unscaledTime:0.00} {s}");
    }

    // Overlay canvases don't reach a RenderTexture: draw them through the camera instead
    static void ToCamera()
    {
        var cam = Camera.main;
        foreach (var c in Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = cam.nearClipPlane + 0.5f;
        }
    }

    static void Shot(string name)
    {
        ToCamera();
        int w = 1280, h = Mathf.RoundToInt(1280f * Screen.height / Mathf.Max(1, Screen.width));
        var cam = Camera.main;
        var rt = new RenderTexture(w, h, 24);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = old;
        shot++;
        File.WriteAllBytes($"{dir}/{shot:00}_{name}.png", tex.EncodeToPNG());
        Object.Destroy(rt);
        Object.Destroy(tex);
        Log("shot " + name);
    }
}
