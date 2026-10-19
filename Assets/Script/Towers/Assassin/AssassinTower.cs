using System.Collections.Generic;
using UnityEngine;

public class AssassinTower : TowerBase
{
    [Tooltip("Dividing line for ability change.")]
    [SerializeField] private float swapLimit = 0f;

    [Header("Backstab Vars")]
    [Tooltip("Range the enemy can be hit from with the backstab")]
    [SerializeField] private float stabRange = 3f;

    [Tooltip("Maximum cooldown for the backstab")]
    [SerializeField] private float maxBackStabCooldown = 2f;

    private float backStabCooldown; //Active cooldown for the backstab

    [Header("Backstab Damage Vars")]
    [Tooltip("Damage values for the backstab. 0 = Close Position, 1 = Far Position")]
    [SerializeField] private float[] stabDamage = new float[2];

    [Tooltip("Can the backstab pierce enemy armor?")]
    [SerializeField] private bool armorPiercing = false; //Can the backstab pierce armor?


    [Header("Panic Attack Vars")]
    [Tooltip("How far away the assassin can activate the panic attack ability from.")]
    [SerializeField] private float panicAttackRange = 2.5f;
    private float panicAttackCooldown;//Active cooldown for the panic attack

    [Tooltip("Maximum cooldown for the panic attack")]
    [SerializeField] private float maxPanicAttackCooldown = 7f;

    [Tooltip("Damage dealt by the panic attack")]
    [SerializeField] private float panicAttackDamage = 25;

    [Tooltip("The velocity that the enemy will be pushed with.")]
    [SerializeField] private Vector3 panicAttackPush = Vector3.right;


    [Header("Damage Resist Vars")]
    [Tooltip("Determines what the damage resistances for this tower will be set to. Close to base = 0, Far from base = 1.")]
    [SerializeField] private float[] damageResists = { 1f, 1.25f };


    //Helper scripts
    private TowerRelocate towerRelocate;
    private MeleeAttack meleeAttack;
    private TowerHealth towerHealth;

    private Animator anim;

    private int layerMask = 1 << 6; //Layer mask for raycast purposes

    void Start()
    {
        towerHealth = GetComponent<TowerHealth>();
        towerRelocate = GetComponent<TowerRelocate>();
        meleeAttack = GetComponent<MeleeAttack>();
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        if (towerRelocate.isRelocating == false)
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
    }


    public override void AbilityClose()
    {
        if (towerHealth.damageResist != damageResists[0])
        {
            towerHealth.damageResist = damageResists[0];
        }

        if (Physics.Linecast(transform.position, new Vector3(transform.position.x + panicAttackRange, transform.position.y, transform.position.z), out RaycastHit hit, layerMask))
        {
            if (hit.collider.tag == "Enemy" && panicAttackCooldown <= 0 && hit.collider.gameObject.activeSelf == true)
            {
                anim.SetTrigger("Panic");
                meleeAttack.Attack(panicAttackDamage * (1 + 1.1f * (tower.level - 1)), false, panicAttackPush, hit.collider.gameObject);
                panicAttackCooldown = maxPanicAttackCooldown;
            }
        }
        else
        {
            BackStab(0);
        }

        if(panicAttackCooldown > 0)
        {
            panicAttackCooldown -= 0;
        }
    }

    public override void AbilityFar()
    {
        if (towerHealth.damageResist != damageResists[1])
        {
            towerHealth.damageResist = damageResists[1];
        }

        BackStab(1);
    }

    void BackStab(int stabDamageType)
    {
        if (backStabCooldown <= 0f)
        {
            if (Physics.Linecast(transform.position, new Vector3(transform.position.x - stabRange, transform.position.y, transform.position.z), out RaycastHit hit, layerMask))
            {
                if (hit.collider.tag == "Enemy" && hit.collider.gameObject.activeSelf == true)
                {
                    SoundManager.Instance.PlaySound("Stab");
                    backStabCooldown = maxBackStabCooldown;
                    anim.SetTrigger("Stab");
                    meleeAttack.Attack(stabDamage[stabDamageType] * Mathf.Clamp(1 + levelScaling * (tower.level - 1), 1, float.MaxValue), armorPiercing, hit.collider.gameObject);
                }
            }
        }
        else
        {
            backStabCooldown -= Time.deltaTime;
        }
    }
}
