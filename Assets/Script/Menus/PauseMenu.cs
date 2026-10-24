using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseButton;
    [Tooltip("Scene loaded when the player gives up.")]
    [SerializeField] private string giveUpScene = "LevelSelect";

    PauseMenuAnimator animator;

    private void Awake()
    {
        animator = GetComponent<PauseMenuAnimator>();
        if (!animator)
        {
            animator = gameObject.AddComponent<PauseMenuAnimator>();
        }
        animator.Setup(pausePanel, pauseButton);
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void Update()
    {
        // Escape on keyboard, back button on Android
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused)
            {
                Continue();
            }
            else
            {
                Pause();
            }
        }

        // Hide the pause button once the match is won or lost
        if (pauseButton && IsGameOver() && pauseButton.activeSelf)
        {
            pauseButton.SetActive(false);
        }
    }

    public void Pause()
    {
        if (IsGameOver())
        {
            return;
        }
        SetPaused(true);
        animator.Open();
    }

    // The menu folds away first; the game resumes once it is gone
    public void Continue()
    {
        animator.Close(() => SetPaused(false));
    }

    public void Restart()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GiveUp()
    {
        SetPaused(false);
        SceneManager.LoadScene(giveUpScene);
    }

    void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0 : 1;
        pausePanel.SetActive(paused);
        if (pauseButton)
        {
            pauseButton.SetActive(!paused);
        }
    }

    bool IsGameOver()
    {
        return WinLossManager.instance && (WinLossManager.instance.winner || WinLossManager.instance.loser);
    }

    private void OnDestroy()
    {
        IsPaused = false;
    }
}
