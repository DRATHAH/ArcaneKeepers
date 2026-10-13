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

    public List<TowerEntry> equippedTowers = new List<TowerEntry>();
    [Header("Dev Testing Section")]
    public List<TowerEntry> cheatedTowers = new List<TowerEntry>();
    public List<TowerEntry> unlockedTowers = new List<TowerEntry>();

    private void Start()
    {
        foreach(TowerEntry tower in cheatedTowers)
        {
            unlockedTowers.Add(tower);
        }
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

        foreach(TowerEntry tow in inventory.towerNum)
        {
            unlockedTowers.Add(tow);
        }
    }
    public void LoadTowers(Inventory inventory)
    {
        inventory.ClearInventory();

        foreach(TowerEntry tower in unlockedTowers)
        {
            for (int i = 0; i < unlockedTowers.Count; i++)
            {
                Debug.Log("added " + tower.tower.towerName);
                inventory.Add(tower);
            }
        }

        for (int i = 0; i < equippedTowers.Count; i++)
        {
            inventory.EquipTower(equippedTowers[i]);
        }
    }
}
