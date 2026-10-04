using System;
using UnityEngine;

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

    [SerializeField] public GameObject winnerCanvas;
    [SerializeField] public GameObject loserCanvas;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(winner == true)
        {
            Win();
        }
        else if(loser == true)
        {
            Lose();
        }
    }

    private void Win()
    {
        winnerCanvas.SetActive(true);
        Time.timeScale = 0;
    }

    private void Lose()
    {
        loserCanvas.SetActive(true);
        Time.timeScale = 0;
    }
}
