using System.Collections.Generic;
using UnityEngine;

public class HealingAura : MonoBehaviour
{
    public float lifeTime;
    public float healingAmount;
    public bool burstHeal = false;

    private List<GameObject> objectsHit = new List<GameObject>();
    public GameObject parent;

    private void OnEnable()
    {
        objectsHit.Clear();
    }

    private void Update()
    {
        if(lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
            transform.position = parent.transform.position;
        }
        else
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }
    }


    private void OnTriggerStay(Collider hit)
    {
        if (hit.gameObject.TryGetComponent<TowerHealth>(out TowerHealth health))
        {
            if (burstHeal == true && hit.gameObject != parent)
            {
                if (!objectsHit.Find(x => x.name == hit.gameObject.name)) //For single burst heal actions
                {
                    health.RecoverHealth(healingAmount);
                }
            }
            else if(burstHeal == false && hit.gameObject != parent) //For gradual, healing-over-time effects
            {
                health.RecoverHealth(healingAmount);
            }
        }
    }
}
