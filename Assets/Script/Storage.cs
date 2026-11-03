using UnityEngine;

// 보관함. E키로 열고, 슬롯을 클릭하면 1개, Shift+클릭하면 전부 옮깁니다.
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
        if (storagePanel == null) return;

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = !storagePanel.activeSelf;
            storagePanel.SetActive(isActive);
            if (isActive) UpdateStorageUI();
        }
        if (storagePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            storagePanel.SetActive(false);
        }
    }

    public void UpdateStorageUI()
    {
        if (dataManager == null) dataManager = DataManager.instance;
        if (dataManager == null) return;

        foreach (Transform child in playerInventoryGrid) Destroy(child.gameObject);
        foreach (var item in dataManager.inventory)
        {
            GameObject slotGO = Instantiate(storageSlotPrefab, playerInventoryGrid);
            StorageSlot slot = slotGO.GetComponent<StorageSlot>();
            if (slot != null) slot.Setup(item.Key, true, this);
        }

        foreach (Transform child in storageGrid) Destroy(child.gameObject);
        foreach (var item in dataManager.storage)
        {
            GameObject slotGO = Instantiate(storageSlotPrefab, storageGrid);
            StorageSlot slot = slotGO.GetComponent<StorageSlot>();
            if (slot != null) slot.Setup(item.Key, false, this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            HUD.Toast("Press E to open the storage box.", 2f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (storagePanel != null) storagePanel.SetActive(false);
        }
    }
}
