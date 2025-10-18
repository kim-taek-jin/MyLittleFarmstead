using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingManager : MonoBehaviour
{
    public GameObject craftingPanel;
    public GameObject recipeSlotPrefab;
    public Transform recipeContentParent;
    public Recipe[] allRecipes;

    private DataManager dataManager;

    void Start()
    {
        dataManager = FindFirstObjectByType<DataManager>();
        craftingPanel.SetActive(false);
        //DisplayRecipes(); // Start에서 미리 호출할 필요는 없습니다.
    }

    // 제작 창을 켜고 끄는 함수
    public void ToggleCraftingWindow()
    {
        craftingPanel.SetActive(!craftingPanel.activeSelf);

        // 창을 열 때마다 레시피 목록을 새로고침해서 버튼 상태를 업데이트
        if (craftingPanel.activeSelf)
        {
            DisplayRecipes();
        }
    }

    // 모든 레시피를 UI에 표시하는 함수
    void DisplayRecipes()
    {
        // 기존에 있던 슬롯들은 모두 삭제
        foreach (Transform child in recipeContentParent)
        {
            Destroy(child.gameObject);
        }

        // 모든 레시피를 순회하면서 슬롯을 하나씩 생성
        foreach (Recipe recipe in allRecipes)
        {
            GameObject slotGO = Instantiate(recipeSlotPrefab, recipeContentParent);

            // 슬롯의 UI 요소들을 가져옴
            Image itemIcon = slotGO.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemNameText = slotGO.transform.Find("ItemNameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI requiredItemsText = slotGO.transform.Find("RequiredItemsText").GetComponent<TextMeshProUGUI>();
            Button craftButton = slotGO.transform.Find("CraftButton").GetComponent<Button>();

            // UI에 레시피 정보 표시
            itemNameText.text = recipe.resultItem.itemName;
            string requiredText = "";
            foreach (ItemInfo item in recipe.requiredItems)
            {
                requiredText += $"{item.itemName} x{item.count} ";
            }
            requiredItemsText.text = requiredText;

            // --- 🔽 [디버깅] 버튼 활성화 로직 🔽 ---
            bool canCraft = true;
            foreach (ItemInfo requiredItem in recipe.requiredItems)
            {
                int ownedCount = dataManager.inventory.ContainsKey(requiredItem.itemName) ? dataManager.inventory[requiredItem.itemName] : 0;

                // --- 🔽 여기가 진짜 디버깅 코드 위치입니다! 🔽 ---
                Debug.Log($"레시피: {recipe.resultItem.itemName}, 확인 아이템: {requiredItem.itemName}, 필요: {requiredItem.count}, 보유: {ownedCount}");

                if (ownedCount < requiredItem.count)
                {
                    canCraft = false;
                    break;
                }
            }
            craftButton.interactable = canCraft;

            // 제작 버튼에 기능 연결
            craftButton.onClick.AddListener(() => {
                CraftItem(recipe);
            });
        }
    }

    // 아이템을 제작하는 함수
    void CraftItem(Recipe recipe)
    {
        // 1. 재료가 충분한지 확인 (버튼이 활성화되었다면 통과하지만, 안전을 위해 한번 더 체크)
        foreach (ItemInfo requiredItem in recipe.requiredItems)
        {
            int ownedCount = dataManager.inventory.ContainsKey(requiredItem.itemName) ? dataManager.inventory[requiredItem.itemName] : 0;
            if (ownedCount < requiredItem.count)
            {
                Debug.Log(requiredItem.itemName + " 재료가 부족합니다! (CraftItem 함수)");
                return;
            }
        }

        // 2. 재료 소모
        foreach (ItemInfo requiredItem in recipe.requiredItems)
        {
            dataManager.RemoveItem(requiredItem.itemName, requiredItem.count);
        }

        // 3. 결과물 아이템 추가
        dataManager.AddItem(recipe.resultItem.itemName, recipe.resultItem.count);

        // 제작 성공 후 목록 새로고침
        DisplayRecipes();
        Debug.Log(recipe.resultItem.itemName + " 제작 성공!");
    }
}