using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyDamage : DamageBase
{
    [Tooltip("How much damage this enemy does.")]
    [SerializeField] private float damageAmount;

    [Tooltip("Does the enemy do a big burst of damage as opposed to constant damage?")]
    [SerializeField] private bool smash;
    
    private IMovement enemyMove;
    [HideInInspector]public bool attacking = false;

    [Tooltip("Animator for the enemy.")]
    [SerializeField] private Animator anim;

    [Header("Smash Variables")]
    [Tooltip("Length of time the windup period of the attack lasts for.")]
    [SerializeField] private float windupDuration = 1f;
    [SerializeField] private float cooldownDuration = 1f;
    public GameObject target;
    private IEnumerator smashSequence;


    private void Start()
    {
        try
        {
            enemyMove = GetComponent<IMovement>();
        }
        catch
        {
            Debug.LogWarning("This enemy has no movement!");
        }
    }

    private void OnTriggerStay(Collider hit)
    {
        if (hit.tag == "Tower")
        {
            if(hit.TryGetComponent<TowerRelocate>(out TowerRelocate relocator))
            {
                if(relocator.isRelocating == false && enemyMove.pushed == false)
                {
                    if(smash == false)
                    {
                        StandardAttack(hit.gameObject);
                    }
                    else
                    {
                        if(target == null)
                        {
                            target = hit.gameObject;
                        }
                        if (smashSequence == null)
                        {
                            smashSequence = Smash();
                            StartCoroutine(smashSequence);
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider hit)
    {
        if (hit.TryGetComponent<TowerRelocate>(out TowerRelocate relocator))
        {
                target = null;
        }
    }

    private void StandardAttack(GameObject victim)
    {
        Attack(damageAmount * Time.deltaTime, victim);
        anim.SetBool("Attacking", true);
        enemyMove.PauseMovement();
        //Debug.Log(damageAmount * Time.deltaTime);
    }


    IEnumerator Smash()
    {
        anim.SetBool("Attacking", true);
        enemyMove.PauseMovement();
        yield return new WaitForSeconds(windupDuration);
        anim.SetTrigger("Smash");
        if(target != null)
        {
            Attack(damageAmount, target);
        }
        if(this.gameObject.TryGetComponent<IAttackAbility>(out IAttackAbility attackAbility))
        {
            attackAbility.AttackAbility();
        }
        yield return new WaitForSeconds(cooldownDuration);
        smashSequence = null;
        target = null;
    }
}
