using UnityEngine;

public class DamageBase : MonoBehaviour
{
    public virtual void Attack(float damageAmount, GameObject target)
    {
        if (target.TryGetComponent<HealthBase>(out HealthBase victimHealth))
        {
            victimHealth.TakeDamage(damageAmount);
        }
    }

    public virtual void Attack(float damageAmount, bool armorPiercing, GameObject target)
    {
        if (target.TryGetComponent<HealthBase>(out HealthBase victimHealth))
        {
            victimHealth.TakeDamage(damageAmount, armorPiercing);
        }
    }

    public virtual void Attack(float damageAmount, bool armorPiercing, Vector3 force, GameObject target)
    {
        if(target.TryGetComponent<HealthBase>(out HealthBase victimHealth))
        {
            victimHealth.TakeDamage(damageAmount, armorPiercing, force);
        }
    }
}
