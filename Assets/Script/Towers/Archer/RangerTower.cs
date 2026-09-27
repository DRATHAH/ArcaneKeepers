using UnityEngine;

public class RangerTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [SerializeField] private float maxFCooldown;
    [SerializeField] private float maxCCooldown;

    private float farCooldown;
    private float closeCooldown;

    [SerializeField] private GameObject weakProjectilePrefab;
    [SerializeField] private GameObject strongProjectilePrefab;

    private void OnEnable()
    {
        farCooldown = maxFCooldown;
        closeCooldown = maxCCooldown;
    }

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


    //While close to home charge a strong single-target projectile that does relatively more damage to armored targets
    public override void AbilityClose()
    {
        if(closeCooldown <= 0)
        {
            closeCooldown = maxCCooldown;
            ObjectPool.pool.Create(strongProjectilePrefab, transform.position);
        }
        else
        {
            closeCooldown -= Time.deltaTime;
        }
    }

    //While far from home shoot weaker projectiles with higher DPS against un-armored targets
    public override void AbilityFar()
    {
        //Fast Attack
        if (farCooldown <= 0)
        {
            farCooldown = maxFCooldown;
            ObjectPool.pool.Create(weakProjectilePrefab, transform.position);
        }
        else
        {
            farCooldown -= Time.deltaTime;
        }
    }
}
