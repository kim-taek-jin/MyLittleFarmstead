using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 핫바: 도구 / 씨앗 / 음식 / 작물 / 정수를 고정된 순서로 보여줍니다.
// 아이템이 슬롯 수보다 많으면 마우스 휠로 넘겨볼 수 있습니다.
public class Hotbar : MonoBehaviour
{
    public static Hotbar Instance { get; private set; }

    [Header("슬롯 설정")]
    public int slotCount = 5;
    public GameObject hotbarSlotPrefab;

    private readonly List<HotbarSlot> slots = new List<HotbarSlot>();
    private List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
    private int selectedIndex = 0;   // items 기준 인덱스
    private int pageOffset = 0;      // 첫 번째 슬롯에 보이는 items 인덱스
    private DataManager dataManager;

    // 씬을 이동해도 선택한 아이템을 기억합니다.
    private static string lastSelectedName;

    // 핫바가 없는 씬에서도 '마지막으로 들고 있던 아이템'을 알려줍니다.
    public static string SelectedItem
    {
        get
        {
            if (Instance != null) return Instance.GetSelectedItemName();
            DataManager data = DataManager.instance;
            if (data != null && !string.IsNullOrEmpty(lastSelectedName) && data.HasItem(lastSelectedName)) return lastSelectedName;
            return null;
        }
    }

    public string GetSelectedItemName()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count) return items[selectedIndex].Key;
        return null;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (dataManager != null) dataManager.OnInventoryChanged -= Refresh;
    }

    void Start()
    {
        dataManager = DataManager.instance;
        CreateSlots();
        if (dataManager != null) dataManager.OnInventoryChanged += Refresh;
        Refresh();
    }

    void Update()
    {
        int keyCount = Mathf.Min(9, slots.Count);
        for (int i = 0; i < keyCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectVisibleSlot(i);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f) Select(selectedIndex - 1);
        else if (scroll < -0.01f) Select(selectedIndex + 1);

        // 혹시 이벤트를 놓친 경우를 대비한 가벼운 주기적 갱신
        if (Time.frameCount % 30 == 0) Refresh();
    }

    void CreateSlots()
    {
        // 픽셀 UI의 핫바 칸을 씁니다. (장면의 예전 핫바 그림은 숨김)
        PixelHUD pixel = PixelHUD.Ensure();
        if (pixel != null && pixel.HotbarSlots.Count > 0)
        {
            UnityEngine.UI.Image oldBackground = GetComponent<UnityEngine.UI.Image>();
            if (oldBackground != null) oldBackground.enabled = false;
            slots.AddRange(pixel.HotbarSlots);
            return;
        }
        if (hotbarSlotPrefab == null) return;
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotGO = Instantiate(hotbarSlotPrefab, this.transform);
            HotbarSlot slot = slotGO.GetComponent<HotbarSlot>();
            if (slot != null) slots.Add(slot);
        }
    }

    // 인벤토리가 바뀔 때 호출: 목록을 다시 만들고, 들고 있던 아이템을 최대한 유지합니다.
    public void Refresh()
    {
        if (dataManager == null) dataManager = DataManager.instance;
        if (dataManager == null) return;

        items = dataManager.inventory
            .Where(item => ItemDB.ShowInHotbar(item.Key))
            .OrderBy(item => ItemDB.SortKey(item.Key))
            .ToList();

        int keep = string.IsNullOrEmpty(lastSelectedName) ? -1 : items.FindIndex(i => i.Key == lastSelectedName);
        if (keep >= 0) selectedIndex = keep;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, items.Count - 1));
        if (items.Count > 0) lastSelectedName = items[selectedIndex].Key;

        KeepSelectionVisible();
        Draw();
    }

    void Draw()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int itemIndex = pageOffset + i;
            if (itemIndex < items.Count) slots[i].DrawSlot(items[itemIndex].Key, items[itemIndex].Value);
            else slots[i].ClearSlot();
            slots[i].SetSelector(itemIndex == selectedIndex && itemIndex < items.Count);
        }
    }

    void SelectVisibleSlot(int slotIndex)
    {
        int index = pageOffset + slotIndex;
        if (index < items.Count) Select(index);
    }

    void Select(int index)
    {
        if (items.Count == 0) return;
        index = ((index % items.Count) + items.Count) % items.Count; // 양 끝에서 순환
        selectedIndex = index;
        lastSelectedName = items[index].Key;
        KeepSelectionVisible();
        Draw();

        ItemDef def = ItemDB.Get(lastSelectedName);
        if (def != null && !string.IsNullOrEmpty(def.description)) HUD.Toast($"{def.name}: {def.description}", 2.5f);
    }

    void KeepSelectionVisible()
    {
        int visible = Mathf.Max(1, slots.Count);
        if (selectedIndex < pageOffset) pageOffset = selectedIndex;
        if (selectedIndex >= pageOffset + visible) pageOffset = selectedIndex - visible + 1;
        pageOffset = Mathf.Clamp(pageOffset, 0, Mathf.Max(0, items.Count - visible));
    }
}
