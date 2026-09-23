using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class TouchManager : MonoBehaviour
{
    InputActionMap actions;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Checks if a touch device exists and a press just started
        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            // Get screen position of the touch
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();

            // Create a ray from the camera to the touch position
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.GetComponent<Tile>())
                {
                    Debug.Log("over tile " + hit.transform.name);
                }
            }
        }
    }
}
