using System.Runtime.CompilerServices;
using UnityEngine;

public class TowerHealth : HealthBase
{
    [Tooltip("How much health the tower can have.")]
    [SerializeField] private float maxHealth = 200;
    [SerializeField] private float currentHealth;

    [Tooltip("Damage resistance modifier.")]
    public float damageResist = 1;

    [HideInInspector] public bool isAttacked = false;
    [HideInInspector]public float attackedTimer = 0.2f;
    private float maxAttackedTimer = 0.2f;


    private void OnEnable()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if(attackedTimer > 0)
        {
            attackedTimer -= Time.deltaTime;
        }
        else
        {
            if(isAttacked != false)
            {
                isAttacked = false;
            }
        }
    }

    public override void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount / damageResist;
        isAttacked = true;
        attackedTimer = maxAttackedTimer;

        if (currentHealth <= 0)
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
        gameObject.transform.position = transform.position;
        isAttacked = false;
        currentHealth = maxHealth;
        GameObject currentGameObject = this.gameObject;
        ObjectPool.pool.Destroy(currentGameObject);
    }
}
