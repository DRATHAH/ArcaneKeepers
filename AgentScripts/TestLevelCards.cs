using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play Mode, from TowerSelect: squad of 4 -> DEPLOY -> LevelSelect -> DemoLevel, then logs the in-level
// tower cards (TowerCard deal-in) frame by frame. Unity must be focused during the call.
public static class TestLevelCards
{
    public static async Task<string> Run(int frames)
    {
        var log = new StringBuilder();
        var screen = Object.FindAnyObjectByType<CharacterSelectScreen>();
        if (screen)
        {
            for (int i = 0; i < 200 && screen.cards.Count == 0; i++) await Awaitable.NextFrameAsync();
            await Wait(1f);
            for (int i = 0; screen.Count < 4 && i < screen.cards.Count; i++) screen.Toggle(i);
            await Wait(0.8f);
            log.AppendLine("squad " + screen.Count);
            screen.Play();
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "LevelSelect"; i++) await Awaitable.NextFrameAsync();
            await Wait(0.5f);
        }
        if (SceneManager.GetActiveScene().name != "DemoLevel")
        {
            SceneManager.LoadScene("DemoLevel");
        }
        for (int i = 0; i < 600 && Object.FindAnyObjectByType<TowerCard>() == null; i++) await Awaitable.NextFrameAsync();
        float start = Time.unscaledTime;
        for (int f = 0; f < frames; f++)
        {
            var cards = Object.FindObjectsByType<TowerCard>().OrderBy(c => c.transform.GetSiblingIndex()).ToArray();
            log.Append($"{Time.unscaledTime - start:0.000} dt={Time.unscaledDeltaTime * 1000:0}ms ts={Time.timeScale}");
            foreach (var c in cards)
            {
                log.Append($" | {c.name}#{c.transform.GetSiblingIndex()} en={c.enabled} x={c.card.anchoredPosition.x:0} rot={c.card.localEulerAngles.z:0} vis={c.gameObject.activeInHierarchy}");
            }
            log.AppendLine();
            await Awaitable.NextFrameAsync();
        }
        return log.ToString();
    }

    static async Task Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) await Awaitable.NextFrameAsync();
    }
}
