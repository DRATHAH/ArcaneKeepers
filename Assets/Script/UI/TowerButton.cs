using UnityEngine;
using UnityEngine.InputSystem;

public class TowerButton : MonoBehaviour
{
    public GameObject towerPlacerPrefab;
    public Tower tower;

    public GameObject createdObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(Tower newTower)
    {
        tower = newTower;
    }

    // Creates the tower icon when you drag from the image
    public void CreateTower()
    {
        if(createdObject == null || createdObject.activeSelf == false)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            GameObject towerPlacer = ObjectPool.pool.Create(towerPlacerPrefab, touchPos, Quaternion.identity);
            TowerPlacer placer = towerPlacer.GetComponent<TowerPlacer>();
            placer.Initialize(tower);
            placer.towerButton = this;
            towerPlacer.transform.SetParent(transform.root, true);
        }
    }
}
