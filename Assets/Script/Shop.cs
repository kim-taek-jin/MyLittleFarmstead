using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ItemForSale
{
    public string itemName;
    public int price;
}

public class Shop : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopPanel;
    public GameObject buyItemGrid;
    public GameObject sellItemGrid;
    public Button buyButton;
    public Button sellButton;

    [Header("Shop Settings")]
    public GameObject shopSlotPrefab;

    [Header("판매할 아이템 목록")]
    public List<ItemForSale> itemsForSale = new List<ItemForSale>();

    private bool isPlayerInRange;
    private DataManager dataManager;

    void Start()
    {
        dataManager = DataManager.instance;
        buyButton.onClick.AddListener(ShowBuyPanel);
        sellButton.onClick.AddListener(ShowSellPanel);
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = !shopPanel.activeSelf;
            shopPanel.SetActive(isActive);

            // ✨ 상점 창이 열릴 때, 기본적으로 '구매' 탭이 보이도록 수정
            if (isActive)
            {
                ShowBuyPanel();
            }
        }
    }

    void ShowBuyPanel()
    {
        buyItemGrid.SetActive(true);
        sellItemGrid.SetActive(false);
        UpdateShopUI(true); // '구매' 모드로 UI 업데이트
    }

    void ShowSellPanel()
    {
        buyItemGrid.SetActive(false);
        sellItemGrid.SetActive(true);
        UpdateShopUI(false); // '판매' 모드로 UI 업데이트
    }

    public void UpdateShopUI(bool isBuyMode)
    {
        Transform gridParent = isBuyMode ? buyItemGrid.transform : sellItemGrid.transform;

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        if (isBuyMode)
        {
            foreach (var item in itemsForSale)
            {
                GameObject slotGO = Instantiate(shopSlotPrefab, gridParent);
                ShopSlot slotScript = slotGO.GetComponent<ShopSlot>();
                if (slotScript != null) slotScript.SetupForBuy(item.itemName, item.price, this);
            }
        }
        else
        {
            foreach (var item in dataManager.inventory)
            {
                GameObject slotGO = Instantiate(shopSlotPrefab, gridParent);
                ShopSlot slotScript = slotGO.GetComponent<ShopSlot>();
                if (slotScript != null) slotScript.SetupForSell(item.Key, item.Value, this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            // ✨ 플레이어가 들어왔을 때 Debug.Log는 굳이 필요 없으므로 삭제 (선택 사항)
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            shopPanel.SetActive(false);
        }
    }
}