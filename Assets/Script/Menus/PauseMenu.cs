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
    }

    public void Continue()
    {
        SetPaused(false);
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
