using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    Rigidbody rb;

    private enum Status
    {
        ALIVE,
        DEAD
    };

    private Status livingStatus;

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
        if(enemyHealth != null)
        {
            if(enemyHealth.currentHealth > 0 && livingStatus != Status.ALIVE)
            {
                livingStatus = Status.ALIVE;
            }
            else if(enemyHealth.currentHealth <= 0 && livingStatus != Status.DEAD)
            {
                livingStatus = Status.DEAD;
            }
        }
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
}
