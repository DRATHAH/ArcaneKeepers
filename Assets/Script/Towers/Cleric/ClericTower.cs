using UnityEngine;

public class ClericTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [Tooltip("How long the waiting period for shots for far ability should be.")]
    [SerializeField] private float maxHealingBurstCooldown;

    private float farCooldown;
    private float healingBurstCooldown;

    [Tooltip("Prefab for the healing aura.")]
    [SerializeField] private GameObject healingAura;

    [SerializeField] private float[] healingSpeeds = new float[2];
    [SerializeField] private float healingAmount = 500;


    private void OnEnable()
    {
        healingBurstCooldown = maxHealingBurstCooldown;
    }

    void FixedUpdate()
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


    //While close to home charge a strong single-target projectile that does relatively more damage to armored targets
    public override void AbilityClose()
    {
        if(healingBurstCooldown > 0)
        {
            healingBurstCooldown -= Time.deltaTime;
            //slowly heal in a 3x3
        }
        else
        {
            //heal quickly in a 3x3
        }
    }

    //While far from home shoot weaker projectiles with higher DPS against un-armored targets
    public override void AbilityFar()
    {
        if(healingBurstCooldown <= 0)
        {
            //Spawn Healing Burst in a 3x3 area
            healingBurstCooldown = maxHealingBurstCooldown;
        }
        else
        {
            //Slowly heal in a 3x3 space
        }
    }

    private void Heal(float healAmount, Vector3 range)
    {
        GameObject newAura = ObjectPool.pool.Create(healingAura, transform.position);
        newAura.GetComponent<HealingAura>().healingAmount = healAmount;
    }
}
