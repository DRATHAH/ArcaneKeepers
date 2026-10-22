using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UI;

public class TowerButton : MonoBehaviour
{
    public GameObject towerPlacerPrefab;
    public Tower tower;
    public Image icon;

    public GameObject createdObject;

    [SerializeField] private Slider towerHealthBar;

    [SerializeField] private Slider deathCooldownSlider;

    private float deathCooldown;
    private float maxDeathCooldown;

    private TowerCard card;

    // 1 right after the tower falls, counting down to 0 when it can be placed again
    public float CooldownFraction => deathCooldown > 0 && maxDeathCooldown != 0 ? Mathf.Clamp01(deathCooldown / maxDeathCooldown) : 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(deathCooldown > 0 && maxDeathCooldown != 0)
        {
            deathCooldown -= Time.deltaTime;
            if (deathCooldownSlider)
            {
                deathCooldownSlider.value = deathCooldown/ maxDeathCooldown;
            }
        }

        // The card (if any) handles dimming the art
        if (card == null)
        {
            icon.color = createdObject != null ? Color.gray : Color.white;
        }
        towerHealthBar.gameObject.SetActive(createdObject != null);
    }

    public void Initialize(Tower newTower)
    {
        tower = newTower;
        card = GetComponent<TowerCard>();
        Sprite art = newTower.cardArt != null ? newTower.cardArt : newTower.towerIcon;
        if (art != null)
        {
            if (card)
            {
                card.SetArt(art);
            }
            else
            {
                icon.sprite = art;
            }
        }
    }

    // Creates the tower icon when you drag from the image
    public void CreateTower()
    {
        if(deathCooldown <= 0)
        {
            if (createdObject == null || createdObject.activeSelf == false)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                GameObject towerPlacer = ObjectPool.pool.Create(towerPlacerPrefab, touchPos, Quaternion.identity);
                TowerPlacer placer = towerPlacer.GetComponent<TowerPlacer>();
                placer.Initialize(tower);
                placer.towerButton = this;
                towerPlacer.transform.SetParent(transform.root, true);
                if (card)
                {
                    card.OnPicked();
                }
            }
        }
    }

    public void UpdateHealthBar(float updatedValue)
    {
        towerHealthBar.value = updatedValue;
    }

    public void KillTower(float setCooldown)
    {
        createdObject = null;
        if(setCooldown != 0)
        {
            maxDeathCooldown = setCooldown;
        }
        deathCooldown = maxDeathCooldown;
    }
}
