using System.Collections.Generic;
using System;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance { get; private set; }

    [Tooltip("List of enemies to spawn in throughout the level. Make a unique entry for each enemy.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Tooltip("Number of enemies to spawn in per wave.")]
    public List<int> numPerWave = new List<int>();

    [Tooltip("Spawn points for each enemy in a wave. Must match enemyPrefabs in length.")]
    public List<Vector3> spawnPoints = new List<Vector3>();

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
        if(enemyPrefabs.Count == 0)
        {
            throw new ArgumentException("No enemies to spawn! Cannot generate wave!", nameof(WaveManager));
        }

        if(numPerWave.Count == 0)
        {
            throw new ArgumentException("Enemies to spawn per wave not listed! Cannot generate wave!", nameof(WaveManager));
        }

        if (spawnPoints.Count == 0)
        {
            throw new ArgumentException("No enemy spawn points listed! Cannot generate wave!", nameof(WaveManager));
        }

        if (enemyPrefabs.Count != spawnPoints.Count)
        {
            throw new ArgumentException("Unequal spawn points and enemyPrefabs!", nameof(WaveManager));
        }
    }
}
