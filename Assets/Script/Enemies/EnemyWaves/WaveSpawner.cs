using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner instance { get; private set; } //Wave spawner singleton
    private WaveManager waveManager; //Reference to the waveManager

    [Header("Wave Vars")]
    [Tooltip("Current wave of enemies. Only adjust for testing purposes!")]
    public int currentWave = 0; //Current wave of enemies
    private int maxWaves = 0; //Maximum number of waves of enemies
    private int waveOffset = 0; //Offset from the start of the enemies list

    [Header("Wave Delay Vars")]
    [Tooltip("Max time between new waves spawning")]
    [SerializeField]private float incrementTimerBase = 5f; //Base amount for the wave timer

    [Tooltip("Time period the player gets to set up at the start of a level.")]
    [SerializeField] private float maxGracePeriod = 10f; //Base amount of grace period for a level
    private float gracePeriod; //actual timer for the grace period

    private float incrementTimer; //Actual timer for wave increases
    [HideInInspector] public List<EnemyHealth> enemiesSpawned = new List<EnemyHealth>(); //List of enemy's health spawned in a wave
    
    [HideInInspector]public float incrementHealthMeter; //Total amount of heal spawned in a wave

    [Header("Wave Health Vars")]
    [Tooltip("Percentile amount of damage that needs to be dealt in order to summon the next wave. Should be a decimal.")]
    [SerializeField]private float incrementPercent; //Multiplier to check against for summoning waves early
    private float incrementAmount; //Total amount of health spawned in a wave multiplied by the increment percent

    private void Awake()
    {
        //Create Singleton conditions
        if(instance != null && instance != this)
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
        gracePeriod = maxGracePeriod; //Set the grace period
        waveManager = WaveManager.instance; //reference the wavemanager singleton for convenience sake
        maxWaves = waveManager.numPerWave.Count; //Set the maximum waves for the level
    }

    void Update()
    {
        //Check if the grace period is up
        if(gracePeriod <= 0)
        {
            //If the incremental health meter is below a certain point or if there are no enemies to spawn
            if (incrementHealthMeter <= incrementAmount * incrementPercent || enemiesSpawned.Count == 0)
            {
                if (currentWave < maxWaves)
                {
                    IncrementWave();
                }
            }

            //If there are no more waves to spawn and all enemies are dead print the debug message
            if (currentWave >= maxWaves && enemiesSpawned.Count == 0)
            {
                Debug.Log("You win!");
            }

            //If the time between waves is greater than a certain amount decrease it
            if (incrementTimer > 0)
            {
                incrementTimer -= Time.deltaTime;
            }
            else
            {
                //Otherwise spawn the next wave if possible
                if (currentWave < maxWaves)
                {
                    IncrementWave();
                }
            }
        }
        else
        {
            //Subtract from the grace period if the time left is greater than 0
            gracePeriod -= Time.deltaTime;
        }
    }

    //Helper func for incrementing a wave
    private void IncrementWave()
    {
        enemiesSpawned.Clear(); //Clear enemies spawned list
        incrementTimer = incrementTimerBase; //Set the increment timer
        gracePeriod = 0; //Clear the grace period

        //For each enemy in the current wave
        for (int i = 0; i < waveManager.numPerWave[currentWave]; i++)
        {
            //Create an enemy from the object pool, and try to get their health component
            GameObject newbie = ObjectPool.pool.Create(waveManager.enemyPrefabs[i + waveOffset], waveManager.spawnPoints[i + waveOffset]);
            if(newbie.TryGetComponent<EnemyHealth>(out EnemyHealth newbieHealth))
            {
                enemiesSpawned.Add(newbieHealth);
            }
            else
            {
                Debug.LogWarning("Enemy created has no health component!");
            }
        }
        //For each enemy in enemy spawned add their health to the health meter
        for(int i = 0; i < enemiesSpawned.Count; i++)
        {
            incrementHealthMeter += enemiesSpawned[i].currentHealth;
        }
        //Set the increment amount equal to the meter amount
        incrementAmount = incrementHealthMeter;

        //Increase the wave offset
        waveOffset += waveManager.numPerWave[currentWave];
        
        //Increase the current wave
        currentWave++;
    }
}
