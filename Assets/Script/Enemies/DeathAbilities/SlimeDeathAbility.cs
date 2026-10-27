using UnityEngine;

public class SlimeDeathAbility : MonoBehaviour, IDeathAbility
{
    private int currentTile = 0;

    [SerializeField] private GameObject bSlimePrefab;

    private Vector3 newbiePushForce = new Vector3(50, 0, 0);

    public void OnDeathAbility()
    {
        if(WaveManager.instance != null)
        {
            Debug.Log(currentTile);
            if (currentTile == 0)
            {
                GameObject newBaby = ObjectPool.pool.Create(bSlimePrefab, transform.position, Quaternion.identity);

                if (newBaby.TryGetComponent<EnemyMovement>(out EnemyMovement move))
                {
                    move.Push(newbiePushForce);
                }
                Debug.Log("One same, one below");
            }
            else if (currentTile == WaveManager.instance.spawnPoints.Count - 1)
            {
                Debug.Log("One above, one below");
            }
            else
            {
                GameObject newBaby = ObjectPool.pool.Create(bSlimePrefab, transform.position, Quaternion.identity);

                if (newBaby.TryGetComponent<EnemyMovement>(out EnemyMovement move))
                {
                    move.Push(newbiePushForce);
                }
                Debug.Log("One above, one below");
            }
        }
    }

    public void SetTile(int newTile)
    {
        currentTile = newTile;
    }
}
