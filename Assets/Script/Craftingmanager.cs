using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// 제작 창. 버튼 또는 C키로 열고 닫습니다.
// 씬에 연결된 레시피(allRecipes) + 코드로 추가되는 기본 레시피(요리, 도구 업그레이드)를 보여줍니다.
public class CraftingManager : MonoBehaviour
{
    public GameObject craftingPanel;
    public GameObject recipeSlotPrefab;
    public Transform recipeContentParent;
    public Recipe[] allRecipes;

    [Tooltip("체크하면 코드에 정의된 기본 레시피(요리, 구리 물뿌리개)를 함께 보여줍니다.")]
    public bool includeBuiltInRecipes = true;

    private DataManager dataManager;
    private readonly List<Recipe> recipes = new List<Recipe>();

    void Start()
    {
        dataManager = DataManager.instance;
        if (craftingPanel != null) craftingPanel.SetActive(false);
        BuildRecipeList();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) ToggleCraftingWindow();
        if (craftingPanel != null && craftingPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) craftingPanel.SetActive(false);
    }

    void BuildRecipeList()
    {
        recipes.Clear();
        if (allRecipes != null) recipes.AddRange(allRecipes.Where(r => r != null && r.resultItem != null));
        if (!includeBuiltInRecipes) return;

        foreach (Recipe recipe in CreateBuiltInRecipes())
        {
            bool exists = recipes.Any(r => r.name == recipe.name);
            if (!exists) recipes.Add(recipe);
        }
    }

    static IEnumerable<Recipe> CreateBuiltInRecipes()
    {
        // 주민에게 배우는 레시피 (해금 전에는 목록에 안 보임)
        yield return Make("Copper Watering Can", 1, "", new ItemInfo("Watering Can", 1), new ItemInfo("Stone", 10), new ItemInfo("Wood", 5)).Locked("recipe_copper_can");
        yield return Make("Carrot Cake", 1, "Campfire", new ItemInfo("Carrot", 2), new ItemInfo("Bread", 1)).Locked("recipe_carrot_cake");
        yield return Make("Grilled Fish", 1, "Campfire", new ItemInfo("Minnow", 1), new ItemInfo("Wood", 1)).Locked("fishing", "Recipe_Grilled Fish (Minnow)");
        yield return Make("Grilled Fish", 2, "Campfire", new ItemInfo("Carp", 1), new ItemInfo("Wood", 1)).Locked("fishing", "Recipe_Grilled Fish (Carp)");
        yield return Make("Roasted Carrot", 1, "Campfire", new ItemInfo("Carrot", 2), new ItemInfo("Wood", 1));
        yield return Make("Baked Potato", 1, "Campfire", new ItemInfo("Potato", 1), new ItemInfo("Wood", 1));
        yield return Make("Corn Soup", 1, "Campfire", new ItemInfo("Corn", 2));
        yield return Make("Strawberry Jam", 1, "Campfire", new ItemInfo("Strawberry", 3));
    }

    static Recipe Make(string result, int count, string station, params ItemInfo[] required)
    {
        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        recipe.name = "Recipe_" + result;
        recipe.resultItem = new ItemInfo(result, count);
        recipe.requiredItems = required;
        recipe.requiredStation = station;
        return recipe;
    }

    // 제작 창을 켜고 끄는 함수 (씬의 Craft 버튼에 연결되어 있음)
    public void ToggleCraftingWindow()
    {
        if (craftingPanel == null) return;
        craftingPanel.SetActive(!craftingPanel.activeSelf);
        if (craftingPanel.activeSelf) DisplayRecipes();
    }

    bool IsAvailable(Recipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.requiredUnlock)) return true;
        DataManager data = DataManager.instance;
        return data != null && data.story.IsUnlocked(recipe.requiredUnlock);
    }

    bool CanCraft(Recipe recipe, out string reason)
    {
        reason = "";
        if (dataManager == null) dataManager = DataManager.instance;
        if (dataManager == null) return false;

        if (!string.IsNullOrEmpty(recipe.requiredStation) && !dataManager.HasItem(recipe.requiredStation))
        {
            reason = $"Needs a {recipe.requiredStation}.";
            return false;
        }
        foreach (ItemInfo requiredItem in recipe.requiredItems)
        {
            if (!dataManager.HasItem(requiredItem.itemName, requiredItem.count))
            {
                reason = $"Not enough {requiredItem.itemName}.";
                return false;
            }
        }
        return true;
    }

    void DisplayRecipes()
    {
        if (recipeContentParent == null || recipeSlotPrefab == null) return;

        foreach (Transform child in recipeContentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Recipe recipe in recipes)
        {
            if (!IsAvailable(recipe)) continue;
            GameObject slotGO = Instantiate(recipeSlotPrefab, recipeContentParent);

            Image itemIcon = FindChild<Image>(slotGO.transform, "ItemIcon");
            TextMeshProUGUI itemNameText = FindChild<TextMeshProUGUI>(slotGO.transform, "ItemNameText");
            TextMeshProUGUI requiredItemsText = FindChild<TextMeshProUGUI>(slotGO.transform, "RequiredItemsText");
            Button craftButton = FindChild<Button>(slotGO.transform, "CraftButton");

            string resultName = recipe.resultItem.itemName;
            if (itemIcon != null)
            {
                itemIcon.sprite = ItemAssets.SpriteFor(resultName);
                itemIcon.preserveAspect = true;
            }
            if (itemNameText != null)
            {
                itemNameText.text = recipe.resultItem.count > 1 ? $"{resultName} x{recipe.resultItem.count}" : resultName;
            }
            if (requiredItemsText != null)
            {
                string requiredText = string.Join(", ", recipe.requiredItems.Select(i => $"{i.itemName} x{i.count}"));
                if (!string.IsNullOrEmpty(recipe.requiredStation)) requiredText += $"  [{recipe.requiredStation}]";
                requiredItemsText.text = requiredText;
            }

            if (craftButton != null)
            {
                craftButton.interactable = CanCraft(recipe, out _);
                Recipe captured = recipe;
                craftButton.onClick.AddListener(() => CraftItem(captured));
            }
        }
    }

    void CraftItem(Recipe recipe)
    {
        if (!CanCraft(recipe, out string reason))
        {
            HUD.Toast(reason);
            return;
        }

        foreach (ItemInfo requiredItem in recipe.requiredItems)
        {
            dataManager.RemoveItem(requiredItem.itemName, requiredItem.count);
        }
        dataManager.AddItem(recipe.resultItem.itemName, recipe.resultItem.count);

        // 물뿌리개를 업그레이드하면 물을 가득 채워 줍니다.
        if (ItemDB.GetToolType(recipe.resultItem.itemName) == ToolType.WateringCan) dataManager.RefillWater();

        DisplayRecipes();
        HUD.Toast($"Crafted {recipe.resultItem.itemName}!");
    }

    static T FindChild<T>(Transform parent, string name) where T : Component
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<T>() : null;
    }
}

static class RecipeExtensions
{
    public static Recipe Locked(this Recipe recipe, string unlockKey, string uniqueName = null)
    {
        recipe.requiredUnlock = unlockKey;
        if (!string.IsNullOrEmpty(uniqueName)) recipe.name = uniqueName;
        return recipe;
    }
}
