using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int levelsBeaten = 0;
    public List<TowerJson> unlockedTowers = new List<TowerJson>();
    public List<TowerJson> equippedTowers = new List<TowerJson>();
}

[System.Serializable]
public class TowerJson
{
    public int towerId;
    public int level;
    public int amount;
}