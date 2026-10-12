using UnityEngine;

public class MeleeAttack : DamageBase
{    public override void Attack(float damageAmount, bool armorPiercing, GameObject target)
    {
        Debug.Log(target);
        if (target.TryGetComponent<HealthBase>(out HealthBase victimHealth))
        {
            victimHealth.TakeDamage(damageAmount, armorPiercing);
        }
    }
}
