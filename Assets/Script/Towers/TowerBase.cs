using UnityEngine;

public class TowerBase : MonoBehaviour
{
    public TowerEntry tower;
    [Tooltip("How much power a tower gains per level. 0.5 = 50% more base power")]
    public float levelScaling = 0.5f;

    public virtual void AbilityClose()
    {
        Debug.Log("Close to Home");
    }

    public virtual void AbilityFar()
    {
        Debug.Log("Far from Home");
    }
}
