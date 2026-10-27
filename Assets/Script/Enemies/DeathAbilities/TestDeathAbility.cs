using UnityEngine;

public class TestDeathAbility : MonoBehaviour, IDeathAbility
{
    public void OnDeathAbility()
    {
        Debug.Log("Test Death Ability");
    }

    public void SetTile(int newTile)
    {
        Debug.Log("Set Tile");
    }
}
