using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Loading scene: plays the cat animation and fills a bar while the target scene loads in the background.
// Use LoadingScreen.Load("SceneName") instead of SceneManager.LoadScene to go through it
public class LoadingScreen : MonoBehaviour
{
    public const string LoadingSceneName = "Loading";

    static string targetScene;

    [Header("Animation")]
    public Image animationImage;
    public Sprite[] frames;
    public float frameTime = 0.1f;

    [Header("Bar")]
    [Tooltip("Masked area that reveals the fill; its right anchor follows the progress.")]
    public RectTransform barFill;
    public TextMeshProUGUI percentText;
    [Tooltip("Keeps the screen up at least this long so the animation is seen, even if loading is instant.")]
    public float minimumTime = 1.5f;
    public float barSpeed = 1.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        targetScene = null;
    }

    public static void Load(string sceneName)
    {
        Time.timeScale = 1;
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        targetScene = sceneName;
        SceneTransition.Load(LoadingSceneName);
    }

    private void Start()
    {
        SetBar(0);
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("LoadingScreen opened without a target scene");
            return;
        }
        StartCoroutine(LoadTarget(targetScene));
    }

    private void Update()
    {
        if (animationImage && frames != null && frames.Length > 0)
        {
            int frame = (int)(Time.unscaledTime / frameTime) % frames.Length;
            animationImage.sprite = frames[frame];
        }
    }

    IEnumerator LoadTarget(string sceneName)
    {
        targetScene = null;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float shown = 0;
        float elapsed = 0;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            // Unity reports 0.9 when loading is done and only activation is left
            float loaded = Mathf.Clamp01(op.progress / 0.9f);
            float target = Mathf.Min(loaded, elapsed / minimumTime);
            shown = Mathf.MoveTowards(shown, target, barSpeed * Time.unscaledDeltaTime);
            SetBar(shown);

            if (shown >= 1f)
            {
                break;
            }
            yield return null;
        }
        op.allowSceneActivation = true;
    }

    void SetBar(float amount)
    {
        if (barFill)
        {
            barFill.anchorMax = new Vector2(amount, barFill.anchorMax.y);
        }
        if (percentText)
        {
            percentText.text = Mathf.RoundToInt(amount * 100) + "%";
        }
    }
}
