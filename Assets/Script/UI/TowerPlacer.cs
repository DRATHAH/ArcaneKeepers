using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TowerPlacer : MonoBehaviour
{
    public Tower tower;
    [HideInInspector] public GameObject createdTower;
    [HideInInspector] public TowerButton towerButton;

    [SerializeField]private Image spriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<Image>();
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
            GameObject newbie = ObjectPool.pool.Create(tower.towerPrefab, TouchManager.instance.hoveringTile.anchorPos.position, TouchManager.instance.hoveringTile.anchorPos.rotation);
            newbie.transform.parent = TouchManager.instance.hoveringTile.anchorPos;
            towerButton.createdObject = newbie;
            ObjectPool.pool.Destroy(gameObject);
        }
        else // If not hovering over tile, remove tower icon
        {
            ObjectPool.pool.Destroy(gameObject);
        }
    }

    public void Initialize(Tower towerToPlace)
    {
        tower = towerToPlace;
        spriteRenderer.sprite = tower.towerIcon;
    }
}
