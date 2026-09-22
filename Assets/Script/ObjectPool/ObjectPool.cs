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

    private void Start()
    {
        
    }

    public void Create(ref GameObject gameObject, Vector3 spawnPoint)
    {
        GameObject localGM = gameObject;
        if (pooledObjects.Find(x => localGM))
        {
            int selectedPooledObject = pooledObjects.FindIndex(x => localGM);
            pooledObjects[selectedPooledObject].SetActive(true);
            pooledObjects.RemoveAt(selectedPooledObject);
        }
        else
        {
            Instantiate(gameObject, spawnPoint, Quaternion.identity);
        }
    }

    public virtual void Destroy(ref GameObject gameObject)
    {
        pooledObjects.Add(gameObject);
        gameObject.SetActive(false);
    }
}
