using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    // --- 아이템 카테고리 ---
    private List<string> normalItems = new List<string> { "Wood", "Stone", "Campfire" };
    private List<string> toolItems = new List<string> { "Hoe", "Watering Can", "Scythe" };

    // --- 데이터 저장 공간 ---
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, int> storage = new Dictionary<string, int>();
    public List<CropData> farmCropData = new List<CropData>();
    public int gold = 0;

    // --- UI 요소 참조 ---
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI goldText;
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider energySlider;
    public Transform specialItemContentParent;
    public GameObject specialItemSlotPrefab;
    public GameObject gameOverPanel;

    // --- 플레이어 상태 변수 ---
    public float health = 100f;
    public float hunger = 100f;
    public float thirst = 100f;
    public float energy = 100f;
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float maxEnergy = 100f;
    public float hungerDecreaseRate = 0.5f;
    public float thirstDecreaseRate = 0.8f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        Time.timeScale = 1f; // 게임 시간을 정상 속도로 되돌립니다.
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIElements();
    }

    void FindUIElements()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            woodText = FindComponentInChild<TextMeshProUGUI>(canvas.transform, "WoodText");
            stoneText = FindComponentInChild<TextMeshProUGUI>(canvas.transform, "StoneText");
            goldText = FindComponentInChild<TextMeshProUGUI>(canvas.transform, "GoldText");
            healthSlider = FindComponentInChild<Slider>(canvas.transform, "HealthSlider");
            hungerSlider = FindComponentInChild<Slider>(canvas.transform, "HungerSlider");
            thirstSlider = FindComponentInChild<Slider>(canvas.transform, "ThirstSlider");
            energySlider = FindComponentInChild<Slider>(canvas.transform, "EnergySlider");
            specialItemContentParent = FindComponentInChild<Transform>(canvas.transform, "SpecialItemPanel1");
            gameOverPanel = FindComponentInChild<Transform>(canvas.transform, "GameOverPanel")?.gameObject;
        }
        UpdateAllUI();
    }

    void Start()
    {
        FindUIElements();
        if (inventory.Count == 0)
        {
            AddItem("Carrot Seed", 5);
            AddItem("Hoe", 1);
            AddItem("Watering Can", 1);
            AddItem("Scythe", 1);
        }
    }

    void Update()
    {
        hunger -= hungerDecreaseRate * Time.deltaTime;
        thirst -= thirstDecreaseRate * Time.deltaTime;

        if (hunger < 0) hunger = 0;
        if (thirst < 0) thirst = 0;

        if (hunger <= 0 || thirst <= 0)
        {
            health -= 5f * Time.deltaTime;
            if (health < 0) health = 0;

            if (health <= 0)
            {
                GameOver();
            }
        }
        UpdateStatusUI();
    }

    public void UpdateAllUI()
    {
        UpdateResourceUI();
        UpdateSpecialItemUI();
        UpdateStatusUI();
        UpdateGoldUI();
    }

    void UpdateResourceUI()
    {
        if (woodText == null || stoneText == null) return;
        woodText.text = "Wood: " + (inventory.ContainsKey("Wood") ? inventory["Wood"] : 0);
        stoneText.text = "Stone: " + (inventory.ContainsKey("Stone") ? inventory["Stone"] : 0);
    }

    void UpdateStatusUI()
    {
        if (healthSlider != null) healthSlider.value = health / maxHealth;
        if (hungerSlider != null) hungerSlider.value = hunger / maxHunger;
        if (thirstSlider != null) thirstSlider.value = thirst / maxThirst;
        if (energySlider != null) energySlider.value = energy / maxEnergy;
    }

    void UpdateSpecialItemUI()
    {
        if (specialItemContentParent == null || specialItemSlotPrefab == null) return;
        foreach (Transform child in specialItemContentParent) Destroy(child.gameObject);

        foreach (KeyValuePair<string, int> item in inventory)
        {
            if (!normalItems.Contains(item.Key))
            {
                GameObject slotGO = Instantiate(specialItemSlotPrefab, specialItemContentParent);
                TextMeshProUGUI itemText = slotGO.GetComponent<TextMeshProUGUI>();
                if (itemText != null) itemText.text = $"{item.Key}: {item.Value}";
            }
        }
    }

    void UpdateGoldUI()
    {
        if (goldText != null) goldText.text = "Gold: " + gold;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    public void AddItem(string itemName, int count = 1)
    {
        if (inventory.ContainsKey(itemName)) inventory[itemName] += count;
        else inventory.Add(itemName, count);
        UpdateAllUI();
    }

    public void RemoveItem(string itemName, int count)
    {
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] -= count;
            if (inventory[itemName] <= 0) inventory.Remove(itemName);
            UpdateAllUI();
        }
    }

    public bool UseEnergy(float amount)
    {
        if (energy >= amount)
        {
            energy -= amount;
            UpdateAllUI();
            return true;
        }
        return false;
    }

    public void RestoreHunger(float amount)
    {
        hunger += amount;
        if (hunger > maxHunger) hunger = maxHunger;
        UpdateAllUI();
    }

    public void RefillThirst()
    {
        thirst = maxThirst;
        UpdateAllUI();
    }

    public void Sleep()
    {
        energy = maxEnergy;
        GameTimeManager timeManager = FindObjectOfType<GameTimeManager>();
        if (timeManager != null)
        {
            timeManager.AdvanceDay();
        }
        UpdateAllUI();
    }

    public void TransferItem(string itemName, bool fromInventoryToStorage)
    {
        int count = fromInventoryToStorage ? inventory[itemName] : storage[itemName];

        if (fromInventoryToStorage)
        {
            inventory.Remove(itemName);
            if (storage.ContainsKey(itemName)) storage[itemName] += count;
            else storage.Add(itemName, count);
        }
        else
        {
            storage.Remove(itemName);
            if (inventory.ContainsKey(itemName)) inventory[itemName] += count;
            else inventory.Add(itemName, count);
        }
    }

    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        Time.timeScale = 0f;
        PlayerAction player = FindObjectOfType<PlayerAction>();
        if (player != null)
        {
            player.enabled = false;
        }
    }

    T FindComponentInChild<T>(Transform parent, string name) where T : Component
    {
        if (parent == null) return null;
        T[] components = parent.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component.gameObject.name == name)
            {
                return component;
            }
        }
        return null;
    }
    // DataManager.cs 스크립트 안에 추가

    public void RestartGame()
    {
        // 1. 멈췄던 게임 시간을 다시 흐르게 합니다.
        Time.timeScale = 1f;

        // 2. 플레이어 스크립트를 다시 활성화합니다. (선택 사항)
        PlayerAction player = FindObjectOfType<PlayerAction>();
        if (player != null)
        {
            player.enabled = true;
        }

        // 3. 첫 씬인 "Farm" 씬을 다시 불러옵니다.
        SceneManager.LoadScene("Farm");
    }
}