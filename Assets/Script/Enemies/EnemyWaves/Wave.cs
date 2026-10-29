using UnityEngine;

[System.Serializable]
public class Wave
{
    public GameObject[] waveEnemies;
    [Range(0,4)]
    public int[] wavePosition;
    public float[] spawnDelay;
}
