using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    [Header("Health Variables")]
    [Tooltip("Maximum health this enemy can have.")]
    [SerializeField] private float maxHealth = 250; //max health this enemy can have
    [HideInInspector] public float currentHealth; //enemy's current health

    [Header("Armor Variables")]
    [Tooltip("Maximum armor health this enemy can have.")]
    [SerializeField] private float maxArmorHealth = 0;
    private float currentArmorHealth; //current armor health this enemy has

    void OnEnable()
    {
        //Reset health and armor for re-use
        currentHealth = maxHealth;
        currentArmorHealth = maxArmorHealth;
    }

    //Taking damage func
    public override void TakeDamage(float damageAmount, bool armorPiercing)
    {
        //If the attack will hit the armor and the armor will survive the hit do damage to the armor
        if(currentArmorHealth - damageAmount > 0 && !armorPiercing)
        {
            currentArmorHealth -= damageAmount;
        }
        else if(currentArmorHealth > 0 && currentArmorHealth - damageAmount <= 0 && !armorPiercing)
        {
            //If the attack will hit the armor but the armor won't survive the hit
            damageAmount -= currentArmorHealth; //Subtract the current armor health from the damage amount
            currentArmorHealth = 0; //Reduce armor health to 0
            if (WaveSpawner.instance.enemiesThisWave.Find(x => this))
            {
                WaveSpawner.instance.incrementHealthMeter -= damageAmount; //Increment damage meter with remaining amount
            }
            currentHealth -= damageAmount; //Subtract remaining amount from current health
        }

        //If the attack is either armor piercing or the armor health is less than 0
        if (armorPiercing || currentArmorHealth <= 0)
        {
            if(WaveSpawner.instance.enemiesThisWave.Find(x => this))
            {
                WaveSpawner.instance.incrementHealthMeter -= damageAmount; //Subtract damage amount from increment meter
            }
            currentHealth -= damageAmount; //Subtract damage amount from current health
        }

        //If the current health is less than 0, die.
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    //Helper func for death
    public override void Die()
    {
        WaveSpawner.instance.enemiesSpawned.Remove(this); //remove this from the enemy spawned pool
        WaveSpawner.instance.enemiesThisWave.Remove(this); //remove this from the enemy spawned pool
        if (TryGetComponent<OnDeathBase>(out OnDeathBase deathAbility))
        {
            deathAbility.OnDeathAbility(); //Trigger the death ability if there is one
        }
        ObjectPool.pool.Destroy(this.gameObject); //Return this item to the object pool
    }
}
