using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI itemCountText;
    public GameObject selector;

    // 슬롯에 아이템 정보를 그려주는 함수
    public void DrawSlot(string itemName, int count)
    {
        icon.gameObject.SetActive(true);
        itemCountText.gameObject.SetActive(true);

        icon.sprite = ItemAssets.Instance.GetSpriteByName(itemName);
        itemCountText.text = count.ToString();
    }

    // 슬롯을 비우는 함수
    public void ClearSlot()
    {
        icon.gameObject.SetActive(false);
        itemCountText.gameObject.SetActive(false);
    }

    // 선택 상태를 업데이트하는 함수
    public void SetSelector(bool isActive)
    {
        selector.SetActive(isActive);
    }
}