using UnityEngine;

public class TowerProjectile : MonoBehaviour
{
    public bool destroy = false;
    private GameObject Projectile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Projectile = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (destroy)
        {
            destroy = false;
            ObjectPool.pool.Destroy(ref Projectile);
        }
    }
}
