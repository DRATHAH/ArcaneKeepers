using System.Collections.Generic;
using UnityEngine;

public class HealingAura : MonoBehaviour
{
    private float maxLifeTime = 1;
    private float lifeTime;
    [HideInInspector] public float healingAmount;
    public bool burstHeal = false;

    private List<GameObject> objectsHit = new List<GameObject>();

    private void OnEnable()
    {
        lifeTime = maxLifeTime;
    }

    private void Update()
    {
        if(lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
        }
    }


    private void OnTriggerEnter(Collider hit)
    {
        if (hit.gameObject.TryGetComponent<TowerHealth>(out TowerHealth health))
        {
            if (burstHeal == true)
            {
                if (!objectsHit.Find(x => x.name == hit.gameObject.name)) //For single burst heal actions
                {
                    health.RecoverHealth(healingAmount);
                }
            }
            else if(burstHeal == false) //For gradual, healing-over-time effects
            {
                health.RecoverHealth(healingAmount);
            }
        }

        if (lifeTime <= 0)
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }
    }
}
