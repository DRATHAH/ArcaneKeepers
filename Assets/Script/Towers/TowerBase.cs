using System.Collections.Generic;
using UnityEngine;

public class TowerBase : MonoBehaviour
{
    public TowerEntry tower;

    [Tooltip("List of lanes tower can hit (Ex: -1 = lane below, 1 = lane above)")]
    public List<int> lanes = new List<int>();
    public string attackSoundName;

    TowerRelocate relocate;
    float timeSinceAttack = 0;
    bool canAttack = true;

    // Awake, not Start: every tower subclass declares its own Start(), which hides this one,
    // so towerRelocate stayed null and FixedUpdate threw every physics tick.
    private void Awake()
    {

    }

    private void Start()
    {
        relocate = GetComponent<TowerRelocate>();
    }

    public virtual void AbilityClose()
    {
        DoAttack(tower.tower.closeAttack);
        Debug.Log(tower.tower.name + " is close to Home");
    }

    public virtual void AbilityFar()
    {
        DoAttack(tower.tower.farAttack);
        Debug.Log(tower.tower.name + " is far from Home");
    }

    public void DoAttack(Attack attackType)
    {
        SoundManager.Instance.PlaySound(attackSoundName);
        
        switch (attackType.attackType)
        {
            case Attack.AttackType.Damage:
                if (attackType.lanes.Length == 0)
                {
                    GameObject attack = Attack.CreateAttack(attackType.attackPrefab, transform.position, transform.rotation);
                    attack.GetComponent<TPDamage>().Initialize(attackType.damage, tower.level, tower.tower.levelScaling, attackType.special);
                }
                else
                {
                    foreach (int i in attackType.lanes)
                    {
                        Vector3 projPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z + i * 2);
                        GameObject attack = Attack.CreateAttack(attackType.attackPrefab, projPosition, transform.rotation);
                        attack.GetComponent<TPDamage>().Initialize(attackType.damage, tower.level, tower.tower.levelScaling, attackType.special);
                    }
                }
                break;

            case Attack.AttackType.Healing:
                GameObject healing = Attack.CreateHeal(attackType.attackPrefab, transform);
                healing.GetComponent<HealingAura>().Initialize(attackType.damage, tower.level, tower.tower.levelScaling, attackType.special, attackType.range, attackType.attackRate);
                break;

            default:
                break;

        }

        timeSinceAttack += attackType.attackRate;
    }

    private void FixedUpdate()
    {
        int enemyLayer = 1 << 6;
        float range = tower.tower.farAttack.range;

        if (transform.position.x < tower.tower.farLimit)
        {
            range = tower.tower.closeAttack.range;
        }


        if (relocate.isRelocating == false && canAttack)
        {
            if (transform.position.x < 0)
            {
                // If close attack heals, don't require an enemy
                if (tower.tower.closeAttack.attackType == Attack.AttackType.Healing)
                {
                    AbilityClose();
                }
                else if (tower.tower.closeAttack.attackType == Attack.AttackType.Damage)
                {
                    if (lanes.Count == 0)
                    {
                        if (Physics.Linecast(transform.position, new Vector3(transform.position.x + transform.right.x * range, transform.position.y, transform.position.z), out RaycastHit hit, enemyLayer))
                        {
                            if (hit.collider.tag == "Enemy")
                            {
                                AbilityClose();
                            }
                        }
                    }
                    else
                    {
                        foreach (int i in lanes)
                        {
                            if (Physics.Linecast(transform.position, new Vector3(transform.position.x + transform.right.x * range, transform.position.y, transform.position.z + i * 2), out RaycastHit hit, enemyLayer))
                            {
                                if (hit.collider.tag == "Enemy")
                                {
                                    AbilityClose();
                                }
                            }
                        }
                    }
                }

                canAttack = false;
            }
            else
            {
                // If far attack heals, don't require an enemy
                if (tower.tower.farAttack.attackType == Attack.AttackType.Healing)
                {
                    AbilityFar();
                }
                else if (tower.tower.farAttack.attackType == Attack.AttackType.Damage)
                {
                    if (Physics.Linecast(transform.position, new Vector3(transform.position.x + transform.right.x * range, transform.position.y, transform.position.z), out RaycastHit hit, enemyLayer))
                    {
                        if (hit.collider.tag == "Enemy")
                        {
                            AbilityFar();
                        }
                    }
                }

                canAttack = false;
            }
        }

        if (timeSinceAttack > 0)
        {
            timeSinceAttack -= Time.deltaTime;
        }
        else if (!canAttack)
        {
            canAttack = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + transform.right.x * tower.tower.farAttack.range, transform.position.y, transform.position.z));
    }
}
