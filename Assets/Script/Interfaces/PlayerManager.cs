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
            if (!Inventory.IsEmpty(inventory.selectedTowers[i]))
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
            // Entries with no tower (e.g. unassigned in the inspector) would break the slots
            if (Inventory.IsEmpty(tower))
            {
                continue;
            }
            inventory.Add(tower);
        }

        for (int i = 0; i < equippedTowers.Count; i++)
        {
            if (!Inventory.IsEmpty(equippedTowers[i]))
            {
                inventory.EquipTower(equippedTowers[i]);
            }
        }
    }
}
