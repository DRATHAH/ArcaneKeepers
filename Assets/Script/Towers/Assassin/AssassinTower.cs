using System.Collections.Generic;
using UnityEngine;

public class AssassinTower : TowerBase
{
    [SerializeField] private float swapLimit = 0f;

    [Header("Backstab Vars")]
    [SerializeField] private float stabRange = 3f;
    [SerializeField] private float maxBackStabCooldown = 2f;
    [SerializeField] private float backStabCooldown;

    [Header("Backstab Damage Vars")]
    [SerializeField] private float stabDamage = 250;
    [SerializeField] private bool armorPiercing = false;


    [Header("Panic Attack Vars")]
    [SerializeField] private float panicAttackRange = 2.5f;
    private float panicAttackCooldown;
    [SerializeField] private float maxPanicAttackCooldown = 7f;
    [SerializeField] private float panicAttackDamage = 25;
    [SerializeField] private Vector3 panicAttackPush = Vector3.right;



    private TowerRelocate towerRelocate;
    private MeleeAttack meleeAttack;

    private int layerMask = 1 << 6;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        towerRelocate = GetComponent<TowerRelocate>();
        meleeAttack = GetComponent<MeleeAttack>();
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
        if (Physics.Linecast(transform.position, new Vector3(transform.position.x + panicAttackRange, transform.position.y, transform.position.z), out RaycastHit hit, layerMask))
        {
            if (hit.collider.tag == "Enemy" && panicAttackCooldown <= 0)
            {
                meleeAttack.Attack(panicAttackDamage, false, panicAttackPush, hit.collider.gameObject);
                panicAttackCooldown = maxPanicAttackCooldown;
            }
        }
        else
        {
            BackStab();
        }

        if(panicAttackCooldown > 0)
        {
            panicAttackCooldown -= 0;
        }
    }

    public override void AbilityFar()
    {

        BackStab();
    }

    void BackStab()
    {
        if (backStabCooldown <= 0f)
        {
            if (Physics.Linecast(transform.position, new Vector3(transform.position.x - stabRange, transform.position.y, transform.position.z), out RaycastHit hit, layerMask))
            {
                if (hit.collider.tag == "Enemy")
                {
                    Debug.Log(hit.collider.gameObject);
                    meleeAttack.Attack(stabDamage, armorPiercing, hit.collider.gameObject);
                    backStabCooldown = maxBackStabCooldown;
                }
            }
        }
        else
        {
            backStabCooldown -= Time.deltaTime;
        }
    }
}
