using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageSlot : MonoBehaviour
{
    private string itemName;
    private bool isPlayerInventorySlot; // 이 슬롯이 플레이어 인벤토리용인지, 보관함용인지 구분

    public void Setup(string name, bool isPlayerSlot)
    {
        itemName = name;
        isPlayerInventorySlot = isPlayerSlot;

        // 아이콘과 아이템 개수 설정
        Image icon = transform.Find("Icon")?.GetComponent<Image>();
        TextMeshProUGUI countText = GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null)
        {
            icon.sprite = ItemAssets.Instance.GetSpriteByName(name);
        }
        if (countText != null)
        {
            countText.text = isPlayerSlot ?
                DataManager.instance.inventory[name].ToString() :
                DataManager.instance.storage[name].ToString();
        }

        // 버튼 클릭 이벤트 연결
        GetComponent<Button>().onClick.AddListener(MoveItem);
    }

    void MoveItem()
    {
        // 플레이어 인벤토리 슬롯을 클릭했다면 -> 보관함으로 이동
        if (isPlayerInventorySlot)
        {
            Debug.Log(itemName + "을(를) 보관함으로 이동.");
            DataManager.instance.inventory.Remove(itemName); // 임시로 전체 이동
            DataManager.instance.storage.Add(itemName, 1); // 임시로 1개 추가
        }
        // 보관함 슬롯을 클릭했다면 -> 플레이어 인벤토리로 이동
        else
        {
            Debug.Log(itemName + "을(를) 인벤토리로 이동.");
            DataManager.instance.storage.Remove(itemName);
            DataManager.instance.inventory.Add(itemName, 1);
        }

        // UI 새로고침 (Storage.cs에 있는 함수를 호출해야 함 - 다음 단계에서 구현)
        FindObjectOfType<Storage>().UpdateStorageUI();
    }
}