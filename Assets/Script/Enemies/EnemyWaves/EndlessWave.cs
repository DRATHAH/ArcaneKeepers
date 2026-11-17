using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class EnemyTier
{
    public int tierLevel;
    public List<GameObject> enemies;
}

public class EndlessWave : MonoBehaviour
{
    public List<Vector3> spawnPoints = new List<Vector3>();
    public List<EnemyTier> enemyTiers = new List<EnemyTier>();

    int waveNum;
    List<GameObject> waveEnemies;
    bool waveStarted = false;


}
