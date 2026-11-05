using JetBrains.Annotations;
using UnityEngine;

public class FreeTile : MonoBehaviour, IDeathAbility
{
    private int currentTile;

    [HideInInspector]
    public bool triggered
    {
        get
        {
            return false;
        }
    }

    public void OnDeathAbility()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5, 1 << 0))
        {
            if (hit.collider.gameObject.TryGetComponent<Tile>(out Tile t))
            {
                t.tileObject = null;
            }
        }
    }

    public void SetTile(int newTile)
    {
        currentTile = newTile;
    }
}
