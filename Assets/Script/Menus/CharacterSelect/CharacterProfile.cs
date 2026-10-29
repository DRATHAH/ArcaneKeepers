using System;
using UnityEngine;

// Everything the character select screen shows about one keeper: who they are and three 1-5 ratings
// (shown as pips only, no descriptions)
[Serializable]
public class CharacterProfile
{
    public Tower tower;
    [Tooltip("Full-body art standing on the platform (feet at the bottom edge)")]
    public Sprite heroSprite;
    public Color accent = Color.white;
    [Range(1, 5)] public int health = 3;
    [Range(1, 5)] public int power = 3;
    [Range(1, 5)] public int range = 3;
}
