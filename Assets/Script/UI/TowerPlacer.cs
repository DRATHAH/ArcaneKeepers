using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TowerPlacer : MonoBehaviour
{
    public Tower tower;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            transform.position = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (TouchManager.instance.hoveringTile)
        {
            Debug.Log("Placed");
            TouchManager.instance.hoveringTile.Initialize(tower.towerPrefab);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Tower towerToPlace)
    {
        tower = towerToPlace;
    }
}
