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
        RelocationManager.instance.GrabObject(this.gameObject);
        originalPos = transform.position;
        if(isRelocating == false)
        {
            if (TouchManager.instance.hoveringTile != null)
            {
                originalTile = TouchManager.instance.hoveringTile;
            }
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
                    if (RelocationManager.instance.relocatedObject == this.gameObject || RelocationManager.instance.relocatedObject == null)
                    {
                        RelocateTower();
                        isRelocating = true;
                        Debug.Log(originalTile.transform.position + " " + this.gameObject.name);
                    }
                }

                if (isRelocating == true)
                {
                    transform.SetParent(null, true);
                    originalTile.tileObject = null;
                    transform.position = worldPosition;
                    gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
            }
        }
        else if (isRelocating == true && TouchManager.instance.hoveringTile && TouchManager.instance.hoveringTile.tileObject == null) // If touch ends while hovering over a tile and tile is empty
        {
            if(RelocationManager.instance.relocatedObject == this.gameObject)
            {
                isRelocating = false;
                newTile = TouchManager.instance.hoveringTile;
                originalTile = newTile;
                newTile.tileObject = this.gameObject;
                transform.position = newTile.anchorPos.position;
                transform.rotation = newTile.anchorPos.rotation;
                transform.parent = newTile.anchorPos;
                TouchManager.instance.hoveringTile.tileObject = this.gameObject;
                RelocationManager.instance.ClearObject();
            }
            Debug.Log(TouchManager.instance.hoveringTile.tileObject);
            // Place tower

        }
        else // If not hovering over tile, reset to original location
        {
            isRelocating = false;
            if (transform.position != originalTile.anchorPos.position)
            {
                transform.position = originalTile.anchorPos.position;
                transform.rotation = originalTile.anchorPos.rotation;
                transform.parent = originalTile.anchorPos;
                gameObject.layer = LayerMask.NameToLayer("Default");
            }

            if (RelocationManager.instance.relocatedObject == this.gameObject)
            {
                RelocationManager.instance.ClearObject();
            }
        }
    }
}
