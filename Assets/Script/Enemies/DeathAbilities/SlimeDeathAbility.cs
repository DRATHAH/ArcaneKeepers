using UnityEngine;

public class SlimeDeathAbility : MonoBehaviour, IDeathAbility
{
    private int currentTile = 0;

    [SerializeField] private GameObject bSlimePrefab;

    private float newbiePushForce = 6;
    private float separationForce = 8.333335f;

    public void OnDeathAbility()
    {
        if(WaveManager.instance != null)
        {
            //Debug.Log(currentTile);
            if (currentTile == 0)
            {
                SpawnBaby(new Vector3(newbiePushForce, 0, 0));
                SpawnBaby(new Vector3(newbiePushForce, 0, -separationForce));
                Debug.Log("One same, one below");
            }
            else if (currentTile == WaveManager.instance.spawnPoints.Count - 1)
            {
                SpawnBaby(new Vector3(newbiePushForce, 0, separationForce));
                SpawnBaby(new Vector3(newbiePushForce, 0, 0));
                Debug.Log("One above, one same");
            }
            else
            {
                SpawnBaby(new Vector3(newbiePushForce, 0, separationForce));
                SpawnBaby(new Vector3(newbiePushForce, 0, -separationForce));
                Debug.Log("One above, one below");
            }
        }
    }

    private void SpawnBaby(Vector3 spawnPush)
    {
        GameObject newBaby = ObjectPool.pool.Create(bSlimePrefab, transform.position, Quaternion.identity);

        if (newBaby.TryGetComponent<IMovement>(out IMovement move))
        {
            move.Push(spawnPush);
        }
    }

    public void SetTile(int newTile)
    {
        currentTile = newTile;
    }
}
