using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 게임 전체 상태(인벤토리, 생존 수치, 마음, 농장, 시장, 저장)를 관리하는 중심 매니저.
// GameManager 오브젝트에 붙어 있고 DontDestroyOnLoad로 씬 사이를 유지합니다.
public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    // --- 데이터 저장 공간 ---
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, int> storage = new Dictionary<string, int>();
    public FarmData farm = new FarmData();
    public MarketState market = new MarketState();
    public DayLog today = new DayLog();
    public StoryState story = new StoryState();
    public int gold = 0;

    [Header("새 게임 설정")]
    public int startingGold = 50;
    public float startingHeart = 20f;

    // --- UI 요소 참조 (씬이 바뀔 때마다 이름으로 다시 찾습니다) ---
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

    [Header("생존 수치 변화 (게임 시간 1시간당)")]
    public float hungerPerGameHour = 5f;
    public float thirstPerGameHour = 7f;
    public float starvingDamagePerGameHour = 15f;
    public float healthRegenPerGameHour = 2f;

    [Header("마음 (Heart) - 번아웃에서 회복한 정도. 세상의 색과 최대 에너지에 영향")]
    public float heart = 20f;
    public float maxHeart = 100f;

    [Header("날씨 / 물뿌리개")]
    public bool raining = false;
    public int waterCharges = 10;

    // --- 이벤트 ---
    public event System.Action OnInventoryChanged;
    public event System.Action OnNewDay;

    public bool IsGameOver { get; private set; }
    public GameTimeManager TimeManager => timeManager;

    private GameTimeManager timeManager;
    private readonly HashSet<string> clearedResources = new HashSet<string>();
    private readonly List<int> heartMilestonesReached = new List<int>();
    private readonly System.Random rng = new System.Random();
    private bool warnedHunger, warnedThirst, journalOpen;
    private float statusTimer;

    private static readonly string[] normalItems = { "Wood", "Stone" };
    private static readonly int[] heartMilestones = { 30, 50, 75, 100 };

    // 마음이 낮으면(번아웃) 하루에 쓸 수 있는 에너지 최대치가 줄어듭니다. (60% ~ 100%)
    public float MaxEnergyNow => maxEnergy * (0.6f + 0.4f * Mathf.Clamp01(heart / maxHeart));

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

        Time.timeScale = 1f;
        HUD.Ensure();
        WorldColor.Ensure();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Start()
    {
        timeManager = GetComponent<GameTimeManager>();
        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();

        if (SaveSystem.HasSave() && LoadGame())
        {
            HUD.Toast($"Welcome back. Day {(timeManager != null ? timeManager.day : 1)}.");
        }
        else
        {
            NewGameState();
        }

        FindUIElements(SceneManager.GetActiveScene());
        ApplyClearedResources(SceneManager.GetActiveScene());
        WorldColor.SetHeart(heart / maxHeart, true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIElements(scene);
        ApplyClearedResources(scene);
    }

    // ===================================================================
    // 매 프레임: 생존 수치, 단축키, 상태 표시
    // ===================================================================
    void Update()
    {
        HandleHotkeys();
        if (IsGameOver) return;

        float minutesPerSecond = timeManager != null ? timeManager.minutesPerSecond : 1f;
        float gameHours = Time.deltaTime * minutesPerSecond / 60f;

        hunger = Mathf.Max(0f, hunger - hungerPerGameHour * gameHours);
        thirst = Mathf.Max(0f, thirst - thirstPerGameHour * gameHours);

        if (hunger <= 0f || thirst <= 0f)
        {
            today.starved = true;
            health = Mathf.Max(0f, health - starvingDamagePerGameHour * gameHours);
            if (health <= 0f)
            {
                GameOver();
                return;
            }
        }
        else if (hunger > 50f && thirst > 50f)
        {
            health = Mathf.Min(maxHealth, health + healthRegenPerGameHour * gameHours);
        }

        CheckStatWarnings();
        UpdateStatusUI();

        statusTimer -= Time.unscaledDeltaTime;
        if (statusTimer <= 0f)
        {
            statusTimer = 0.25f;
            HUD.SetStatus(BuildStatusLine());
        }
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F5) && !IsGameOver)
        {
            SaveGame();
            HUD.Toast("Game saved.");
        }
        if (Input.GetKeyDown(KeyCode.F9) && SaveSystem.HasSave())
        {
            ReloadFromSave();
        }
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (ctrl && shift && Input.GetKeyDown(KeyCode.N))
        {
            StartNewGame();
        }
        if (Input.GetKeyDown(KeyCode.J) && !HUD.DialogueOpen)
        {
            if (journalOpen && HUD.AnnouncementOpen)
            {
                HUD.HideAnnouncement();
                journalOpen = false;
            }
            else
            {
                HUD.Announce(StoryEvents.BuildJournal(), 600f);
                journalOpen = true;
            }
        }
    }

    void CheckStatWarnings()
    {
        if (hunger < 20f && !warnedHunger) { warnedHunger = true; HUD.Toast("You're getting hungry. (Right-click / F to eat)"); }
        if (hunger > 30f) warnedHunger = false;
        if (thirst < 20f && !warnedThirst) { warnedThirst = true; HUD.Toast("You're thirsty. Drink at the well (E)."); }
        if (thirst > 30f) warnedThirst = false;
    }

    string BuildStatusLine()
    {
        string weather = raining ? "<color=#8fb8ff>Rain</color>" : "<color=#ffe28a>Sunny</color>";
        string held = Hotbar.SelectedItem;
        string heldText = string.IsNullOrEmpty(held) ? "" : $"   Holding: {held}";
        int capacity = WaterCapacity;
        string water = capacity > 0 ? $"   Water {waterCharges}/{capacity}" : "";
        return $"<color=#ff8fb1>Heart</color> {heart:0}/{maxHeart:0}   {weather}{water}{heldText}";
    }

    // ===================================================================
    // UI
    // ===================================================================
    void FindUIElements(Scene scene)
    {
        woodText = FindInScene<TextMeshProUGUI>(scene, "WoodText");
        stoneText = FindInScene<TextMeshProUGUI>(scene, "StoneText");
        goldText = FindInScene<TextMeshProUGUI>(scene, "GoldText");
        healthSlider = FindInScene<Slider>(scene, "HealthSlider");
        hungerSlider = FindInScene<Slider>(scene, "HungerSlider");
        thirstSlider = FindInScene<Slider>(scene, "ThirstSlider");
        energySlider = FindInScene<Slider>(scene, "EnergySlider");
        specialItemContentParent = FindInScene<Transform>(scene, "SpecialItemPanel1");
        gameOverPanel = FindInScene<Transform>(scene, "GameOverPanel")?.gameObject;

        // 씬에 저장된 RESTART 버튼은 '씬에 있던(곧 파괴되는) DataManager'를 가리키므로
        // 살아있는 DataManager의 RestartGame을 런타임에 다시 연결합니다.
        if (gameOverPanel != null)
        {
            foreach (Button button in gameOverPanel.GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveListener(RestartGame);
                button.onClick.AddListener(RestartGame);
            }
        }
        UpdateAllUI();
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
        if (woodText != null) woodText.text = "Wood: " + GetCount("Wood");
        if (stoneText != null) stoneText.text = "Stone: " + GetCount("Stone");
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
            if (normalItems.Contains(item.Key)) continue;
            GameObject slotGO = Instantiate(specialItemSlotPrefab, specialItemContentParent);
            TextMeshProUGUI itemText = slotGO.GetComponent<TextMeshProUGUI>();
            if (itemText != null) itemText.text = $"{item.Key}: {item.Value}";
        }
    }

    void UpdateGoldUI()
    {
        if (goldText != null) goldText.text = "Gold: " + gold;
    }

    // ===================================================================
    // 인벤토리
    // ===================================================================
    public int GetCount(string itemName)
    {
        itemName = ItemDB.Normalize(itemName);
        return itemName != null && inventory.TryGetValue(itemName, out int count) ? count : 0;
    }

    public bool HasItem(string itemName, int count = 1) => GetCount(itemName) >= count;

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        UpdateGoldUI();
    }

    public void AddItem(string itemName, int count = 1)
    {
        itemName = ItemDB.Normalize(itemName);
        if (string.IsNullOrEmpty(itemName) || count <= 0) return;

        if (inventory.ContainsKey(itemName)) inventory[itemName] += count;
        else inventory.Add(itemName, count);
        InventoryChanged();
    }

    public void RemoveItem(string itemName, int count)
    {
        itemName = ItemDB.Normalize(itemName);
        if (itemName == null || !inventory.ContainsKey(itemName)) return;

        inventory[itemName] -= count;
        if (inventory[itemName] <= 0) inventory.Remove(itemName);
        ClampWaterCharges();
        InventoryChanged();
    }

    void InventoryChanged()
    {
        UpdateAllUI();
        OnInventoryChanged?.Invoke();
    }

    // 인벤토리 ↔ 보관함 이동 (count개, 보유량보다 많으면 전부)
    public void MoveItem(string itemName, bool fromInventoryToStorage, int count)
    {
        Dictionary<string, int> from = fromInventoryToStorage ? inventory : storage;
        Dictionary<string, int> to = fromInventoryToStorage ? storage : inventory;
        if (!from.TryGetValue(itemName, out int owned) || owned <= 0) return;

        int amount = Mathf.Clamp(count, 1, owned);
        from[itemName] = owned - amount;
        if (from[itemName] <= 0) from.Remove(itemName);
        to[itemName] = (to.TryGetValue(itemName, out int existing) ? existing : 0) + amount;

        ClampWaterCharges();
        InventoryChanged();
    }

    // 예전 코드 호환: 해당 아이템 전부 이동
    public void TransferItem(string itemName, bool fromInventoryToStorage)
    {
        MoveItem(itemName, fromInventoryToStorage, int.MaxValue);
    }

    // ===================================================================
    // 생존 수치 / 마음
    // ===================================================================
    public bool UseEnergy(float amount)
    {
        if (energy >= amount)
        {
            energy -= amount;
            if (energy < 1f) today.exhausted = true;
            UpdateStatusUI();
            return true;
        }
        today.exhausted = true;
        HUD.Toast("You're too tired... Eat something or get some sleep.");
        return false;
    }

    public void RestoreHunger(float amount)
    {
        hunger = Mathf.Min(maxHunger, hunger + amount);
        today.mealsEaten++;
        UpdateStatusUI();
    }

    public void RefillThirst()
    {
        thirst = maxThirst;
        today.drankWater = true;
        UpdateStatusUI();
    }

    // 먹을 수 있는 아이템을 먹습니다.
    public bool Eat(string itemName)
    {
        ItemDef def = ItemDB.Get(itemName);
        if (def == null || !def.IsEdible || !HasItem(def.name)) return false;

        RemoveItem(def.name, 1);
        hunger = Mathf.Min(maxHunger, hunger + def.hunger);
        thirst = Mathf.Min(maxThirst, thirst + def.thirst);
        energy = Mathf.Min(MaxEnergyNow, energy + def.hunger * 0.25f);
        today.mealsEaten++;
        if (def.thirst > 0f) today.drankWater = true;

        string msg = $"Ate {def.name}.";
        if (def.heart > 0f)
        {
            ChangeHeart(def.heart, null);
            msg += $"  <color=#ff8fb1>+{def.heart:0} Heart</color>";
        }
        HUD.Toast(msg);
        UpdateStatusUI();
        return true;
    }

    public void ChangeHeart(float amount, string reason)
    {
        float before = heart;
        heart = Mathf.Clamp(heart + amount, 0f, maxHeart);
        WorldColor.SetHeart(heart / maxHeart);

        if (!string.IsNullOrEmpty(reason) && Mathf.Abs(amount) >= 0.5f)
        {
            string sign = amount > 0 ? "+" : "";
            HUD.Toast($"<color=#ff8fb1>{sign}{amount:0} Heart</color>  {reason}");
        }

        foreach (int milestone in heartMilestones)
        {
            if (before < milestone && heart >= milestone && !heartMilestonesReached.Contains(milestone))
            {
                heartMilestonesReached.Add(milestone);
                HUD.Announce(GetMilestoneText(milestone), 6f);
            }
        }
    }

    string GetMilestoneText(int milestone)
    {
        switch (milestone)
        {
            case 30: return "<size=40>Color returns...</size>\n\nThe gray haze of the city is starting to lift.\nThe sky looks a little bluer today.";
            case 50: return "<size=40>Breathing again</size>\n\nYou notice the smell of the soil and the sound of the wind.\nYou feel like you can do a little more each day.";
            case 75: return "<size=40>Warm days</size>\n\nThe village feels like home now.\nYour fields shine with color.";
            default: return "<size=40>Happiness rebuilt</size>\n\nThe world is full of color again.\nYou built this life with your own hands.";
        }
    }

    // ===================================================================
    // 농사 관련 헬퍼
    // ===================================================================
    public int WaterCapacity
    {
        get
        {
            int capacity = 0;
            foreach (var item in inventory)
            {
                ItemDef def = ItemDB.Get(item.Key);
                if (def != null && def.toolType == ToolType.WateringCan) capacity = Mathf.Max(capacity, def.waterCapacity);
            }
            return capacity;
        }
    }

    public void RefillWater()
    {
        waterCharges = WaterCapacity;
    }

    void ClampWaterCharges()
    {
        waterCharges = Mathf.Clamp(waterCharges, 0, WaterCapacity);
    }

    public void OnHarvest(string itemName, int amount, bool blessed)
    {
        today.harvested += amount;
        if (itemName == "Sunflower") ChangeHeart(2f * amount, "The sunflower makes you smile.");
        if (blessed) ChangeHeart(3f, "A golden harvest!");
        else if (today.harvested == amount) ChangeHeart(1f, "Your first harvest of the day.");
    }

    // ===================================================================
    // 시장
    // ===================================================================
    public int GetSellPrice(string itemName) => market.GetSellPrice(ItemDB.Normalize(itemName));

    // 아이템 1개 판매. 판매 금액을 돌려주고, 팔 수 없으면 0
    public int SellItem(string itemName)
    {
        itemName = ItemDB.Normalize(itemName);
        if (!ItemDB.CanSell(itemName) || !HasItem(itemName)) return 0;

        int price = market.GetSellPrice(itemName);
        RemoveItem(itemName, 1);
        market.RecordSale(itemName);
        AddGold(price);
        return price;
    }

    public void ApplyHaggleResult(float bonus)
    {
        market.haggledToday = true;
        market.haggleBonus = Mathf.Max(market.haggleBonus, bonus);
        if (bonus > 0f) ChangeHeart(1f, "A friendly chat with the merchant.");
    }

    // ===================================================================
    // 자원(나무/돌/사과) - 하루 동안 사라졌다가 다음 날 다시 자랍니다.
    // ===================================================================
    static string ResourceKey(string sceneName, GameObject go)
    {
        Vector3 p = go.transform.position;
        return $"{sceneName}:{go.tag}:{Mathf.RoundToInt(p.x * 10f)},{Mathf.RoundToInt(p.y * 10f)}";
    }

    public void MarkResourceCleared(GameObject go)
    {
        clearedResources.Add(ResourceKey(go.scene.name, go));
    }

    void ApplyClearedResources(Scene scene)
    {
        if (clearedResources.Count == 0) return;
        string[] tags = { "Wood", "Stone", "Food" };
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (!tags.Contains(go.tag)) continue;
                if (clearedResources.Contains(ResourceKey(scene.name, go))) Destroy(go);
            }
        }
    }

    // ===================================================================
    // 하루의 끝 (잠자기 / 기절)
    // ===================================================================
    public void Sleep()
    {
        TrySleep();
    }

    public bool TrySleep()
    {
        if (IsGameOver) return false;
        int hour = timeManager != null ? timeManager.hour : 20;
        bool tired = energy <= MaxEnergyNow * 0.25f;
        if (hour < 18 && !tired)
        {
            HUD.Toast("It's too early to sleep. (You can sleep after 18:00)");
            return false;
        }
        EndDay(false);
        return true;
    }

    // 새벽 2시까지 깨어 있으면 기절 → 집에서 눈을 뜹니다.
    public void PassOut()
    {
        if (IsGameOver) return;
        EndDay(true);
        if (SceneManager.GetActiveScene().name != "Home" && Application.CanStreamedLevelBeLoaded("Home"))
        {
            SceneData.nextPlayerPosition = new Vector2(0f, -4.5f);
            SceneData.hasNextPlayerPosition = true;
            SceneManager.LoadScene("Home");
        }
    }

    void EndDay(bool passedOut)
    {
        int hour = timeManager != null ? timeManager.hour : 22;
        var lines = new List<string>();
        float delta = 0f;

        if (passedOut)
        {
            delta -= 8f;
            lines.Add("Passed out from exhaustion  <color=#ff8080>-8</color>");
            int lost = Mathf.Min(gold / 10, 100);
            if (lost > 0)
            {
                gold -= lost;
                lines.Add($"Someone carried you home... you lost {lost}G");
            }
        }
        else if (hour < 24)
        {
            delta += 4f;
            lines.Add("Went to bed on time  <color=#9dff9d>+4</color>");
        }
        else
        {
            delta -= 2f;
            lines.Add("Stayed up too late  <color=#ff8080>-2</color>");
        }

        if (today.mealsEaten > 0) { delta += 3f; lines.Add("Ate proper meals  <color=#9dff9d>+3</color>"); }
        if (today.drankWater) { delta += 1f; lines.Add("Drank fresh water  <color=#9dff9d>+1</color>"); }
        if (!today.exhausted) { delta += 3f; lines.Add("Didn't overwork  <color=#9dff9d>+3</color>"); }
        else { delta -= 3f; lines.Add("Worked until exhausted  <color=#ff8080>-3</color>"); }
        if (today.starved) { delta -= 5f; lines.Add("Went hungry or thirsty  <color=#ff8080>-5</color>"); }
        if (today.harvested > 0) { delta += 2f; lines.Add($"Harvested {today.harvested} crops  <color=#9dff9d>+2</color>"); }

        ChangeHeart(delta, null);

        // 밤사이 변화
        hunger = Mathf.Max(0f, hunger - 15f);
        thirst = Mathf.Max(0f, thirst - 20f);
        health = Mathf.Min(maxHealth, health + 30f);

        // 새로운 날
        raining = rng.NextDouble() < (raining ? 0.15 : 0.25);
        int matured = farm.AdvanceDay(raining);
        market.StartNewDay(rng);
        clearedResources.Clear();
        today = new DayLog();
        energy = passedOut ? MaxEnergyNow * 0.5f : MaxEnergyNow;
        if (timeManager != null) timeManager.AdvanceDay();

        SaveGame();

        int day = timeManager != null ? timeManager.day : 0;
        string header = $"<size=42>Day {day}</size>\n{(raining ? "It's raining. The fields are watered." : "A clear morning.")}";
        if (matured > 0) header += $"\n{matured} crop(s) are ready to harvest!";
        string body = string.Join("\n", lines);
        string sign = delta >= 0 ? "+" : "";
        HUD.Announce($"{header}\n\n<size=26>{body}</size>\n\n<color=#ff8fb1>Heart {sign}{delta:0}  ({heart:0}/{maxHeart:0})</color>", 7f);

        OnNewDay?.Invoke();
        InventoryChanged();
        StoryEvents.OnNewDay(day);
        StoryEvents.CheckEnding();
    }

    // ===================================================================
    // 게임 오버 / 재시작
    // ===================================================================
    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        PlayerAction player = PlayerAction.instance;
        if (player != null) player.enabled = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // 게임오버 패널이 없는 씬(집, 시장 등)에서는 잠시 후 자동 재시작
            HUD.Announce("<size=42>You collapsed...</size>\n\nTake better care of yourself.\nWaking up from your last save.", 3f);
            Time.timeScale = 0f;
            StartCoroutine(AutoRestart());
        }
    }

    IEnumerator AutoRestart()
    {
        yield return new WaitForSecondsRealtime(3f);
        RestartGame();
    }

    // 게임오버 화면의 RESTART 버튼: 마지막 저장(마지막 아침)에서 다시 시작합니다.
    public void RestartGame()
    {
        // 버튼에 리스너가 중복 연결되어 있어도 한 번만 실행되도록
        if (!IsGameOver) return;
        Time.timeScale = 1f;
        IsGameOver = false;

        PlayerAction player = PlayerAction.instance;
        if (player != null) player.enabled = true;

        if (!(SaveSystem.HasSave() && LoadGame()))
        {
            NewGameState();
        }
        SceneManager.LoadScene("Farm");
    }

    public void StartNewGame()
    {
        SaveSystem.DeleteSave();
        Time.timeScale = 1f;
        IsGameOver = false;
        NewGameState();
        HUD.Toast("Started a new game.");
        SceneManager.LoadScene("Farm");
    }

    void ReloadFromSave()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        if (LoadGame())
        {
            HUD.Toast("Loaded the last save.");
            SceneManager.LoadScene("Farm");
        }
    }

    // ===================================================================
    // 새 게임 / 저장 / 불러오기
    // ===================================================================
    void NewGameState()
    {
        inventory.Clear();
        storage.Clear();
        inventory["Hoe"] = 1;
        inventory["Watering Can"] = 1;
        inventory["Scythe"] = 1;
        inventory["Carrot Seed"] = 5;
        inventory["Apple"] = 3;

        gold = startingGold;
        health = maxHealth;
        hunger = maxHunger;
        thirst = maxThirst;
        heart = startingHeart;
        energy = MaxEnergyNow;
        heartMilestonesReached.Clear();

        farm = new FarmData();
        market = new MarketState();
        market.StartNewDay(rng);
        today = new DayLog();
        story = new StoryState();
        clearedResources.Clear();
        raining = false;
        waterCharges = WaterCapacity;

        if (timeManager != null) timeManager.SetTime(1, 6, 0f);
        WorldColor.SetHeart(heart / maxHeart, true);
        InventoryChanged();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            gold = gold,
            health = health,
            hunger = hunger,
            thirst = thirst,
            energy = energy,
            heart = heart,
            farm = farm,
            market = market,
            today = today,
            story = story,
            raining = raining,
            waterCharges = waterCharges,
            clearedResources = clearedResources.ToList(),
            heartMilestonesReached = new List<int>(heartMilestonesReached),
        };
        foreach (var item in inventory) data.inventory.Add(new ItemStack(item.Key, item.Value));
        foreach (var item in storage) data.storage.Add(new ItemStack(item.Key, item.Value));

        if (timeManager != null)
        {
            data.day = timeManager.day;
            data.hour = timeManager.hour;
            data.minute = timeManager.minute;
        }
        SaveSystem.Save(data);
    }

    public bool LoadGame()
    {
        SaveData data = SaveSystem.Load();
        if (data == null) return false;

        gold = data.gold;
        health = data.health;
        hunger = data.hunger;
        thirst = data.thirst;
        energy = data.energy;
        heart = data.heart;
        farm = data.farm ?? new FarmData();
        market = data.market ?? new MarketState();
        today = data.today ?? new DayLog();
        story = data.story ?? new StoryState();
        raining = data.raining;

        inventory.Clear();
        foreach (var stack in data.inventory) AddToDict(inventory, stack);
        storage.Clear();
        foreach (var stack in data.storage) AddToDict(storage, stack);

        clearedResources.Clear();
        if (data.clearedResources != null) foreach (var key in data.clearedResources) clearedResources.Add(key);
        heartMilestonesReached.Clear();
        if (data.heartMilestonesReached != null) heartMilestonesReached.AddRange(data.heartMilestonesReached);

        waterCharges = Mathf.Clamp(data.waterCharges, 0, WaterCapacity);
        if (timeManager != null) timeManager.SetTime(data.day, data.hour, data.minute);

        WorldColor.SetHeart(heart / maxHeart, true);
        InventoryChanged();
        return true;
    }

    static void AddToDict(Dictionary<string, int> dict, ItemStack stack)
    {
        if (stack == null || string.IsNullOrEmpty(stack.itemName) || stack.count <= 0) return;
        string name = ItemDB.Normalize(stack.itemName);
        dict[name] = (dict.TryGetValue(name, out int c) ? c : 0) + stack.count;
    }

    // ===================================================================
    // 유틸
    // ===================================================================
    static T FindInScene<T>(Scene scene, string name) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == name) return component;
            }
        }
        return null;
    }
}
