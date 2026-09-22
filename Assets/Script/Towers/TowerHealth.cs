using System.Runtime.CompilerServices;
using UnityEngine;

public class TowerHealth : HealthBase
{
    [SerializeField] private float maxHealth = 200;
    private float currentHealth;

    private float damageResist;

    public override void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount / damageResist;
    }

    public override void RecoverHealth(float recoveryAmount)
    {
        currentHealth += recoveryAmount;
    }
    public override void Die()
    {
        currentHealth = maxHealth;
        GameObject currentGameObject = this.gameObject;
        ObjectPool.pool.Destroy(ref currentGameObject);
    }
}
