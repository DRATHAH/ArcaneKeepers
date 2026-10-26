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

    void Start()
    {
        winnerCanvas.SetActive(false);
        loserCanvas.SetActive(false);
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
        backgroundMusic.mute = true;
        SoundManager.Instance.PlaySound("Win");
        winnerCanvas.SetActive(true);
        Time.timeScale = 0;
    }

    private void Lose()
    {
        backgroundMusic.mute = true;
        SoundManager.Instance.PlaySound("Lose");
        loserCanvas.SetActive(true);
        Time.timeScale = 0;
    }
}
