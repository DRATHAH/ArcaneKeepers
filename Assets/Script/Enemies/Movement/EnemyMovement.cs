using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour, IMovement
{
    Rigidbody rb;

    [HideInInspector] public enum Status
    {
        ALIVE,
        STOPPED
    };

    public Status livingStatus;

    [SerializeField] private float maxMoveDelay = 0.5f;
    [SerializeField] private float moveDelay;

    [Tooltip("How fast the enemy moves. Negative number moves left, positive number moves right")]
    [SerializeField] private float moveSpeed;

    private EnemyHealth enemyHealth;

    public bool pushed = false;
    private float pushedCooldown = 0.25f;

    [SerializeField] private Animator anim;

    private void Start()
    {
        try
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }
        catch
        {
            throw new ArgumentException("Unable to find Enemy Health Component!", nameof(EnemyMovement));
        }
    }

    private void OnEnable()
    {

        pushed = false;

        rb = GetComponent<Rigidbody>();
        CalcMovement();

    }

    private void Update()
    {
        if(moveDelay > 0)
        {
            moveDelay -= Time.deltaTime;
        }
        else
        {
            anim.SetBool("Attacking", false);
            pushed = false;
        }

        if (enemyHealth != null)
        {
            if (enemyHealth.currentHealth > 0 && moveDelay <= 0)
            {
                if (livingStatus != Status.ALIVE)
                {
                    livingStatus = Status.ALIVE;
                }
            }
            else
            {
                if (livingStatus != Status.STOPPED)
                {
                    livingStatus = Status.STOPPED;
                }
            }
        }

        CalcMovement();
    }

    void CalcMovement()
    {
        if(livingStatus == Status.ALIVE)
        {
            rb.linearVelocity = Vector2.left * moveSpeed;
            anim.SetFloat("Movement", moveSpeed);
            if (enemyHealth != null)
            {
                enemyHealth.ToggleInvincible(false);
            }
        }
        else
        {
            if (!pushed)
            {
                rb.linearVelocity = Vector2.zero;
                anim.SetFloat("Movement", 0);
                if (enemyHealth != null)
                {
                    enemyHealth.ToggleInvincible(false);
                }
            }
        }
    }

    public void PauseMovement()
    {
        moveDelay = maxMoveDelay;
    }

    public void Push(Vector3 pushForce)
    {
        moveDelay = pushedCooldown;
        pushed = true;
        rb.linearVelocity = pushForce;
    }

    public void Push(Vector3 pushForce, bool invincible)
    {
        moveDelay = pushedCooldown;
        pushed = true;
        rb.linearVelocity = pushForce;
        enemyHealth.ToggleInvincible(invincible);
    }
}
