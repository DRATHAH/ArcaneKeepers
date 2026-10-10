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

        towerSelectionCanvas.SetActive(false);
        StartCoroutine(WaitToLoadTowers());
    }

    void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.towerNum.Count)
            {
                Tower towerIndex = inventory.towerNum.Keys.ElementAt(i);
                slots[i].AddItem(towerIndex, inventory.towerNum[towerIndex]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }

        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (inventory.selectedTowers[i] != null)
            {
                selectedSlots[i].AddItem(inventory.selectedTowers[i], inventory.towerNum[inventory.selectedTowers[i]]);
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