using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool pool { get; private set; }

    private List<GameObject> pooledObjects = new List<GameObject>();

    private GameObject poolParent;

    private void Awake()
    {
        if(pool != null && pool != this)
        {
            Destroy(pool);
        }
        else
        {
            pool = this;
        }
    }

    //Func for creating new objects
    public GameObject Create(GameObject objectType, Vector3 spawnPoint, Quaternion rotation)
    {
        string localObject = objectType.name;
        
        //Check if the object is in the object pools
        if (pooledObjects.Find(x => x.name == objectType.name))
        {
            // Find where the object is in the pooledObjects list
            int selectedPooledObject = pooledObjects.FindIndex(x => x.name == objectType.name);
            GameObject newbie = pooledObjects[selectedPooledObject];
            newbie.SetActive(true); //Set the object active
            newbie.transform.position = spawnPoint;
            pooledObjects.RemoveAt(selectedPooledObject); //Remove it from the list
            return newbie;
        }
        else
        {
            //If there are no objects available in the pool create a new one.
            GameObject newbie = Instantiate(objectType, spawnPoint, rotation);
            newbie.name = objectType.name;
            return newbie;
        }
    }

    //Func for destroying objects
    public virtual GameObject Destroy(GameObject gameObject)
    {
        pooledObjects.Add(gameObject); //Add object to pool
        Debug.Log(pooledObjects.Count);
        gameObject.SetActive(false); //Disable object
        return gameObject;
    }
}
