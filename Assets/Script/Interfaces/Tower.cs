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
    public int level = 1;
    public int amount = 1;
}

public static class RarityCalculation
{
    public static int[] commonReqs = { 1, 2, 4, 10, 20, 50, 100, 200, 400, 800 };
    public static int[] rareReqs = { 1, 2, 4, 10, 20, 50, 100, 200 };
    public static int[] epicReqs = { 1, 2, 4, 10, 20 };

    public static int GetCardReqs(TowerEntry tower)
    {
        Tower.TowerRarity rarity = tower.tower.rarity;
        int tempLevel = tower.level;
        switch (rarity)
        {
            case Tower.TowerRarity.Common:
                break;

            case Tower.TowerRarity.Rare:
                if (tower.level < 2)
                {
                    tempLevel = 2;
                }
                return rareReqs[tempLevel - 2];

            case Tower.TowerRarity.Epic:
                if (tower.level < 5)
                {
                    tempLevel = 5;
                }
                return epicReqs[tempLevel - 5];
        }

        return commonReqs[tempLevel];
    }
}