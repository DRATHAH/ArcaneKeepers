using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play mode, TowerSelect open: fills the squad, taps DEPLOY and logs every frame (time, frame gap, scene,
// transition overlay state) until the transition ends. Shows hitches as big frame gaps.
public static class TestDeployTransition
{
    public static async Task<string> Run()
    {
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (!screen) return "no CharacterSelectScreen in " + SceneManager.GetActiveScene().name;
        for (int i = 0; i < screen.cards.Count && screen.Count < CharacterSelectScreen.SquadSize; i++)
        {
            screen.Toggle(i);
        }
        // Let the recruit flights finish
        float wait = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - wait < 1.5f) await Awaitable.NextFrameAsync();
        if (screen.Count < CharacterSelectScreen.SquadSize) return "squad only " + screen.Count;

        var log = new StringBuilder();
        float start = Time.realtimeSinceStartup;
        float last = start;
        screen.Play();
        bool seen = false;
        float maxGap = 0;
        while (Time.realtimeSinceStartup - start < 8)
        {
            await Awaitable.NextFrameAsync();
            float now = Time.realtimeSinceStartup;
            float gap = now - last;
            last = now;
            var go = GameObject.Find("SceneTransition");
            var canvas = go ? go.GetComponent<Canvas>() : null;
            bool on = canvas && canvas.enabled;
            seen |= on;
            if (gap > maxGap) maxGap = gap;
            if (gap > 0.03f || (on && Mathf.Repeat(now * 30, 3) < 1))
            {
                log.Append($"{now - start:0.000}s gap={gap * 1000:0}ms {SceneManager.GetActiveScene().name} overlay={on}\n");
            }
            if (seen && !on) break;
        }
        return $"-> {SceneManager.GetActiveScene().name} in {Time.realtimeSinceStartup - start:0.00}s maxGap={maxGap * 1000:0}ms\n" + log;
    }
}
