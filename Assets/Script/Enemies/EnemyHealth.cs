using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    [Header("Health Variables")]
    [SerializeField] private float maxHealth = 250;
    private float currentHealth;

    [Header("Armor Variables")]
    [SerializeField] private float maxArmorHealth = 0;
    private float currentArmorHealth;

    void OnEnable()
    {
        currentHealth = maxHealth;
        currentArmorHealth = maxArmorHealth;
    }

    public override void TakeDamage(float damageAmount, bool armorPiercing)
    {
        if(currentArmorHealth - damageAmount > 0 && !armorPiercing)
        {
            currentArmorHealth -= damageAmount;
        }
        else if(currentArmorHealth > 0 && currentArmorHealth - damageAmount <= 0 && !armorPiercing)
        {
            damageAmount -= currentArmorHealth;
            currentArmorHealth = 0;
            currentHealth -= damageAmount;
        }

        if (armorPiercing || currentArmorHealth <= 0)
        {
            currentHealth -= damageAmount;
        }

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    public override void Die()
    {
        GameObject currentGameObject = this.gameObject;

        if(TryGetComponent<OnDeathBase>(out OnDeathBase deathAbility))
        {
            deathAbility.OnDeathAbility();
        }
        ObjectPool.pool.Destroy(currentGameObject);
    }
}
