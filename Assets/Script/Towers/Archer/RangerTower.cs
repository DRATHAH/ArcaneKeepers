using UnityEngine;

public class RangerTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [SerializeField] private float maxFCooldown;
    [SerializeField] private float maxCCooldown;

    private float farCooldown;
    private float closeCooldown;

    [SerializeField] private GameObject strongProjectilePrefab;

    void Update()
    {
        if(transform.position.x < swapLimit)
        {
            AbilityClose();
        }
        else
        {
            AbilityFar();
        }
    }


    //While close to home charge a strong single-target projectile (More damage than up close)
    public override void AbilityClose()
    {
        if(closeCooldown <= 0)
        {
            closeCooldown = maxCCooldown;
            ObjectPool.pool.Create(ref strongProjectilePrefab, transform.position);
        }
        else
        {
            closeCooldown -= Time.deltaTime;
        }
    }

    //While far from home shoot weaker projectiles with slightly more defense
    public override void AbilityFar()
    {
        //DamageResist ++

        //Fast Attack
        if(farCooldown <= 0)
        {
            //Shoot Fast Attack Projectile
        }
    }
}
