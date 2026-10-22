using System;
using UnityEngine;

[Serializable]
public class Attack
{
    public enum AttackType
    {
        Melee,
        Ranged,
        Wall
    }
    public AttackType attackType;

    public void RunAttack()
    {
        switch (attackType)
        {
            case AttackType.Melee:
                break;

            case AttackType.Ranged:
                break;

            default:
                break;
        }
    }

    public GameObject CreateProjectile(int damage, GameObject prefab, Transform tower)
    {
        GameObject projectile = ObjectPool.pool.Create(prefab, tower.position, Quaternion.identity);
        return projectile;
    }
}
