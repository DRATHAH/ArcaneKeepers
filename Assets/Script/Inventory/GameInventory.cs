using UnityEngine;

public class GameInventory : Inventory
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerManager.instance.LoadTowers(this);
    }
}
