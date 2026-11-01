using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoppingEnemy : MonoBehaviour, IMovement
{
    Rigidbody rb;

    [HideInInspector]
    public enum Status
    {
        ALIVE,
        STOPPED
    };

    public Status livingStatus;

    [SerializeField] private float maxMoveDelay = 0.5f;
    [SerializeField] private float moveDelay;

    [Header("Hop Variables")]
    [Tooltip("How fast the enemy moves. Negative number moves left, positive number moves right")]
    [SerializeField] private float moveSpeed;

    [SerializeField] private float moveDuration = 2;
    [SerializeField] private float hopDelay = 2f;
    [SerializeField] private float maxHopDelay = 2f;
    private float windupLength = 0.375f;


    private bool hopping = false;

    private IEnumerator hopCoroutine = null;

    private EnemyHealth enemyHealth;

    public bool pushing = false;
    private float pushedCooldown = 0.25f;

    [HideInInspector] public bool pushed
    {
        get
        {
            return pushing;
        }
    }
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

        pushing = false;
        hopDelay = maxHopDelay;
        hopping = false;
        rb = GetComponent<Rigidbody>();
        //CalcMovement();
    }

    private void OnDisable()
    {
        if(hopCoroutine != null)
        {
            StopCoroutine(hopCoroutine);
            hopCoroutine = null;
        }
    }

    private void Update()
    {
        if (moveDelay > 0)
        {
            moveDelay -= Time.deltaTime;
        }
        else
        {
            anim.SetBool("Attacking", false);
            pushing = false;
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

        Hop();
        CalcMovement();
    }

    void CalcMovement()
    {
        if (livingStatus == Status.ALIVE && hopping)
        {
            rb.linearVelocity = Vector2.left * moveSpeed;
            anim.SetFloat("Movement", moveSpeed);
        }
        else
        {
            if (!pushing)
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

    private void Hop()
    {
        if(hopCoroutine == null)
        {
            if(hopDelay <= 0)
            {
                hopCoroutine = moveTimer();
                StartCoroutine(hopCoroutine);
            }
            else
            {
                hopDelay -= Time.deltaTime;
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
        pushing = true;
        rb.linearVelocity = pushForce;
    }
    public void Push(Vector3 pushForce, bool invincible)
    {
        moveDelay = pushedCooldown;
        pushing = true;
        rb.linearVelocity = pushForce;
        if (enemyHealth != null)
        {
            enemyHealth.ToggleInvincible(invincible);
        }
    }
    private IEnumerator moveTimer()
    {
        anim.SetTrigger("Hop");
        yield return new WaitForSeconds(windupLength);
        hopping = true;
        yield return new WaitForSeconds(moveDuration);
        hopDelay = maxHopDelay;
        hopping = false;
        hopCoroutine = null;
    }
}
