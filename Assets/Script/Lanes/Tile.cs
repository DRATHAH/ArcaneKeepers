using UnityEngine;

public class Tile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseEnter()
    {
        Debug.Log("Over");
    }

    public void Initialize(GameObject preset)
    {
        // If we want an obstacle here, call this function
    }
}
