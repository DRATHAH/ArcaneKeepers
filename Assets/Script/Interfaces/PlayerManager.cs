using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    // Determines whether InventoryUI should load from file or not
    public bool launched = false;

    public List<TowerEntry> equippedTowers = new List<TowerEntry>();
    [Header("Dev Testing Section")]
    public List<TowerEntry> unlockedTowers = new List<TowerEntry>();
    public int levelsBeaten = 0;

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

    public void SaveToFile()
    {
        if (Inventory.instance != null)
        {
            SaveTowers(Inventory.instance);
        }

        if (FileManager.instance.loadedSuccessfully)
        {
            FileManager.instance.SerializeJson();
        }
    }

    public void LoadFromFile(PlayerData data)
    {
        unlockedTowers.Clear();
        equippedTowers.Clear();

        TowerLibrary library = TowerLibrary.instance;

        if (data != null)
        {
            levelsBeaten = data.levelsBeaten;

            foreach (TowerJson tower in data.unlockedTowers)
            {
                TowerEntry towerEntry = new TowerEntry();
                towerEntry.level = tower.level;
                towerEntry.amount = tower.amount;
                towerEntry.tower = library.towers[tower.towerId];

                unlockedTowers.Add(towerEntry);
            }

            foreach (TowerJson tower in data.equippedTowers)
            {
                TowerEntry towerEntry = new TowerEntry();
                towerEntry.level = tower.level;
                towerEntry.amount = tower.amount;
                towerEntry.tower = library.towers[tower.towerId];

                equippedTowers.Add(towerEntry);
            }
        }

        LoadTowers(Inventory.instance);
        FileManager.instance.loadedSuccessfully = true;
    }
}
