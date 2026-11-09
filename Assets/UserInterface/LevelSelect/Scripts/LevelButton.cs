using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A level on the level select screen. Levels unlock in order (LevelProgress: beat level N to open N+1);
// a level whose scene isn't in the build yet stays locked as "Coming soon". The tile swaps between its
// playable look (blue, gold medal) and its locked look (grey, padlock) to match, and the next level to
// play becomes the glowing guide tile.
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    public string sceneName;
    public TextMeshProUGUI statusText;
    [Tooltip("Opened instead of the level if no towers are equipped yet.")]
    public string characterScene = "TowerSelect";
    [Tooltip("Seconds to wait after the tap so its effect can play before the screen changes")]
    public float loadDelay = 0.25f;

    [Header("Look")]
    public Image medal;
    public Image pill;
    public GameObject padlock;
    public Sprite tileOpen, tileLocked, medalOpen, medalLocked, pillOpen, pillLocked;

    Button button;
    bool loading;

    public int LevelNumber => LevelProgress.NumberOf(sceneName);
    public bool InBuild => !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    public bool Unlocked => InBuild && LevelProgress.IsUnlocked(LevelNumber);

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
        // Set up in Awake so LevelTile / LevelSelectFx see the right state when they start
        Refresh();
    }

    public void Refresh()
    {
        bool open = Unlocked;
        button.interactable = open;

        SetSprite(GetComponent<Image>(), open ? tileOpen : tileLocked);
        SetSprite(medal, open ? medalOpen : medalLocked);
        SetSprite(pill, open ? pillOpen : pillLocked);
        if (padlock)
        {
            padlock.SetActive(!open);
        }

        // The next level to play glows and nudges
        LevelTile tile = GetComponent<LevelTile>();
        if (tile)
        {
            tile.guide = open && LevelNumber == PlayerManager.instance.levelsBeaten + 1;
        }

        if (statusText)
        {
            if (open)
            {
                statusText.text = TeamInfo.HasTeam ? "Play" : "Pick team";
            }
            else
            {
                statusText.text = InBuild ? "Locked" : "Coming soon";
            }
        }
    }

    static void SetSprite(Image image, Sprite sprite)
    {
        if (image && sprite)
        {
            image.sprite = sprite;
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
