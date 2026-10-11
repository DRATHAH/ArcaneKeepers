using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class LevelEndObject : MonoBehaviour
{
    [SerializeField] private Collider hitBox;

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
            Vector2 mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.up);

            // Correctly sets the position of the tower to hover over the ground
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);
                if (hitBox.bounds.Contains(worldPosition) && WinLossManager.instance != null)
                {
                    WinLossManager.instance.winner = true;
                }
            }
        }
    }
}
