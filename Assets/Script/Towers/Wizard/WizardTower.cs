using UnityEngine;

public class WizardTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [Tooltip("How long the waiting period for shots for far ability should be.")]
    [SerializeField] private float maxFCooldown;

    [Tooltip("How long the waiting period for shots for the close ability should be.")]
    [SerializeField] private float maxCCooldown;

    [SerializeField] private float fireOffset;

    private float farCooldown;
    private float closeCooldown;

    [Tooltip("Prefab for the far ability projectile.")]
    [SerializeField] private GameObject farProjectile;

    [Tooltip("Prefab for the close ability projectile.")]
    [SerializeField] private GameObject closeProjectile;
    private TowerRelocate towerRelocate;

    [SerializeField] private float fireWaveRange = 3;

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
        if(towerRelocate.isRelocating == false)
        {
            if (transform.position.x < swapLimit)
            {
                if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hit))
                {
                    if (hit.collider.tag == "Enemy")
                    {
                        AbilityClose();
                    }
                }
            }
            else
            {
                RaycastHit hit;
                if (Physics.Linecast(transform.position, new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z), out hit)
                    || Physics.Linecast(new Vector3(transform.position.x, transform.position.y, transform.position.z + fireOffset), new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z + fireOffset), out hit)
                    || Physics.Linecast(new Vector3(transform.position.x, transform.position.y, transform.position.z - fireOffset), new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z - fireOffset), out hit))
                {
                    if (hit.collider.tag == "Enemy")
                    {
                        AbilityFar();
                    }
                }
            }
        }
    }


    //While close to home charge a strong single-target projectile that does relatively more damage to armored targets
    public override void AbilityClose()
    {
        if (closeCooldown <= 0)
        {
            closeCooldown = maxCCooldown;
            ObjectPool.pool.Create(closeProjectile, transform.position, transform.rotation);
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
            ObjectPool.pool.Create(farProjectile, new Vector3(transform.position.x, transform.position.y, transform.position.z + fireOffset), transform.rotation);
            ObjectPool.pool.Create(farProjectile, transform.position, transform.rotation);
            ObjectPool.pool.Create(farProjectile, new Vector3(transform.position.x, transform.position.y, transform.position.z - fireOffset), transform.rotation);
        }
        else
        {
            ReduceCooldown(ref farCooldown);
        }
    }


    private void ReduceCooldown(ref float cooldown)
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z));
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, transform.position.z - fireOffset), new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z - fireOffset));
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, transform.position.z + fireOffset), new Vector3(transform.position.x + fireWaveRange, transform.position.y, transform.position.z + fireOffset));
    }
}
