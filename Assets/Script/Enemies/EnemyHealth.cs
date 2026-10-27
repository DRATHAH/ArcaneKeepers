using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    [Header("Health Variables")]
    [Tooltip("Maximum health this enemy can have.")]
    [SerializeField] private float maxHealth = 250; //max health this enemy can have
    /*[HideInInspector]*/ public float currentHealth; //enemy's current health

    [Header("Armor Variables")]
    [Tooltip("Maximum armor health this enemy can have.")]
    [SerializeField] private float maxArmorHealth = 0;
    [SerializeField]private float currentArmorHealth; //current armor health this enemy has

    private EnemyMovement enemyMovement;
    private ColorFlash damageFlash;
    [SerializeField] private float flashLength = 0.5f;

    [Header("Animator Vars")]
    [SerializeField]private Animator healthAnim;
    [SerializeField] private int armorType;

    private BoxCollider col;
    void Start()
    {
        col = GetComponent<BoxCollider>();
        damageFlash = GetComponent<ColorFlash>();
        enemyMovement = GetComponent<EnemyMovement>();
        healthAnim.SetInteger("ArmorType", armorType);
    }

    void OnEnable()
    {
        //Reset health and armor for re-use
        if(col != null)
        {
            col.enabled = true;
        }
        currentHealth = maxHealth;
        currentArmorHealth = maxArmorHealth;
        AnimUpdate();
    }

    //Taking damage func
    public override void TakeDamage(float damageAmount, bool armorPiercing)
    {
        SoundManager.Instance.PlaySound("EnemyDamage");
        CalcDamage(damageAmount, armorPiercing);
        //If the current health is less than 0, die.
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public override void TakeDamage(float damageAmount, bool armorPiercing, Vector3 knockback)
    {
        CalcDamage(damageAmount, armorPiercing);

        enemyMovement.Push(knockback);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    //Helper func for death
    public override void Die()
    {
        if(WaveSpawner.instance != null)
        {
            WaveSpawner.instance.enemiesSpawned.Remove(this); //remove this from the enemy spawned pool
            WaveSpawner.instance.enemiesThisWave.Remove(this); //remove this from the enemy spawned pool
        }
        if (TryGetComponent<IDeathAbility>(out IDeathAbility deathAbility))
        {
            deathAbility.OnDeathAbility(); //Trigger the death ability if there is one
        }
        StartCoroutine(DeathAnim());
    }

    private IEnumerator DeathAnim()
    {
        col.enabled = false;
        yield return new WaitForSeconds(0.375f);
        ObjectPool.pool.Destroy(this.gameObject); //Return this item to the object pool
    }

    private void CalcDamage(float damageAmount, bool armorPiercing)
    {
        float activeDamage = damageAmount;
        if (damageFlash != null)
        {
            damageFlash.CallFlash(Color.white, flashLength);
        }

        //If the attack will hit the armor and the armor will survive the hit do damage to the armor
        if (currentArmorHealth - activeDamage > 0 && !armorPiercing)
        {
            currentArmorHealth -= activeDamage;
        }
        else if (currentArmorHealth > 0 && currentArmorHealth - activeDamage <= 0 && !armorPiercing)
        {
            //If the attack will hit the armor but the armor won't survive the hit
            activeDamage -= currentArmorHealth; //Subtract the current armor health from the damage amount
            Debug.Log(activeDamage);
            currentArmorHealth = 0; //Reduce armor health to 0
            IncrementMeter(activeDamage);
            currentHealth -= activeDamage; //Subtract remaining amount from current health
        }
        else if(armorPiercing || currentArmorHealth <= 0)
        {
            //If the attack is either armor piercing or the armor health is less than 0
            IncrementMeter(activeDamage);
            currentHealth -= activeDamage; //Subtract damage amount from current health
        }

        AnimUpdate();
    }

    private void IncrementMeter(float damageAmount)
    {
        if (WaveSpawner.instance != null)
        {
            if (WaveSpawner.instance.enemiesThisWave.Find(x => this))
            {
                WaveSpawner.instance.incrementHealthMeter -= damageAmount; //Increment damage meter with remaining amount
            }
        }
    }


    private void AnimUpdate()
    {
        if (healthAnim != null)
        {
            healthAnim.SetFloat("Health", currentHealth / maxHealth);
        }

        if (healthAnim != null)
        {
            healthAnim.SetFloat("ArmorHealth", currentArmorHealth / maxArmorHealth);
        }
    }

}
