using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchManager : MonoBehaviour
{
    InputActionMap actions;
    bool isTouching = false;

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
        if (Touchscreen.current.touches.Count > 0)
        {
            // Get screen position of the touch
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();

            bool touchInProgress = false;
            // Checks if the finger is still touching the screen
            foreach(Touch touch in Touch.activeTouches)
            {

                // Detect if over UI
                if (EventSystem.current.IsPointerOverGameObject(touch.touchId) && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    OverUI(GetUIElementUnderTouch(touchPos));
                    return;
                }

                if (touch.isInProgress)
                {
                    touchInProgress = true;
                }
            }
            isTouching = touchInProgress;

            // Create a ray from the camera to the touch position
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && isTouching)
            {
                // If over a tile, drag tower to it
                if (hit.transform.GetComponent<Tile>())
                {
                    Debug.Log("over tile " + hit.transform.name);
                }
            }
        }
    }

    GameObject GetUIElementUnderTouch(Vector2 screePos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current) {position = screePos};
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count > 0)
        {
            return raycastResults[0].gameObject;
        }

        return null;
    }

    void OverUI(GameObject UI)
    {
        Debug.Log(UI.name);

        // Detect if over tower icon
        if (UI.GetComponent<TowerButton>())
        {
            UI.GetComponent<TowerButton>().CreateTower();
        }
    }
}
