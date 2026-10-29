using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public CharacterSelectScreen selectScreen;
    public LineupScroller lineup;
    public Transform towersParent;
    public GameObject cardPrefab;

    Inventory inventory;

    // Start is called before the first frame update
    void Start()
    {
        inventory = Inventory.instance;
        inventory.onItemChangedCallback += UpdateUI;

        StartCoroutine(WaitToLoadTowers());
    }

    void UpdateUI()
    {
        List<CharacterCard> cardsToAdd = new List<CharacterCard>();

        for (int i = 0; i < inventory.towerNum.Count; i++)
        {
            TowerEntry towerIndex = inventory.towerNum[i];
            GameObject card = Instantiate(cardPrefab, towersParent);
            CharacterCard charCard = card.GetComponent<CharacterCard>();
            charCard.AddTower(towerIndex);
            cardsToAdd.Add(charCard);
        }

        selectScreen.UpdateUI(cardsToAdd);
        lineup.UpdateCards(cardsToAdd);
    }

    IEnumerator WaitToLoadTowers()
    {
        yield return new WaitForSeconds(0.1f);
        PlayerManager.instance.LoadTowers(inventory);
    }
}