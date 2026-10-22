using System.Collections.Generic;
using UnityEngine;

public class TowerBase : MonoBehaviour
{
    public TowerEntry tower;
    [Tooltip("How much power a tower gains per level. 0.5 = 50% more base power")]
    public float levelScaling = 0.5f;

    public enum AttackType
    {
        Melee,
        Ranged,
        Wall
    };
    public AttackType attack;
    public float range = 20;
    [Tooltip("List of lanes tower can hit (Ex: -1 = lane below, 1 = lane above)")]
    public List<int> lanes = new List<int>();
    [Tooltip("Dividing line for ability transistion")]
    public int farLimit = 5;

    TowerRelocate towerRelocate;

    // Awake, not Start: every tower subclass declares its own Start(), which hides this one,
    // so towerRelocate stayed null and FixedUpdate threw every physics tick.
    private void Awake()
    {
        towerRelocate = GetComponent<TowerRelocate>();
    }

    public virtual void AbilityClose()
    {
        Debug.Log("Close to Home");
    }

    public virtual void AbilityFar()
    {
        Debug.Log("Far from Home");
    }

    private void FixedUpdate()
    {
        int enemyLayer = 1 << 6;

        if (towerRelocate.isRelocating == false)
        {
            if (Physics.Linecast(transform.position, new Vector3(range, transform.position.y, transform.position.z), out RaycastHit hit, enemyLayer))
            {
                if (hit.collider.tag == "Enemy")
                {
                    if (transform.position.x < farLimit)
                    {
                        AbilityClose();
                    }
                    else
                    {
                        AbilityFar();
                    }
                }
            }
        }
    }
}
