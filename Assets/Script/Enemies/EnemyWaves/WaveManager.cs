using System.Collections.Generic;
using System;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance { get; private set; }

    //[Tooltip("List of enemies to spawn in throughout the level. Make a unique entry for each enemy.")]
    //public List<GameObject> enemyPrefabs = new List<GameObject>();

    public List<Wave> levelWaves = new List<Wave>();

    public List<Vector3> spawnPoints = new List<Vector3>();

    //[Tooltip("Number of enemies to spawn in per wave.")]
    //public List<int> numPerWave = new List<int>();


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


    void Start()
    {
        if(levelWaves.Count == 0)
        {
            throw new ArgumentException("No enemies to spawn! Cannot generate wave!", nameof(WaveManager));
        }
    }
}
