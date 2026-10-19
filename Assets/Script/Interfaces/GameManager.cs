using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager instance;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Game Manager found!");
            return;
        }

        instance = this;
    }

    #endregion

    public int gridSize;
    public GridLayoutGroup lanes;
    public GameObject tile;

    public Transform towerButtonParent;
    public GameObject towerButtonPrefab;
    public int lootRolls = 3;
    [Tooltip("Based on Common rarity. Rare # is 50% less, Epic # is 25% less")]
    public int minTowerDrops = 5;
    public List<TowerEntry> towerLoot;

    int laneNum = 5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Create the grid on the map
        GridLayoutGroup.Constraint type = lanes.constraint;
        int count = lanes.constraintCount;
        if (type == GridLayoutGroup.Constraint.FixedRowCount)
        {
            laneNum = count;
        }

        for (int i = 0; i < laneNum * gridSize; i++)
        {
            GameObject createdTile = Instantiate(tile, transform.position, Quaternion.identity);
            createdTile.transform.SetParent(lanes.transform, false);
        }

        // Load the towers the player equipped (none when the level is started directly, without the character menu)
        if (PlayerManager.instance == null)
        {
            Debug.LogWarning("No PlayerManager: start from the character menu to bring a team into the level.");
            return;
        }
        foreach(TowerEntry tower in PlayerManager.instance.equippedTowers)
        {
            if (Inventory.IsEmpty(tower))
            {
                continue;
            }
            GameObject towerButton = Instantiate(towerButtonPrefab, towerButtonParent);
            towerButton.GetComponent<TowerButton>().Initialize(tower.tower);
        }
    }

    public void GetMatchReward()
    {
        for (int i = 0;i < lootRolls; i++)
        {
            int r = Random.Range(0, towerLoot.Count);
            TowerEntry chosenLoot = towerLoot[r];

            TowerEntry entry = new TowerEntry();
            entry.tower = chosenLoot.tower;
            int rarityMult = 1;
            if (entry.tower.rarity == Tower.TowerRarity.Rare)
            {
                rarityMult = 2;
            }
            else if(entry.tower.rarity == Tower.TowerRarity.Epic)
            {
                rarityMult = 4;
            }
            entry.amount = Random.Range(minTowerDrops, chosenLoot.amount);
            entry.amount = Mathf.Clamp(entry.amount / rarityMult, 1, chosenLoot.amount);

            Inventory.instance.Add(entry);
            Debug.Log("Added " +  entry.amount + " " + entry.tower.towerName);
        }

        PlayerManager.instance.SaveTowers(Inventory.instance);
    }
}
