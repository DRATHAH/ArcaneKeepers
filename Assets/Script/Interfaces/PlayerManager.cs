using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    #region Singleton

    public static PlayerManager instance;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.Log("More than one instance of Player Manager found!");
            return;
        }

        instance = this;
        DontDestroyOnLoad(instance);
    }

    #endregion

    public List<Tower> equippedTowers = new List<Tower>();
    [Header("Dev Testing Section")]
    public List<Tower> cheatedTowers = new List<Tower>();
    public Dictionary<Tower, int> unlockedTowers = new Dictionary<Tower, int>();

    private void Start()
    {
        foreach(Tower tower in cheatedTowers)
        {
            unlockedTowers.Add(tower, 1);
        }

        LoadTowers(Inventory.instance);
    }

    public void SaveTowers(Inventory inventory)
    {
        unlockedTowers.Clear();
        equippedTowers.Clear();

        for (int i = 0; i < inventory.selectedTowers.Length; i++)
        {
            if (inventory.selectedTowers[i] != null)
            {
                equippedTowers.Add(inventory.selectedTowers[i]);
            }
        }

        foreach(Tower tow in inventory.towerNum.Keys)
        {
            unlockedTowers.Add(tow, inventory.towerNum[tow]);
        }
    }
    public void LoadTowers(Inventory inventory)
    {
        inventory.ClearInventory();

        foreach(Tower tower in unlockedTowers.Keys)
        {
            for (int i = 0; i < unlockedTowers[tower]; i++)
            {
                Debug.Log("added " + tower.towerName);
                inventory.Add(tower);
            }
        }

        for (int i = 0; i < equippedTowers.Count; i++)
        {
            inventory.EquipTower(equippedTowers[i]);
        }
    }
}
