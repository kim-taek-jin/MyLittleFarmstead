using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ItemForSale
{
    public string itemName;
    public int price;
}

// 시장 상점. E키로 열고, 구매/판매 탭을 전환합니다.
// 판매 가격은 '살아있는 시장'(오늘의 수요, 많이 팔면 하락, 흥정 보너스)에 따라 변합니다.
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
    private bool isBuyMode = true;
    private DataManager dataManager;

    void Start()
    {
        dataManager = DataManager.instance;
        if (buyButton != null) buyButton.onClick.AddListener(ShowBuyPanel);
        if (sellButton != null) sellButton.onClick.AddListener(ShowSellPanel);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Update()
    {
        if (shopPanel == null) return;

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = !shopPanel.activeSelf;
            shopPanel.SetActive(isActive);
            if (isActive) ShowBuyPanel();
        }
        if (shopPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            shopPanel.SetActive(false);
        }
    }

    void ShowBuyPanel()
    {
        isBuyMode = true;
        buyItemGrid.SetActive(true);
        sellItemGrid.SetActive(false);
        UpdateShopUI(true);
    }

    void ShowSellPanel()
    {
        isBuyMode = false;
        buyItemGrid.SetActive(false);
        sellItemGrid.SetActive(true);
        UpdateShopUI(false);

        if (dataManager != null)
        {
            MarketState m = dataManager.market;
            string bonus = m.haggleBonus > 0f ? $"  |  Haggle bonus +{m.haggleBonus * 100f:0}%" : "";
            HUD.Toast($"Today's demand: {m.demandItem} (+{MarketState.DemandBonus * 100f:0}%){bonus}", 4f);
        }
    }

    public void UpdateShopUI(bool buyMode)
    {
        if (dataManager == null) dataManager = DataManager.instance;
        Transform gridParent = buyMode ? buyItemGrid.transform : sellItemGrid.transform;

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        if (buyMode)
        {
            // 씬에 정해진 상품 + 주민 덕분에 새로 들어온 상품
            var stock = new List<ItemForSale>(itemsForSale);
            if (dataManager != null)
            {
                foreach (var unlock in VillagerDB.ShopUnlocks)
                {
                    if (dataManager.story.IsUnlocked(unlock.Key)) stock.Add(unlock.Value);
                }
                if (dataManager.CurrentSeason >= Season.Autumn) stock.Add(new ItemForSale { itemName = "Pumpkin Seed", price = 70 });
                if (!dataManager.HasItem("Mattock")) stock.Add(new ItemForSale { itemName = "Mattock", price = 150 });
            }
            foreach (var item in stock)
            {
                // 지금 마음의 계절에 자라지 않는 씨앗은 아직 팔지 않습니다.
                CropDef crop = CropDB.FromSeed(ItemDB.Normalize(item.itemName));
                if (crop != null && dataManager != null && !crop.GrowsIn(dataManager.CurrentSeason)) continue;
                GameObject slotGO = Instantiate(shopSlotPrefab, gridParent);
                ShopSlot slotScript = slotGO.GetComponent<ShopSlot>();
                if (slotScript != null) slotScript.SetupForBuy(ItemDB.Normalize(item.itemName), item.price, this);
            }
        }
        else if (dataManager != null)
        {
            // 도구는 팔 수 없습니다. 목록을 복사해서 순회 (판매 중 인벤토리 변경 대비)
            foreach (var item in dataManager.inventory.ToList())
            {
                if (!ItemDB.CanSell(item.Key)) continue;
                GameObject slotGO = Instantiate(shopSlotPrefab, gridParent);
                ShopSlot slotScript = slotGO.GetComponent<ShopSlot>();
                if (slotScript != null) slotScript.SetupForSell(item.Key, item.Value, this);
            }
        }
    }

    public void RefreshCurrentTab()
    {
        UpdateShopUI(isBuyMode);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            HUD.Toast("Press E to trade with the merchant.", 2.5f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (shopPanel != null) shopPanel.SetActive(false);
        }
    }
}
