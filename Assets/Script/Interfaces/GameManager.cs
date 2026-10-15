using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int gridSize;
    public GridLayoutGroup lanes;
    public GameObject tile;

    public Transform towerButtonParent;
    public GameObject towerButtonPrefab;

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
}
