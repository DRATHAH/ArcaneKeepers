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

    [Tooltip("How many of each tower the player can have.")]
    public int itemSpace = 10;
    [Tooltip("How many unique towers the player can collect.")]
    public int space = 14;
    [Tooltip("The selected towers for a level.")]
    public TowerEntry[] selectedTowers = new TowerEntry[4];

    public List<TowerEntry> towerNum = new List<TowerEntry>();

    public bool Add(TowerEntry newTower)
    {
        // If we have tower overflow (probably won't be needed in the future of the game)
        if (towerNum.Contains(newTower) && towerNum[towerNum.IndexOf(newTower)].amount >= itemSpace) // Modify item name so that 'towerNum' doesn't have duplicate keys
        {
            Tower tempItem = (Tower)ScriptableObject.CreateInstance(typeof(Tower));
            tempItem.name = newTower.tower.name;
            tempItem.towerIcon = newTower.tower.towerIcon;
            tempItem.towerPrefab = newTower.tower.towerPrefab;
            tempItem.towerId = newTower.tower.towerId;

            TowerEntry entry = new TowerEntry();
            entry.tower = tempItem;
            entry.amount = newTower.amount;
            newTower = entry;
        }

        if (towerNum.Contains(newTower))
        {
            towerNum[towerNum.IndexOf(newTower)].amount++;
        }
        else if (towerNum.Count < space)
        {
            towerNum.Add(newTower);
        }
        else if (towerNum.Count > space)
        {
            return false;
        }

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }

        Debug.Log("Added " + newTower.tower.towerName);
        return true;
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