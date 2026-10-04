using UnityEngine;

public class ObjectKiller : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        ObjectPool.pool.Destroy(other.gameObject);
    }
}
