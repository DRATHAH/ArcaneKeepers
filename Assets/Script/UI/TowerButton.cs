using UnityEngine;
using UnityEngine.InputSystem;

public class TowerButton : MonoBehaviour
{
    public GameObject towerPlacerPrefab;
    public Tower tower;

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
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        GameObject towerPlacer = Instantiate(towerPlacerPrefab, touchPos, Quaternion.identity);
        TowerPlacer placer = towerPlacer.GetComponent<TowerPlacer>();
        placer.Initialize(tower);
        towerPlacer.transform.SetParent(transform.root, true);
    }
}
