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
        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = ItemAssets.SpriteFor(itemName);
            icon.preserveAspect = true;
        }

        if (itemCountText != null)
        {
            // 도구는 개수 대신 빈칸 (물뿌리개는 남은 물 표시)
            string text = count.ToString();
            ItemDef def = ItemDB.Get(itemName);
            if (def != null && def.category == ItemCategory.Tool)
            {
                text = def.toolType == ToolType.WateringCan && DataManager.instance != null
                    ? $"{DataManager.instance.waterCharges}"
                    : "";
            }
            itemCountText.gameObject.SetActive(true);
            itemCountText.text = text;
        }
    }

    // 슬롯을 비우는 함수
    public void ClearSlot()
    {
        if (icon != null) icon.gameObject.SetActive(false);
        if (itemCountText != null) itemCountText.gameObject.SetActive(false);
    }

    // 선택 상태를 업데이트하는 함수
    public void SetSelector(bool isActive)
    {
        if (selector != null) selector.SetActive(isActive);
    }
}
