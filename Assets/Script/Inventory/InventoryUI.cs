using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;

public class InventoryUI : MonoBehaviour
{
    public GameObject towerSelectionCanvas;
    public Transform towersParent;
    public Transform selectedTowersParent;

    Inventory inventory;
    InventorySlot[] slots;
    InventorySlot[] selectedSlots;

    // Start is called before the first frame update
    void Start()
    {
        inventory = Inventory.instance;
        inventory.onItemChangedCallback += UpdateUI;
        slots = towersParent.GetComponentsInChildren<InventorySlot>();
        selectedSlots = selectedTowersParent.GetComponentsInChildren<InventorySlot>();

        // Optional: the picker can also be always visible
        if (towerSelectionCanvas)
        {
            towerSelectionCanvas.SetActive(false);
        }
        StartCoroutine(WaitToLoadTowers());
    }

    void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.towerNum.Count)
            {
                TowerEntry towerIndex = inventory.towerNum[i];
                slots[i].AddItem(towerIndex);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }

        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (!Inventory.IsEmpty(inventory.selectedTowers[i]))
            {
                selectedSlots[i].AddItem(inventory.selectedTowers[i]);
            }
            else
            {
                selectedSlots[i].ClearSlot();
            }
        }
    }

    IEnumerator WaitToLoadTowers()
    {
        yield return new WaitForSeconds(0.1f);
        PlayerManager.instance.LoadTowers(inventory);
    }
}