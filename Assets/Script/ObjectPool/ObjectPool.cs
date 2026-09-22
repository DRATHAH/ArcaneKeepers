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
    public void Create(ref GameObject gameObject, Vector3 spawnPoint)
    {
        GameObject localGM = gameObject;
        
        //Check if the object is in the object pools
        if (pooledObjects.Find(x => localGM))
        {
            // Find where the object is in the pooledObjects list
            int selectedPooledObject = pooledObjects.FindIndex(x => localGM);
            pooledObjects[selectedPooledObject].SetActive(true); //Set the object active
            pooledObjects.RemoveAt(selectedPooledObject); //Remove it from the list
        }
        else
        {
            //If there are no objects available in the pool create a new one.
            Instantiate(gameObject, spawnPoint, Quaternion.identity);
        }
    }

    //Func for destroying objects
    public virtual void Destroy(ref GameObject gameObject)
    {
        pooledObjects.Add(gameObject); //Add object to pool
        gameObject.SetActive(false); //Disable object
    }
}
