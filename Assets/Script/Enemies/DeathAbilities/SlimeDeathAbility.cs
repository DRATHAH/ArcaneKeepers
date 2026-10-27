using UnityEngine;

public class SlimeDeathAbility : MonoBehaviour, IDeathAbility
{
    private int currentTile = 0;

    [SerializeField] private GameObject bSlimePrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDeathAbility()
    {
        if(WaveManager.instance != null)
        {
            if (currentTile == 0)
            {
                Debug.Log("One same, one below");
            }
            else if (currentTile == WaveManager.instance.spawnPoints.Count - 1)
            {
                Debug.Log("One above, one below");
            }
            else
            {
                Debug.Log("One above, one same");
            }
        }
    }
}
