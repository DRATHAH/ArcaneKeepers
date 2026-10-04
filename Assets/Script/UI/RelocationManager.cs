using UnityEngine;

public class RelocationManager : MonoBehaviour
{
    public static RelocationManager instance;

    public GameObject relocatedObject;

    [SerializeField]private float maxRelocateCooldown = 0.5f;
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
        if(relocateCooldown > 0 && relocatedObject == null)
        {
            relocateCooldown -= Time.deltaTime;
        }
    }

    public void GrabObject(GameObject grabbedObject)
    {
        relocatedObject = grabbedObject;
        relocateCooldown = maxRelocateCooldown;
        //busy = true;
    }

    public void ClearObject()
    {
        relocatedObject = null;
        relocateCooldown = maxRelocateCooldown;
        //busy = false;
    }
}
