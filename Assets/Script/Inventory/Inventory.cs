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
    }

    #endregion

    public delegate void OnItemChanged(); // When invoked, calls every method subscribed to this
    public OnItemChanged onItemChangedCallback; // Call after modifying inventory to update it

    [Tooltip("How many of each tower the player can have.")]
    public int itemSpace = 10;
    [Tooltip("How many unique towers the player can collect.")]
    public int space = 14;
    [Tooltip("The selected towers for a level.")]
    public Tower[] selectedTowers = new Tower[4];

    public Dictionary<Tower, int> towerNum = new Dictionary<Tower, int>();

    public bool Add(Tower newTower)
    {
        // If we have tower overflow (probably won't be needed in the future of the game)
        if (towerNum.ContainsKey(newTower) && towerNum[newTower] >= itemSpace) // Modify item name so that 'towerNum' doesn't have duplicate keys
        {
            Tower tempItem = (Tower)ScriptableObject.CreateInstance(typeof(Tower));
            tempItem.name = newTower.name;
            tempItem.towerIcon = newTower.towerIcon;
            tempItem.towerPrefab = newTower.towerPrefab;
            tempItem.towerId = newTower.towerId;
            newTower = tempItem;
        }

        if (towerNum.ContainsKey(newTower) && towerNum[newTower] < itemSpace)
        {
            towerNum[newTower]++;
        }
        else if (towerNum.Count < space)
        {
            towerNum.Add(newTower, 1);
        }
        else if (towerNum.Count > space)
        {
            return false;
        }

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }

        Debug.Log("Added " + newTower.towerName);
        return true;
    }

    public void Remove(Tower newTower)
    {
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (selectedTowers[i] != null && selectedTowers[i].towerId == newTower.towerId)
            {
                selectedTowers[i] = null;
            }
        }

        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }

    public void EquipTower(Tower equippedTower)
    {
        for (int i = 0; i < selectedTowers.Length; i++)
        {
            if (selectedTowers[i] == null)
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
            if (selectedTowers[i] != null)
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