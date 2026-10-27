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

    private float mapBorder = 11f;

    private TowerRelocate towerRelocate;

    private Animator anim;

    private void Start()
    {
        towerRelocate = GetComponent<TowerRelocate>();
        anim = GetComponentInChildren<Animator>();
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
            if (Physics.Linecast(transform.position, new Vector3(mapBorder, transform.position.y, transform.position.z), out RaycastHit hit, layerMask))
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
                    anim.SetBool("Charging", true);
                    anim.SetFloat("ChargeAmount", closeCooldown/ maxCCooldown);
                    ReduceCooldown(ref closeCooldown);
                }
            }
        }
    }


    //While close to home charge a strong single-target projectile that does relatively more damage to armored targets
    public override void AbilityClose()
    {
        anim.SetBool("Charging", true);
        if (closeCooldown <= 0)
        {
            SoundManager.Instance.PlaySound("Arrow");
            anim.SetTrigger("Shoot");
            closeCooldown = maxCCooldown;
            GameObject projectile = ObjectPool.pool.Create(strongProjectilePrefab, transform.position, Quaternion.identity);
            projectile.GetComponent<TPDamage>().Initialize(tower.level, levelScaling);
        }
        else
        {
            anim.SetFloat("ChargeAmount", closeCooldown / maxCCooldown);
            ReduceCooldown(ref closeCooldown);
        }
    }

    //While far from home shoot weaker projectiles with higher DPS against un-armored targets
    public override void AbilityFar()
    {
        //Fast Attack
        anim.SetBool("Charging", false);
        if (farCooldown <= 0)
        {
            SoundManager.Instance.PlaySound("Arrow");
            anim.SetTrigger("Shoot");
            farCooldown = maxFCooldown;
            GameObject projectile = ObjectPool.pool.Create(weakProjectilePrefab, transform.position, Quaternion.identity);
            projectile.GetComponent<TPDamage>().Initialize(tower.level, levelScaling);
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
