using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// In-level pause screen (Assets/UserInterface/PauseMenu/Prefabs/PauseCanvas.prefab).
// Freezes the game and hands all motion to PauseMenuAnimator.
// Restart closes an iris on the button and reloads; Give Up slides back to the main menu.
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseButton;
    [Tooltip("Scene loaded when the player gives up.")]
    [SerializeField] private string giveUpScene = "MainMenu";

    static readonly Color Ink = new Color32(29, 21, 51, 255);

    PauseMenuAnimator animator;
    bool leaving;

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
        IsPaused = false;
        Time.timeScale = 1;
        animator.HideInstant();
    }

    private void Update()
    {
        if (leaving)
        {
            return;
        }

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
        if (IsPaused || leaving || IsGameOver() || SceneTransition.IsTransitioning)
        {
            return;
        }
        IsPaused = true;
        Time.timeScale = 0;
        animator.Open();
    }

    // The menu folds away first; the game resumes once it is gone
    public void Continue()
    {
        if (!IsPaused || leaving)
        {
            return;
        }
        animator.Close(() =>
        {
            IsPaused = false;
            Time.timeScale = 1;
        });
    }

    public void Restart()
    {
        if (Leave())
        {
            StartCoroutine(RestartRoutine(PressedPoint()));
        }
    }

    public void GiveUp()
    {
        if (Leave())
        {
            StartCoroutine(GiveUpRoutine());
        }
    }

    bool Leave()
    {
        if (leaving || !IsPaused)
        {
            return false;
        }
        leaving = true;
        animator.Leave();
        return true;
    }

    // Short beat so the button's bounce reads before the screen changes
    IEnumerator RestartRoutine(Vector2 screenPoint)
    {
        yield return FxKit.Wait(0.12f);
        IsPaused = false;
        LevelEndScreen.IrisWipe.Reload(Ink, screenPoint);
    }

    IEnumerator GiveUpRoutine()
    {
        yield return FxKit.Wait(0.12f);
        IsPaused = false;
        SceneTransition.Load(giveUpScene);
    }

    // Screen position of the button that was just tapped (the iris closes on it)
    static Vector2 PressedPoint()
    {
        GameObject pressed = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (pressed && pressed.transform is RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, rect.position);
        }
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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
