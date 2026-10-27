using System;
using UnityEngine;

[Serializable]
public class Attack
{
    public enum AttackType
    {
        Damage,
        Healing,
        Wall
    }
    public AttackType attackType;
    public int damage = 100;
    public float attackRate = 2f;
    [Tooltip("If attack is special. Damage = Ignore Armor, Healing = Burst")]
    public bool special = false;
    public float range = 5f;
    [Tooltip("List of lanes move can hit (Ex: -1 = lane below, 1 = lane above)")]
    [Range(-2,2)]
    public int[] lanes;
    public GameObject attackPrefab;

    public static GameObject CreateAttack(GameObject prefab, Vector3 spawnPos, Quaternion tower)
    {
        GameObject projectile = ObjectPool.pool.Create(prefab, spawnPos, Quaternion.identity);
        projectile.transform.rotation = tower;
        return projectile;
    }

    public static GameObject CreateHeal(GameObject prefab, Vector3 spawnPos)
    {
        GameObject healingField = ObjectPool.pool.Create(prefab, new Vector3(spawnPos.x, 0, spawnPos.z), Quaternion.identity);
        return healingField;
    }
}
