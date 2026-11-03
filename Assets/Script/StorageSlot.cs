using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageSlot : MonoBehaviour
{
    private string itemName;
    private bool isPlayerInventorySlot; // 이 슬롯이 플레이어 인벤토리용인지, 보관함용인지 구분
    private Storage owner;

    public void Setup(string name, bool isPlayerSlot, Storage storage = null)
    {
        itemName = name;
        isPlayerInventorySlot = isPlayerSlot;
        owner = storage != null ? storage : FindFirstObjectByType<Storage>();

        DataManager data = DataManager.instance;
        Transform iconTransform = transform.Find("Icon");
        Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        TextMeshProUGUI countText = GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null)
        {
            icon.sprite = ItemAssets.SpriteFor(name);
            icon.preserveAspect = true;
        }
        if (countText != null && data != null)
        {
            var source = isPlayerSlot ? data.inventory : data.storage;
            countText.text = source.TryGetValue(name, out int count) ? "x" + count : "";
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(MoveItem);
        }
    }

    // 클릭: 1개 이동, Shift+클릭: 전부 이동
    void MoveItem()
    {
        DataManager data = DataManager.instance;
        if (data == null) return;

        bool moveAll = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        data.MoveItem(itemName, isPlayerInventorySlot, moveAll ? int.MaxValue : 1);

        if (owner != null) owner.UpdateStorageUI();
    }
}
