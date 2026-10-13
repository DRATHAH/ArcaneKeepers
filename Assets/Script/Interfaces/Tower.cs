using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower", menuName = "Arcane Keepers/Tower")]
public class Tower : ScriptableObject
{
    public enum TowerRarity
    {
        Common,
        Rare,
        Epic
    }
    public TowerRarity rarity = TowerRarity.Common;
    public string towerName = "";
    public GameObject towerPrefab;
    public Sprite towerIcon;
    public int towerId = 0;
}

[Serializable]
public class TowerEntry
{
    public Tower tower;
    public int amount = 1;
}

public class RarityCalculation
{
    public int[] commonReqs = { 1, 2, 4, 10, 20, 50, 100, 200, 400, 800};
    public int[] rareReqs = { 1, 2, 4, 10, 20, 50, 100, 200};
    public int[] epicReqs = { 1, 2, 4, 10, 20 };

    public int GetCardReqs(TowerEntry tower)
    {
        Tower.TowerRarity rarity = tower.tower.rarity;
        switch (rarity)
        {
            case Tower.TowerRarity.Common:
                break;

            case Tower.TowerRarity.Rare:
                return rareReqs[tower.amount - 2];

            case Tower.TowerRarity.Epic:
                return epicReqs[tower.amount - 5];
        }

        return commonReqs[tower.amount];
    }
}