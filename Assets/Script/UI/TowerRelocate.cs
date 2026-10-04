using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TowerRelocate : MonoBehaviour
{
    Tower tower;
    Vector3 originalPos;
    [SerializeField] bool isRelocating = false;

    [SerializeField] private Collider hitBox;

    private Tile originalTile;
    private Tile newTile;


    private void Start()
    {
        tower = GetComponent<TowerBase>().tower;
        originalPos = transform.position;
        if(TouchManager.instance.hoveringTile != null)
        {
            originalTile = TouchManager.instance.hoveringTile;
        }
    }

    public void RelocateTower()
    {
        isRelocating = true;
        originalPos = transform.position;
        if (TouchManager.instance.hoveringTile != null)
        {
            originalTile = TouchManager.instance.hoveringTile;
        }
    }

    private void Update()
    {
            // Check if player is touching screen
            if (Touch.activeTouches.Count > 0)
            {
                // Get position of touch
                Vector2 mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                Plane groundPlane = new Plane(Vector3.up, Vector3.up);

                // Correctly sets the position of the tower to hover over the ground
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 worldPosition = ray.GetPoint(distance);

                    if (hitBox.bounds.Contains(worldPosition))
                    {
                        RelocateTower();
                        transform.SetParent(null, true);
                        originalTile.tileObject = null;
                        transform.position = worldPosition;
                        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                    }
                }
            }
            else if (TouchManager.instance.hoveringTile && isRelocating == true && TouchManager.instance.hoveringTile.tileObject == null) // If touch ends while hovering over a tile and tile is empty
            {
                // Place tower
                isRelocating = false;
                newTile = TouchManager.instance.hoveringTile;
                newTile.tileObject = this.gameObject;
                transform.position = newTile.anchorPos.position;
                transform.rotation = newTile.anchorPos.rotation;
                transform.parent = newTile.anchorPos;
            }
            else // If not hovering over tile, reset to original location
            {
                gameObject.layer = LayerMask.NameToLayer("Default");
                isRelocating = false;
            }
    }
}
