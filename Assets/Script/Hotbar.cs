using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Hotbar : MonoBehaviour
{
    [Header("슬롯 설정")]
    public int slotCount = 5;
    public GameObject hotbarSlotPrefab;

    private List<HotbarSlot> slots = new List<HotbarSlot>();
    private int selectedSlotIndex = 0;
    private DataManager dataManager;

    // ✨ 현재 선택된 아이템의 이름을 다른 스크립트가 알 수 있도록 public으로 선언
    public string GetSelectedItemName()
    {
        // 핫바에 표시된 아이템 목록을 가져온다
        var hotbarItems = GetHotbarItems();
        // 현재 선택된 슬롯 인덱스가 목록 범위 내에 있는지 확인
        if (selectedSlotIndex < hotbarItems.Count)
        {
            return hotbarItems[selectedSlotIndex].Key; // 아이템 이름 반환
        }
        return null; // 선택된 아이템이 없으면 null 반환
    }

    void Start()
    {
        dataManager = DataManager.instance;
        CreateSlots();
        UpdateHotbarUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // ✨ 인벤토리가 변경될 때마다 핫바를 새로고침 (간단한 방식)
        if (Time.frameCount % 10 == 0) // 매 10프레임마다 체크
        {
            UpdateHotbarUI();
        }
    }

    void CreateSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotGO = Instantiate(hotbarSlotPrefab, this.transform);
            slots.Add(slotGO.GetComponent<HotbarSlot>());
        }
    }

    // ✨ 핫바 UI를 업데이트하는 함수
    void UpdateHotbarUI()
    {
        var hotbarItems = GetHotbarItems();

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < hotbarItems.Count)
            {
                slots[i].DrawSlot(hotbarItems[i].Key, hotbarItems[i].Value);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    // ✨ 인벤토리에서 '도구'와 '씨앗'만 필터링해서 가져오는 함수
    List<KeyValuePair<string, int>> GetHotbarItems()
    {
        List<string> toolAndSeedItems = new List<string> { "Hoe", "Watering Can", "Scythe", "Fertile Hoe", "Copper Watering Can", "Steel Axe", "Carrot Seed" };

        return dataManager.inventory
            .Where(item => toolAndSeedItems.Contains(item.Key))
            .ToList();
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        slots[selectedSlotIndex].SetSelector(false);
        selectedSlotIndex = index;
        slots[selectedSlotIndex].SetSelector(true);
    }
}