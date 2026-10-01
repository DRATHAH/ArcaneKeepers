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
        // Check if player is touching screen
        if (Touch.activeTouches.Count > 0)
        {
            // Get position of touch
            transform.position = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (TouchManager.instance.hoveringTile) // If touch ends while hovering over a tile
        {
            // Place tower
            Debug.Log("Placed");
            TouchManager.instance.hoveringTile.Initialize(tower.towerPrefab);
            Destroy(gameObject);
        }
        else // If not hovering over tile, remove tower icon
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Tower towerToPlace)
    {
        tower = towerToPlace;
    }
}
