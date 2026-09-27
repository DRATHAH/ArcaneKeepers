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
    public void Create(GameObject objectType, Vector3 spawnPoint)
    {
        string localObject = objectType.name;
        
        //Check if the object is in the object pools
        if (pooledObjects.Find(x => x.name == objectType.name))
        {
            // Find where the object is in the pooledObjects list
            int selectedPooledObject = pooledObjects.FindIndex(x => x.name == objectType.name);
            pooledObjects[selectedPooledObject].SetActive(true); //Set the object active
            pooledObjects[selectedPooledObject].transform.position = spawnPoint;
            pooledObjects.RemoveAt(selectedPooledObject); //Remove it from the list
        }
        else
        {
            //If there are no objects available in the pool create a new one.
            GameObject newbie = Instantiate(objectType, spawnPoint, Quaternion.identity);
            newbie.name = objectType.name;
        }
    }

    //Func for destroying objects
    public virtual void Destroy(GameObject gameObject)
    {
        pooledObjects.Add(gameObject); //Add object to pool
        gameObject.SetActive(false); //Disable object
    }
}
