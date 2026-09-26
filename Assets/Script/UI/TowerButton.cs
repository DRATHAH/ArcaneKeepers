using UnityEngine;
using UnityEngine.InputSystem;

public class TowerButton : MonoBehaviour
{
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

    public void CreateTower()
    {
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Debug.Log("Tower");
    }
}
