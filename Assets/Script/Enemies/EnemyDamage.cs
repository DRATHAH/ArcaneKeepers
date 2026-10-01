using UnityEngine;

public class EnemyDamage : DamageBase
{

    [Tooltip("How much damage this enemy does.")]
    [SerializeField] private float damageAmount;
    private EnemyMovement enemyMove;
    [HideInInspector]public bool attacking = false;

    private void Start()
    {
        try
        {
            enemyMove = GetComponent<EnemyMovement>();
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
            Attack(damageAmount * Time.deltaTime, hit.gameObject);
            enemyMove.PauseMovement();
        }
    }
}
