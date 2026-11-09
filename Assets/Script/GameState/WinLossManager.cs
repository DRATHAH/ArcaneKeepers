using System;
using UnityEngine;
using UnityEngine.UI;

public class WinLossManager : MonoBehaviour
{
    #region
    public static WinLossManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance);
        }
        else
        {
            instance = this;
        }
    }
    #endregion


    public bool winner;
    public bool loser;
    private bool gameEnded = false;

    [SerializeField] public GameObject winnerCanvas;
    [SerializeField] public GameObject loserCanvas;
    [SerializeField] private AudioSource backgroundMusic;

    // Where the match ended (the enemy that broke through / the reward that appeared).
    // The end screen centres its effects on it.
    private Vector3? focusPoint;

    public void SetFocus(Vector3 worldPoint)
    {
        if (!gameEnded && !winner && !loser)
        {
            focusPoint = worldPoint;
        }
    }

    void Start()
    {
        if (winnerCanvas) winnerCanvas.SetActive(false);
        if (loserCanvas) loserCanvas.SetActive(false);
    }

    void Update()
    {
        if (gameEnded == true) return;

        if (winner == true)
        {
            gameEnded = true;
            Win();
        }
        else if(loser == true)
        {
            gameEnded = true;
            Lose();
        }
    }

    private void Win()
    {
        // Opens the next level on the level select
        LevelProgress.MarkBeaten(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        if (backgroundMusic) backgroundMusic.mute = true;
        SoundManager.Instance.PlaySound("Win");

        // In-level finale (Resources/LevelEndScreen); the old canvas is only a fallback
        LevelEndScreen screen = CreateEndScreen();
        if (screen)
        {
            screen.PlayWin(focusPoint, GameManager.instance ? GameManager.instance.lastRewards : null);
            return;
        }
        if (winnerCanvas) winnerCanvas.SetActive(true);
        Time.timeScale = 0;

        PlayerManager.instance.SaveToFile();
    }

    private void Lose()
    {
        if (backgroundMusic) backgroundMusic.mute = true;
        SoundManager.Instance.PlaySound("Lose");

        LevelEndScreen screen = CreateEndScreen();
        if (screen)
        {
            screen.PlayLose(focusPoint);
            return;
        }
        if (loserCanvas) loserCanvas.SetActive(true);
        Time.timeScale = 0;
    }

    private LevelEndScreen CreateEndScreen()
    {
        LevelEndScreen prefab = Resources.Load<LevelEndScreen>(LevelEndScreen.ResourcePath);
        return prefab ? Instantiate(prefab) : null;
    }
}
