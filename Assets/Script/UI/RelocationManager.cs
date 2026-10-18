using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class RelocationManager : MonoBehaviour
{
    public bool canRelocate = false;

    [SerializeField] Slider cooldownSlider;
    [SerializeField] Button relocateButton;

    public static RelocationManager instance;

    public GameObject relocatedObject;

    [SerializeField]private float maxRelocateCooldown = 1.5f;
    public float relocateCooldown;

    public bool busy;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Relocation Manager found!");
            return;
        }

        instance = this;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(canRelocate == true)
        {
            if (Touch.activeTouches.Count > 0)
            {
                       
            }
            else if (TouchManager.instance.hoveringTile && TouchManager.instance.hoveringTile.tileObject == null)
            {
                if (relocatedObject == null)
                {
                    ClearObject(0.1f);
                }
            }
        }

        if (relocateCooldown > 0 && relocatedObject == null)
        {
            relocateCooldown -= Time.deltaTime;
            if(relocateButton.interactable == true)
            {
                relocateButton.interactable = false;
                canRelocate = false;
            }
            cooldownSlider.value = relocateCooldown/maxRelocateCooldown;
        }
        else
        {
            if(relocateButton.interactable == false)
            {
                relocateButton.interactable = true;
            }
        }
    }

    void RelocateObject()
    {

    }

    public void GrabObject(GameObject grabbedObject)
    {
        relocatedObject = grabbedObject;
        //busy = true;
    }

    public void ClearObject()
    {
        relocatedObject = null;
        relocateCooldown = maxRelocateCooldown;
        canRelocate = false;
        //busy = false;
    }

    public void ClearObject(float coolDown)
    {
        relocatedObject = null;
        relocateCooldown = coolDown;
        canRelocate = false;
    }

    public void EnableRelocate()
    {
        canRelocate = true;
        cooldownSlider.value = maxRelocateCooldown/maxRelocateCooldown;
    }
}
