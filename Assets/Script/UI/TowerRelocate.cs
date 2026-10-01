using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TowerRelocate : MonoBehaviour
{
    Tower tower;
    Vector3 originalPos;
    bool isRelocating = false;

    private void Start()
    {
        tower = GetComponent<TowerBase>().tower;
        originalPos = transform.position;
    }

    public void RelocateTower()
    {
        isRelocating = true;
    }

    private void Update()
    {// Check if player is touching screen
        if (Touch.activeTouches.Count > 0)
        {
            // Get position of touch
            Vector2 mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.up);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);

                Debug.Log(worldPosition);
                transform.SetParent(null, true);
                transform.position = worldPosition;
                gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
        }
        else if (TouchManager.instance.hoveringTile && TouchManager.instance.hoveringTile.tileObject == null) // If touch ends while hovering over a tile and tile is empty
        {
            // Place tower
            Debug.Log("Placed");
            isRelocating = false;
            TouchManager.instance.hoveringTile.Initialize(tower.towerPrefab);
            Destroy(gameObject);
        }
        else // If not hovering over tile, reset to original location
        {
            transform.position = originalPos;
            gameObject.layer = LayerMask.NameToLayer("Default");
            isRelocating = false;
        }
    }
}
