using UnityEngine;

public class Storage : MonoBehaviour
{
    [Header("UI")]
    public GameObject storagePanel;

    [Header("Storage Settings")]
    public GameObject storageSlotPrefab;
    public Transform playerInventoryGrid; // 플레이어 인벤토리를 표시할 그리드
    public Transform storageGrid;         // 보관함을 표시할 그리드

    private bool isPlayerInRange;
    private DataManager dataManager;

    void Start()
    {
        dataManager = DataManager.instance;
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = !storagePanel.activeSelf;
            storagePanel.SetActive(isActive);

            if (isActive)
            {
                UpdateStorageUI();
            }
        }
    }

    // ✨ 보관함 UI를 업데이트하는 함수
    public void UpdateStorageUI()
    {
        // 1. 플레이어 인벤토리 UI 업데이트
        // 기존 슬롯 삭제
        foreach (Transform child in playerInventoryGrid) Destroy(child.gameObject);
        // 새 슬롯 생성
        foreach (var item in dataManager.inventory)
        {
            GameObject slotGO = Instantiate(storageSlotPrefab, playerInventoryGrid);
            slotGO.GetComponent<StorageSlot>().Setup(item.Key, true); // true = 플레이어 인벤토리 슬롯
        }

        // 2. 보관함 UI 업데이트
        foreach (Transform child in storageGrid) Destroy(child.gameObject);
        foreach (var item in dataManager.storage)
        {
            GameObject slotGO = Instantiate(storageSlotPrefab, storageGrid);
            slotGO.GetComponent<StorageSlot>().Setup(item.Key, false); // false = 보관함 슬롯
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            storagePanel.SetActive(false);
        }
    }
}