using UnityEngine;

public class TowerBase : MonoBehaviour
{
    public virtual void AbilityClose()
    {
        Debug.Log("Close to Home");
    }

    public virtual void AbilityFar()
    {
        Debug.Log("Far from Home");
    }
}
