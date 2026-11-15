using UnityEngine;
using UnityEngine.InputSystem;

public class Tile : MonoBehaviour
{
    public GameObject tileObject;
    public Transform anchorPos;

    [Tooltip("Floor art variants; one is picked at random so the vine grid doesn't look stamped")]
    public Sprite[] floorVariants;

    void Awake()
    {
        SpriteRenderer floor = GetComponent<SpriteRenderer>();
        if (floor != null && floorVariants != null && floorVariants.Length > 0)
        {
            floor.sprite = floorVariants[Random.Range(0, floorVariants.Length)];
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Places an object on this tile
    public void Initialize(GameObject preset)
    {
        tileObject = ObjectPool.pool.Create(preset, anchorPos.position, anchorPos.rotation);
        tileObject.transform.SetParent(anchorPos);
    }

    public void SetTileObject(GameObject newTO)
    {
        tileObject = newTO;
    }
}
