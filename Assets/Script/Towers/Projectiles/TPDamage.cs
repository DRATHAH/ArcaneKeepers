using System;
using System.Collections.Generic;
using UnityEngine;

public class TPDamage : DamageBase
{
    [Tooltip("How much damage a projectile does.")]
    [SerializeField] private float damageAmount;

    [Tooltip("Number of enemies this projectile can hit before disappearing.")]
    [SerializeField] private int hitCount = 1;

    private List<GameObject> objectsHit = new List<GameObject>();

    [Tooltip("Does this projectile pierce enemy armor?")]
    [SerializeField] private bool armorPiercing = false;

    private void OnEnable()
    {
        objectsHit.Clear();
    }


    private void OnTriggerEnter2D(Collider2D hit)
    {
        if(hit.tag == "Enemy" && !objectsHit.Find(x => hit.gameObject))
        {
            Attack(damageAmount, armorPiercing, hit.gameObject);
            hitCount--;
            objectsHit.Add(hit.gameObject);
        }

        if (hitCount <= 0)
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }
    }
}
