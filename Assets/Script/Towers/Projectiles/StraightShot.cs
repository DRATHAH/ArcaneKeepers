using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StraightShot : MonoBehaviour
{
    Rigidbody rb;

    [Tooltip("How fast the projectile moves.")]
    [SerializeField] private float projectileSpeed = 1;

    [Tooltip("How long the projectile exists for. Set to 999 for infinite existance.")]
    [SerializeField] private float maxLifeTime = 999;
    private float lifeTime;

    void OnEnable()
    {
        lifeTime = maxLifeTime;
        if(rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        rb.linearVelocity = new Vector2(projectileSpeed, 0);
    }

    void Update()
    {
        if(lifeTime > 0 && lifeTime < 999)
        {
            lifeTime -= Time.deltaTime;
        }

        if(lifeTime <= 0)
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }

    }
}
