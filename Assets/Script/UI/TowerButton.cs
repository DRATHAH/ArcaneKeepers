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
            deathCooldownSlider.value = deathCooldown/ maxDeathCooldown;
        }

        if(createdObject != null)
        {
            icon.color = Color.gray;
            towerHealthBar.gameObject.SetActive(true);
        }
        else
        {
            icon.color = Color.white;
            towerHealthBar.gameObject.SetActive(false);
        }
    }

    public void Initialize(Tower newTower)
    {
        tower = newTower;
        if (newTower.towerIcon != null)
        {
            icon.sprite = newTower.towerIcon;
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
