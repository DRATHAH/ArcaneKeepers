using System.Collections.Generic;
using System;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    #region Singleton Setup
    public static WaveManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    #endregion

    [Tooltip("Waves of enemies in the level.")]
    public List<Wave> levelWaves = new List<Wave>();

    [Tooltip("The different places enemies can spawn from.")]
    public List<Vector3> spawnPoints = new List<Vector3>();


    //Warning for if there are no waves currently loaded
    void Start()
    {
        if(levelWaves.Count == 0)
        {
            throw new ArgumentException("No enemies to spawn! Cannot generate wave!", nameof(WaveManager));
        }
    }
}
