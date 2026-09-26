using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower", menuName = "Arcane Keepers/Tower")]
public class Tower : ScriptableObject
{
    public string towerName = "";
    public GameObject towerPrefab;
}
