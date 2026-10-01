using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
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

    private void OnEnable()
    {
        try
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }
        catch
        {
            throw new ArgumentException("Unable to find Enemy Health Component!", nameof(EnemyMovement));
        }

        rb = GetComponent<Rigidbody>();
        CalcMovement();
    }

    private void Update()
    {
        if(moveDelay > 0)
        {
            moveDelay -= Time.deltaTime;
        }

        if (enemyHealth != null)
        {
            if(enemyHealth.currentHealth > 0 && moveDelay <= 0)
            {
                if(livingStatus != Status.ALIVE)
                {
                    livingStatus = Status.ALIVE;
                }
            }
            else
            {
                if(livingStatus != Status.STOPPED)
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
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void PauseMovement()
    {
        moveDelay = maxMoveDelay;
    }
}
