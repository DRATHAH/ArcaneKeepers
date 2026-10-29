using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner instance { get; private set; } //Wave spawner singleton
    private WaveManager waveManager; //Reference to the waveManager

    [Header("Wave Vars")]
    [Tooltip("Current wave of enemies. Only adjust for testing purposes!")]
    public int currentWave = 0; //Current wave of enemies
    private int maxWaves = 0; //Maximum number of waves of enemies

    [Header("Wave Delay Vars")]
    [Tooltip("Max time between new waves spawning")]
    [SerializeField]private float incrementTimerBase = 5f; //Base amount for the wave timer

    [Tooltip("Time period the player gets to set up at the start of a level.")]
    [SerializeField] private float maxGracePeriod = 10f; //Base amount of grace period for a level
    private float gracePeriod; //actual timer for the grace period

    private float incrementTimer; //Actual timer for wave increases
    [HideInInspector] public List<EnemyHealth> enemiesSpawned = new List<EnemyHealth>(); //List of enemy's health spawned in a wave
    [HideInInspector] public List<EnemyHealth> enemiesThisWave = new List<EnemyHealth>();


    public float incrementHealthMeter; //Total amount of heal spawned in a wave

    [Header("Wave Health Vars")]
    [Tooltip("Percentile amount of damage that needs to be dealt in order to summon the next wave. Should be a decimal.")]
    [SerializeField]private float incrementPercent; //Multiplier to check against for summoning waves early
    [SerializeField]private float incrementAmount; //Total amount of health spawned in a wave multiplied by the increment percent

    [Header("Game End Vars")]
    [Tooltip("Prefab for the object that should be awarded at the end of the level.")]
    [SerializeField] private GameObject winTrigger; 
    private Vector3 winTriggerSpawn = new Vector3(0, 1.25f, 0); //Where the win trigger for the level spawns in
    private bool winner = false; //Has the player finished the level

    private bool spawning = false; //Is the enemy actively spawning in enemies?

    private IEnumerator spawnWave; //coroutine for spawning in an enemy

    //Singleton setup
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
        maxWaves = waveManager.levelWaves.Count; //Set the maximum waves for the level
        //Debug.Log(maxWaves); //Debug message for checking the current wave count
    }

    void Update()
    {
        //Check if the grace period is up
        if(gracePeriod <= 0)
        {
            //If the incremental health meter is below a certain point or if there are no enemies to spawn
            if ((incrementHealthMeter <= incrementAmount || enemiesSpawned.Count == 0) && spawning == false)
            {
                if (currentWave < maxWaves - 1)
                {
                    //Debug.Log("Armed and Dangerous!");
                    IncrementWave();
                }
            }


            //If the time between waves is greater than a certain amount decrease it
            if (incrementTimer > 0)
            {
                incrementTimer -= Time.deltaTime;
            }
            else
            {
                //Otherwise spawn the next wave if possible
                if (currentWave < maxWaves - 1)
                {
                    //Debug.Log(maxWaves);
                    //Debug.Log("Again!");
                    IncrementWave();
                }
            }

            //If there are no more waves to spawn and all enemies are dead print the debug message
            if (currentWave >= maxWaves - 1 && enemiesSpawned.Count == 0)
            {
                if (winner == false)
                {
                    winner = true; //Don't trigger this again
                    ObjectPool.pool.Create(winTrigger, winTriggerSpawn, Quaternion.identity); //Create a win trigger at its spawn point
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
        if(spawnWave == null)
        {
            //Increase the current wave
            currentWave++;

            enemiesThisWave.Clear(); //Clear enemies spawned list
            incrementTimer = incrementTimerBase; //Set the increment timer
            gracePeriod = 0; //Clear the grace period

            //Create an enemy from the object pool, and try to get their health component
            spawnWave = spawnEnemy();
            StartCoroutine(spawnWave);
        }

    }

    //Coroutine for spawning a wave
    private IEnumerator spawnEnemy()
    {
        spawning = true; //The coroutine is currently spawning enemies

        //For each enemy in a wave
        for (int i = 0; i < waveManager.levelWaves[currentWave].waveEnemies.Length; i++)
        {
            yield return new WaitForSeconds(waveManager.levelWaves[currentWave].spawnDelay[i]); //Wait for spawnDelay seconds
           
            //Create an enemy from the object pool
            GameObject newbie = ObjectPool.pool.Create(waveManager.levelWaves[currentWave].waveEnemies[i], waveManager.spawnPoints[waveManager.levelWaves[currentWave].wavePosition[i]], Quaternion.identity);

            //Try to get the enemy's health component and add it to the various lists needed for operation
            if (newbie.TryGetComponent<EnemyHealth>(out EnemyHealth newbieHealth))
            {
                newbieHealth.SetTile(waveManager.levelWaves[currentWave].wavePosition[i]);
                enemiesSpawned.Add(newbieHealth);
                enemiesThisWave.Add(newbieHealth);
            }
            else
            {
                //If the enemy has no health component, say so
                Debug.LogWarning("Enemy created has no health component!");
            }
        }

        incrementHealthMeter = 0; //Reset the increment health

        //For each enemy in enemy spawned add their health to the health meter
        for (int i = 0; i < enemiesThisWave.Count; i++)
        {
            Debug.Log(enemiesThisWave[i]);
            incrementHealthMeter += enemiesThisWave[i].currentHealth;
        }
        //Set the increment amount equal to the meter amount
        incrementAmount = incrementHealthMeter * incrementPercent;


        spawning = false; //No longer spawning

        spawnWave = null; //Clear the coroutine so it can be run again
    }

    public void BonusSpawn(GameObject newbies)
    {
        //Try to get the enemy's health component and add it to the various lists needed for operation
        if (newbies.TryGetComponent<EnemyHealth>(out EnemyHealth newbieHealth))
        {
            enemiesSpawned.Add(newbieHealth);
            enemiesThisWave.Add(newbieHealth);
        }
        else
        {
            //If the enemy has no health component, say so
            Debug.LogWarning("Enemy created has no health component!");
        }
    }

}
