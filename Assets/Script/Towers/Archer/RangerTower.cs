using UnityEngine;

public class RangerTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [Tooltip("How long the waiting period for shots for far ability should be.")]
    [SerializeField] private float maxFCooldown;

    [Tooltip("How long the waiting period for shots for the close ability should be.")]
    [SerializeField] private float maxCCooldown;

    private float farCooldown;
    private float closeCooldown;

    [Tooltip("Prefab for the far ability projectile.")]
    [SerializeField] private GameObject weakProjectilePrefab;

    [Tooltip("Prefab for the close ability projectile.")]
    [SerializeField] private GameObject strongProjectilePrefab;

    private TowerRelocate towerRelocate;

    private void Start()
    {
        towerRelocate = GetComponent<TowerRelocate>();
    }

    private void OnEnable()
    {
        farCooldown = maxFCooldown;
        closeCooldown = maxCCooldown;
    }

    void FixedUpdate()
    {
        int layerMask = 1 << 6;

        if (towerRelocate.isRelocating == false)
        {
            if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                if(hit.collider.tag == "Enemy")
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
            else
            {
                if (transform.position.x < swapLimit)
                {
                    ReduceCooldown(ref closeCooldown);
                }
            }
        }
    }


    //While close to home charge a strong single-target projectile that does relatively more damage to armored targets
    public override void AbilityClose()
    {
        if(closeCooldown <= 0)
        {
            closeCooldown = maxCCooldown;
            ObjectPool.pool.Create(strongProjectilePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            ReduceCooldown(ref closeCooldown);
        }
    }

    //While far from home shoot weaker projectiles with higher DPS against un-armored targets
    public override void AbilityFar()
    {
        //Fast Attack
        if (farCooldown <= 0)
        {
            farCooldown = maxFCooldown;
            ObjectPool.pool.Create(weakProjectilePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            ReduceCooldown(ref farCooldown);
        }
    }


    private void ReduceCooldown(ref float cooldown)
    {
        if(cooldown > 0)
        {
            cooldown -= Time.deltaTime;
        }
    }
}
