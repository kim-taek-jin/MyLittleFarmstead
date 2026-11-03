using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour
{
    private string itemName;
    private int itemCount;
    private int itemPrice;

    private Shop shopManager;
    private DataManager dataManager;
    private Button button;

    void Init(string name, Shop shop)
    {
        itemName = ItemDB.Normalize(name);
        shopManager = shop;
        dataManager = DataManager.instance;
        button = GetComponent<Button>();

        Transform iconTransform = transform.Find("Icon");
        Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (icon != null)
        {
            icon.sprite = ItemAssets.SpriteFor(itemName);
            icon.preserveAspect = true;
        }
    }

    void SetLabel(string text)
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = text;
    }

    // 판매 UI를 위한 설정 함수
    public void SetupForSell(string name, int count, Shop shop)
    {
        Init(name, shop);
        itemCount = count;
        int price = dataManager != null ? dataManager.GetSellPrice(itemName) : 0;
        bool demanded = dataManager != null && dataManager.market.demandItem == itemName;
        SetLabel(demanded ? $"x{itemCount} <color=#ffd84a>{price}G</color>" : $"x{itemCount} {price}G");

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SellItem);
        }
    }

    // 구매 UI를 위한 설정 함수
    public void SetupForBuy(string name, int price, Shop shop)
    {
        Init(name, shop);
        itemPrice = price;
        SetLabel(itemPrice + " G");

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(BuyItem);
        }
    }

    void SellItem()
    {
        if (dataManager == null || shopManager == null) return;

        int price = dataManager.SellItem(itemName);
        if (price <= 0)
        {
            HUD.Toast("You can't sell that.");
            return;
        }
        HUD.Toast($"Sold {itemName} for {price}G.", 2f);
        shopManager.UpdateShopUI(false);
    }

    void BuyItem()
    {
        if (dataManager == null) return;

        if (dataManager.gold >= itemPrice)
        {
            dataManager.AddGold(-itemPrice);
            dataManager.AddItem(itemName, 1);
            HUD.Toast($"Bought {itemName}.", 2f);
        }
        else
        {
            HUD.Toast("Not enough gold!");
        }
    }
}
