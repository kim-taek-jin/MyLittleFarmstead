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

    // 판매 UI를 위한 설정 함수
    public void SetupForSell(string name, int count, Shop shop)
    {
        itemName = name;
        itemCount = count;
        shopManager = shop;
        dataManager = DataManager.instance;
        button = GetComponent<Button>();

        transform.Find("Icon").GetComponent<Image>().sprite = ItemAssets.Instance.GetSpriteByName(itemName);
        GetComponentInChildren<TextMeshProUGUI>().text = itemCount.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(SellItem);
        //Debug.Log(itemName + " 슬롯의 판매 버튼이 설정되었습니다.");
    }

    // 구매 UI를 위한 새로운 설정 함수
    public void SetupForBuy(string name, int price, Shop shop)
    {
        itemName = name;
        itemPrice = price;
        shopManager = shop;
        dataManager = DataManager.instance;
        button = GetComponent<Button>();

        transform.Find("Icon").GetComponent<Image>().sprite = ItemAssets.Instance.GetSpriteByName(itemName);
        GetComponentInChildren<TextMeshProUGUI>().text = itemPrice.ToString() + " G";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(BuyItem);
    }

    // ShopSlot.cs의 SellItem() 함수

    void SellItem()
    {
        if (dataManager == null || shopManager == null) return;

        // ✨ 1. ItemAssets에 아이템의 판매 가격을 물어봅니다.
        int price = ItemAssets.Instance.GetSellPriceByName(itemName);

        // 2. DataManager에 아이템 1개 제거 요청
        dataManager.RemoveItem(itemName, 1);

        // ✨ 3. 고정된 10골드 대신, 위에서 받아온 가격(price)을 사용합니다.
        dataManager.AddGold(price);

        // 4. 상점 UI를 '판매' 모드로 새로고침
        shopManager.UpdateShopUI(false);
    }

    // 아이템 구매 함수
    void BuyItem()
    {
        if (dataManager.gold >= itemPrice)
        {
            dataManager.AddGold(-itemPrice);
            dataManager.AddItem(itemName, 1);
            Debug.Log(itemName + " 아이템을 구매했습니다!");
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
        }
    }
}