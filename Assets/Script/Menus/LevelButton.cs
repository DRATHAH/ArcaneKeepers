using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A level on the level select screen. Locks itself if its scene isn't in the build settings yet
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    public string sceneName;
    public TextMeshProUGUI statusText;
    [Tooltip("Opened instead of the level if no towers are equipped yet.")]
    public string characterScene = "TowerSelect";
    [Tooltip("Seconds to wait after the tap so its effect can play before the screen changes")]
    public float loadDelay = 0.25f;

    Button button;
    bool loading;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
    }

    private void Start()
    {
        bool available = !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        button.interactable = available;
        if (statusText)
        {
            if (!available)
            {
                statusText.text = "Coming soon";
            }
            else
            {
                statusText.text = TeamInfo.HasTeam ? "Play" : "Pick team";
            }
        }
    }

    void LoadLevel()
    {
        if (!loading)
        {
            StartCoroutine(LoadAfterDelay());
        }
    }

    IEnumerator LoadAfterDelay()
    {
        loading = true;
        yield return new WaitForSecondsRealtime(loadDelay);
        Time.timeScale = 1;

        // Levels need the towers picked in the character menu (PlayerManager is created there)
        if (PlayerManager.instance == null || PlayerManager.instance.equippedTowers.Count == 0)
        {
            SceneTransition.Load(characterScene);
            loading = false;
            yield break;
        }
        LoadingScreen.Load(sceneName);
        loading = false;
    }
}
