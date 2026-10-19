using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Singleton

    public static Inventory instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;

        // TowerEntry is serializable, so Unity fills empty team slots with blank entries; treat those as empty
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (IsEmpty(selectedTowers[i]))
            {
                selectedTowers[i] = null;
            }
        }
    }

    #endregion

    // A team slot counts as empty when it has no entry or the entry has no tower
    public static bool IsEmpty(TowerEntry entry)
    {
        return entry == null || entry.tower == null;
    }

    public delegate void OnItemChanged(); // When invoked, calls every method subscribed to this
    public OnItemChanged onItemChangedCallback; // Call after modifying inventory to update it

    [Tooltip("How many unique towers the player can collect.")]
    public int space = 14;
    [Tooltip("The selected towers for a level.")]
    public TowerEntry[] selectedTowers = new TowerEntry[4];

    public List<TowerEntry> towerNum = new List<TowerEntry>();

    public bool Add(TowerEntry newTower)
    {
        foreach(TowerEntry t in towerNum)
        {
            if (t.tower.towerId == newTower.tower.towerId)
            {
                towerNum[towerNum.IndexOf(t)].amount += newTower.amount;


                if (onItemChangedCallback != null)
                {
                    onItemChangedCallback.Invoke();
                }

                Debug.Log("Added " + newTower.tower.towerName);
                return true;
            }
        }

        if (towerNum.Count < space)
        {
            towerNum.Add(newTower);


            if (onItemChangedCallback != null)
            {
                onItemChangedCallback.Invoke();
            }

            Debug.Log("Added " + newTower.tower.towerName);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Remove(Tower newTower)
    {
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (!IsEmpty(selectedTowers[i]) && selectedTowers[i].tower.towerId == newTower.towerId)
            {
                selectedTowers[i] = null;
            }
        }

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }

    public void EquipTower(TowerEntry equippedTower)
    {
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (IsEmpty(selectedTowers[i]))
            {
                selectedTowers[i] = equippedTower;

                if (onItemChangedCallback != null)
                {
                    onItemChangedCallback.Invoke();
                }
                return;
            }
        }
    }

    public bool CheckEquippedTowers()
    {
        int length = 0;
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (!IsEmpty(selectedTowers[i]))
            {
                length++;
            }
        }

        if (length > 0)
        {
            return true;
        }
        return false;
    }

    public void UpgradeTower(Tower tower, int amount)
    {
        TowerEntry entry = towerNum[0];
        foreach(TowerEntry towers in towerNum)
        {
            if (towers.tower.towerId == tower.towerId)
            {
                entry = towers;
                break;
            }
        }

        entry.level++;
        entry.amount -= amount;

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }

    public void ClearInventory()
    {
        towerNum.Clear();
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            selectedTowers[i] = null;
        }

        Debug.LogWarning("Cleared Inventory");

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }
}