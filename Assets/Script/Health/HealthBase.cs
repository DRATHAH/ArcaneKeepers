using UnityEngine;

public class HealthBase : MonoBehaviour
{
    public virtual void TakeDamage(float damageAmount)
    {
        Debug.Log("Take damage");
    }

    public virtual void RecoverHealth(float recoveryAmount)
    {
        Debug.Log("Heal health");
    }

    public virtual void Die()
    {
        Debug.Log(this.name + "Died.");
    }
}
