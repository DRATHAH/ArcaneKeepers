using System.Runtime.CompilerServices;
using UnityEngine;

public class TowerHealth : HealthBase
{
    [SerializeField] private float maxHealth = 200;
    [SerializeField]private float currentHealth;

    [HideInInspector] public float damageResist;

    public override void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount / damageResist;

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    public override void RecoverHealth(float recoveryAmount)
    {
        if(currentHealth + recoveryAmount < maxHealth)
        {
            currentHealth += recoveryAmount;
        }
        else
        {
            currentHealth = maxHealth;
        }
    }
    public override void Die()
    {
        currentHealth = maxHealth;
        GameObject currentGameObject = this.gameObject;
        ObjectPool.pool.Destroy(currentGameObject);
    }
}
