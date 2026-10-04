using Unity.VisualScripting;
using UnityEngine;

public class ClericTower : TowerBase
{
    [Tooltip("Dividing Line for Ability Transistion")]
    [SerializeField] private float swapLimit = 4f;

    [Tooltip("How long the waiting period for shots for far ability should be.")]
    [SerializeField] private float maxHealingBurstCooldown;
    private float healingBurstCooldown;

    [Tooltip("Amount of time between spawnings of a new healing field")]
    [SerializeField] private float maxHealingFieldCooldown = 5;
    private float healingFieldCooldown;

    private float farCooldown;

    [Tooltip("Prefab for the healing aura.")]
    [SerializeField] private GameObject healingAura;

    [Tooltip("The two speeds for the healing field. Slot 0 = Slow Speed. Slot 1 = Fast Speed")]
    [SerializeField] private float[] healingSpeeds = new float[2];

    [Tooltip("Healing amount for the burst heal ability.")]
    [SerializeField] private float healingAmount = 500;

    [Tooltip("How long each healing field should linger for.")]
    [SerializeField] private float healingFieldLifeTime = 5;

    [Tooltip("Offset for spawning the healing field.")]
    [SerializeField] private float verticalOffset = 0.5f;
    private GameObject healingField;

    private TowerRelocate towerRelocate;

    private void Start()
    {
        towerRelocate = GetComponent<TowerRelocate>();
    }

    private void OnEnable()
    {
        healingBurstCooldown = maxHealingBurstCooldown;
        healingFieldCooldown = maxHealingFieldCooldown;
    }

    void FixedUpdate()
    {
        if(towerRelocate.isRelocating == false)
        {
            if (healingFieldCooldown > 0)
            {
                healingFieldCooldown -= Time.deltaTime;
            }

            if (transform.position.x < swapLimit)
            {
                AbilityClose();
            }
            else
            {
                AbilityFar();
            }
        }
    }


    public override void AbilityClose()
    {
        if(healingBurstCooldown > 0)
        {
            healingBurstCooldown -= Time.deltaTime;
            if(healingFieldCooldown <= 0)
            {
                Heal(healingSpeeds[0], false, healingFieldLifeTime);
            }
        }
        else
        {
            if (healingFieldCooldown <= 0)
            {
                Heal(healingSpeeds[1], false, healingFieldLifeTime);
            }
        }
    }

    public override void AbilityFar()
    {
        if(healingBurstCooldown <= 0)
        {
            Heal(healingAmount, true, 5);
            healingBurstCooldown = maxHealingBurstCooldown;
        }
        else if(healingFieldCooldown <= 0)
        {
            Heal(healingSpeeds[0], false, healingFieldLifeTime);
        }
    }

    private void Heal(float healAmount, bool burst, float lifeTime)
    {
        healingField = ObjectPool.pool.Create(healingAura, new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z), Quaternion.identity);
        HealingAura aura = healingField.GetComponent<HealingAura>();
        aura.parent = this.gameObject;
        aura.lifeTime = lifeTime;
        if (burst == false)
        {
            aura.healingAmount = healAmount * Time.deltaTime;
        }
        else
        {
            aura.healingAmount = healAmount;
        }
        aura.burstHeal = burst;
        healingFieldCooldown = maxHealingFieldCooldown;
    }
}
