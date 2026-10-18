using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    TowerEntry tower;
    public bool selectedSlot = false;
    public Image icon;
    public TMP_Text number;
    public int amount = 0;
    public Slider amountSlider;
    [Tooltip("Optional: shows the tower's name.")]
    public TMP_Text nameText;
    [Tooltip("Optional: shown only while the slot is empty.")]
    public GameObject emptyMarker;

    int maxAmount = 5;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            OnButton();
        });
    }

    public void AddItem(TowerEntry newItem)
    {
        tower = newItem;
        icon.sprite = tower.tower.towerIcon;
        icon.enabled = true;
        amount = tower.amount;

        number.transform.parent.gameObject.SetActive(true);
        number.text = amount.ToString() + "/" + maxAmount.ToString();
        amountSlider.value = (float)amount / (float)maxAmount;

        if (nameText)
        {
            nameText.text = tower.tower.towerName;
        }
        if (emptyMarker)
        {
            emptyMarker.SetActive(false);
        }
    }

    public void ClearSlot()
    {
        tower = null;
        icon.sprite = null;
        icon.enabled = false;
        number.transform.parent.gameObject.SetActive(false);
        amount = 0;

        if (nameText)
        {
            nameText.text = "";
        }
        if (emptyMarker)
        {
            emptyMarker.SetActive(true);
        }
    }

    public void OnButton()
    {
        SoundManager.Instance.PlaySound("ButtonClick");
        if (tower != null)
        {
            if (selectedSlot)
            {
                Inventory.instance.Remove(tower.tower);
            }
            else
            {
                bool containsTower = false;
                for (int i = 0; i < Inventory.instance.selectedTowers.Length; i++)
                {
                    if (!Inventory.IsEmpty(Inventory.instance.selectedTowers[i]) && Inventory.instance.selectedTowers[i].tower.towerId == tower.tower.towerId)
                    {
                        containsTower = true;
                    }
                }

                if (!containsTower)
                {
                    Inventory.instance.EquipTower(tower);
                }
            }
        }
    }
}