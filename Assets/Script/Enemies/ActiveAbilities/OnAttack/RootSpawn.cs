using UnityEngine;

public class RootSpawn : MonoBehaviour, IAttackAbility
{
    [SerializeField] private GameObject spawnedEnemy;
    [SerializeField] private float raycastRange;



    private GameObject spawnedRoot;

    public void AttackAbility()
    {
        SpawnRoot();
    }

    private void SpawnRoot()
    {
        Debug.DrawRay(transform.position, Vector3.left);
        if(Physics.Raycast(new Vector3(transform.position.x, 0, transform.position.z), Vector3.left, out RaycastHit hit, raycastRange))
        {
            if (hit.collider.gameObject.TryGetComponent<Tile>(out Tile t))
            {
                GameObject root = ObjectPool.pool.Create(spawnedEnemy, hit.transform.position, Quaternion.identity);
                root.transform.parent = t.anchorPos.transform;
                root.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogWarning("Something other than a tile hit!");
            }
        }

    }

}
