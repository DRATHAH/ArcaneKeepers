using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    Tower tower;
    public bool selectedSlot = false;
    public Image icon;
    public TMP_Text number;
    public int amount = 0;
    public Slider amountSlider;

    int maxAmount = 5;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            OnButton();
        });
    }

    public void AddItem(Tower newItem, int num)
    {
        tower = newItem;
        icon.sprite = tower.towerIcon;
        icon.enabled = true;
        amount = num;

        number.transform.parent.gameObject.SetActive(true);
        number.text = amount.ToString() + "/" + maxAmount.ToString();
        amountSlider.value = (float)amount / (float)maxAmount;
    }

    public void ClearSlot()
    {
        tower = null;
        icon.sprite = null;
        icon.enabled = false;
        number.transform.parent.gameObject.SetActive(false);
        amount = 0;
    }

    public void OnButton()
    {
        if (tower != null)
        {
            if (selectedSlot)
            {
                Inventory.instance.Remove(tower);
            }
            else
            {
                bool containsTower = false;
                for (int i = 0; i < Inventory.instance.selectedTowers.Length; i++)
                {
                    if (Inventory.instance.selectedTowers[i] != null && Inventory.instance.selectedTowers[i].towerId == tower.towerId)
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