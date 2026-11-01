using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Play Mode in a level (Unity focused): pauses, presses Restart (iris reload), then pauses again and
// presses Give Up (slide to LevelSelect). Logs the state after each step.
public static class TestPauseButtons
{
    public static async Task<string> Run()
    {
        var log = new System.Text.StringBuilder();
        var first = Object.FindAnyObjectByType<PauseMenu>();
        if (!first) return "no PauseMenu";
        string level = SceneManager.GetActiveScene().name;

        first.Pause();
        await Wait(0.9f);
        Press("RestartButton");
        await Wait(0.3f);
        log.AppendLine($"restart +0.3s: iris={(GameObject.Find("IrisWipe") != null)} scene={SceneManager.GetActiveScene().name}");
        await Wait(1.6f);
        var second = Object.FindAnyObjectByType<PauseMenu>();
        log.AppendLine($"restart done: scene={SceneManager.GetActiveScene().name} reloaded={(second && second != first)} paused={PauseMenu.IsPaused} timeScale={Time.timeScale} iris={(GameObject.Find("IrisWipe") != null)}");

        second.Pause();
        await Wait(0.9f);
        Press("GiveUpButton");
        await Wait(1.8f);
        log.AppendLine($"give up done: scene={SceneManager.GetActiveScene().name} (from {level}) paused={PauseMenu.IsPaused} timeScale={Time.timeScale}");
        return log.ToString();
    }

    static void Press(string name)
    {
        var button = Object.FindObjectsByType<Button>().First(b => b.name == name);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }

    static async Task Wait(float seconds)
    {
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < seconds) await Awaitable.NextFrameAsync();
    }
}
