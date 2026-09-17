using UnityEngine;

public class IHealth : MonoBehaviour
{
    public virtual void TakeDamage(ref float healthVar, float damageAmount)
    {
        healthVar -= damageAmount;
    }

    public virtual void RecoverHealth(ref float healthVar, float recoveryAmount)
    {
        healthVar += recoveryAmount;
    }

    public virtual void Die()
    {
        Debug.Log(this.name + "Died.");
    }
}
