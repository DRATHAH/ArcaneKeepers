using System;
using System.Collections.Generic;
using UnityEngine;

public class TPDamage : DamageBase
{
    [Tooltip("How much damage a projectile does.")]
    [SerializeField] private float damageAmount;

    [Tooltip("Number of enemies this projectile can hit before disappearing.")]
    [SerializeField] private int maxhitCount = 1;
    private int hitCount;

    [SerializeField]private List<GameObject> objectsHit = new List<GameObject>();

    [Tooltip("Does this projectile pierce enemy armor?")]
    [SerializeField] private bool armorPiercing = false;

    private void OnEnable()
    {
        hitCount = maxhitCount;
        objectsHit.Clear();
    }


    private void OnTriggerEnter(Collider hit)
    {
        if(hit.tag == "Enemy")
        {
            if(!objectsHit.Find(x => x.gameObject.GetEntityId() == hit.gameObject.GetEntityId()))
            {
                objectsHit.Add(hit.gameObject);
                Attack(damageAmount, armorPiercing, hit.gameObject);
                hitCount--;
            }
        }

        if (hitCount <= 0)
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }
    }
}
