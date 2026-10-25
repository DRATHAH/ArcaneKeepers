using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Play mode, on MainMenu: taps StartButton and logs the cloud banks per frame until the wipe ends
public static class TestCloudWipe
{
    public static async Task<string> Run()
    {
        var log = new StringBuilder();
        var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.name == "StartButton");
        if (!button) return "no StartButton in " + SceneManager.GetActiveScene().name;
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        float start = Time.realtimeSinceStartup;
        bool seen = false;
        int frame = 0;
        while (Time.realtimeSinceStartup - start < 10)
        {
            await Awaitable.NextFrameAsync();
            var go = GameObject.Find("SceneTransition");
            var canvas = go ? go.GetComponent<Canvas>() : null;
            if (canvas && canvas.enabled)
            {
                seen = true;
                var clouds = go.transform.Find("Clouds");
                float w = ((RectTransform)go.transform).rect.width;
                if (clouds && clouds.gameObject.activeSelf && frame++ % 3 == 0)
                {
                    var l = (RectTransform)clouds.Find("LeftBank");
                    log.Append($"{Time.realtimeSinceStartup - start:0.00}s {SceneManager.GetActiveScene().name} leftEdge={l.anchoredPosition.x / w:0.00}\n");
                }
            }
            else if (seen)
            {
                break;
            }
        }
        return $"-> {SceneManager.GetActiveScene().name} in {Time.realtimeSinceStartup - start:0.00}s seen={seen}\n" + log;
    }
}
