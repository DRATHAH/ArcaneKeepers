using System.Collections.Generic;
using UnityEngine;

public class HealingAura : MonoBehaviour
{
    [HideInInspector] public float lifeTime;
    [HideInInspector] public float healingAmount;
    [HideInInspector] public bool burstHeal = false;

    private List<GameObject> objectsHit = new List<GameObject>();
    private List<float> objectTimers = new List<float>();
    private float maxObjectTimer = 0.1f;

    private List<GameObject> healingVFX = new List<GameObject>();

    [HideInInspector] public GameObject parent;

    private GameObject healingVFXPrefab;

    private void OnEnable()
    {
        objectsHit.Clear();
    }

    private void OnDisable()
    {
        foreach(GameObject obj in healingVFX)
        {
            ObjectPool.pool.Destroy(obj);
        }
        healingVFX.Clear();
        objectTimers.Clear();
    }

    private void Update()
    {
        if(lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
        }
        else
        {
            ObjectPool.pool.Destroy(this.gameObject);
        }

        for(int i = 0; i < objectTimers.Count; i++)
        {
            if (objectTimers[i] > 0)
            {
                objectTimers[i] -= Time.deltaTime;
            }
            else
            {
                //ObjectPool.pool.Destroy(healingVFX[i]);
                healingVFX.RemoveAt(i);
                objectTimers.RemoveAt(i);
                objectsHit.RemoveAt(i);
            }
        }

    }


    private void OnTriggerStay(Collider hit)
    {
        if (hit.gameObject.TryGetComponent<TowerHealth>(out TowerHealth health) && hit.gameObject != parent)
        {
            if (!objectsHit.Find(x => x.name == hit.gameObject.name)) //For single burst heal actions
            {
                objectsHit.Add(hit.gameObject);

                if (burstHeal == true)
                {
                    health.RecoverHealth(healingAmount);
                }

                CreateVFX(hit.transform.position);
            }
            else
            {
                int selectedObject = objectsHit.FindIndex(x => x.name == hit.gameObject.name);

                objectTimers[selectedObject] = maxObjectTimer;
            }


            if (burstHeal == false) //For gradual, healing-over-time effects
            {
                health.RecoverHealth(healingAmount);
                
            }
        }
    }

    private void CreateVFX(Vector3 origin)
    {
        GameObject newbie = ObjectPool.pool.Create(healingVFXPrefab, origin, Quaternion.identity);
        healingVFX.Add(newbie);
        objectTimers.Add(maxObjectTimer);
    }
}
