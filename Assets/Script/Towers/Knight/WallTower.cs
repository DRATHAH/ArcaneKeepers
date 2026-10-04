using System;
using UnityEngine;

[RequireComponent(typeof(TowerHealth))]
public class WallTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 3f;

    private TowerHealth towerHealth;
    private TowerRelocate towerRelocate;

    [Tooltip("Health the tower recovers while regenerating.")]
    [SerializeField] private float regenRate = 5f;

    [Tooltip("Determines what the damage resistances for this tower will be set to. Close to base = 0, Far from base = 1.")]
    [SerializeField] private float[] damageResists = new float[2];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        towerHealth = GetComponent<TowerHealth>();
        towerRelocate = GetComponent<TowerRelocate>();
    }

    // Update is called once per frame
    void Update()
    {
        if(towerRelocate.isRelocating == false)
        {
            if (transform.position.x < swapLimit)
            {
                AbilityClose();
            }
            else
            {
                AbilityFar();
            }
        }
    }

    //While close to home charge a strong single-target projectile (More damage than up close)
    public override void AbilityClose()
    {
        if(towerHealth.damageResist != damageResists[0])
        {
            towerHealth.damageResist = damageResists[0];
        }

        if(towerHealth.attackedTimer <= 0)
        {
            towerHealth.RecoverHealth(regenRate * Time.deltaTime);
        }

    }

    //While far from home shoot weaker projectiles with slightly more defense
    public override void AbilityFar()
    {
        if(towerHealth.damageResist != damageResists[1])
        {
            towerHealth.damageResist = damageResists[1];
        }
    }
}
