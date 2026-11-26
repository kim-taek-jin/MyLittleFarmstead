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
    public bool snowing = false;   // 겨울(마음의 계절)에는 비 대신 눈 (밭을 적시지 않음)
    public int waterCharges = 10;

    [Header("데모 빌드")]
    [Tooltip("켜면 '봄'이 오는 순간 데모가 끝납니다 (계절은 겨울에 머묾). DEMO_BUILD 심볼로도 켤 수 있습니다.")]
    public bool demoMode = false;
    public string wishlistUrl = "https://store.steampowered.com/";

    // --- 이벤트 ---
    public event System.Action OnInventoryChanged;
    public event System.Action OnNewDay;

    public bool IsGameOver { get; private set; }
    public GameTimeManager TimeManager => timeManager;
    public Season CurrentSeason => (Season)story.season;

    private GameTimeManager timeManager;
    private readonly HashSet<string> clearedResources = new HashSet<string>();
    private readonly List<int> heartMilestonesReached = new List<int>();
    private readonly System.Random rng = new System.Random();
    private bool warnedHunger, warnedThirst, journalOpen;
    private float statusTimer;

    private static readonly string[] normalItems = { "Wood", "Stone" };
    private static readonly int[] heartMilestones = { 30, 50, 75, 100 };

    // 마음이 낮으면(번아웃) 하루에 쓸 수 있는 에너지 최대치가 줄어듭니다. (60% ~ 100%)
    public float MaxEnergyNow => maxEnergy * (0.6f + 0.4f * Mathf.Clamp01(heart / maxHeart))
                                 * (1f + 0.1f * SpiritDB.PerkLevel(story != null ? story.spirit : null, "sun"));

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
#if DEMO_BUILD
        demoMode = true;
#endif
        HUD.Ensure();
        WorldColor.Ensure();
        WeatherFX.Ensure();
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
            // "다시 왔어요"는 타이틀 화면에서 이어하기를 누를 때 (TitleScreen)
        }
        else
        {
            NewGameState();
        }

        FindUIElements(SceneManager.GetActiveScene());
        ApplyClearedResources(SceneManager.GetActiveScene());
        WorldColor.SetHeart(heart / maxHeart, true);
        Spirit.EnsureSpawned();
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
        if (TitleScreen.Blocking) return;      // 타이틀 화면, 오프닝 중에는 단축키를 받지 않음
        if (Input.GetKeyDown(KeyCode.F5) && !IsGameOver)
        {
            SaveGame();
            HUD.Toast(Loc.T("Game saved.", "저장했어요."));
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
        if (Input.GetKeyDown(KeyCode.P) && !HUD.DialogueOpen && !HUD.AlbumOpen) HUD.ToggleAlbum();
        if (demoMode && story.IsUnlocked("demo_end") && HUD.AnnouncementOpen && Input.GetKeyDown(KeyCode.Return))
        {
            Application.OpenURL(wishlistUrl);
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
        if (hunger < 20f && !warnedHunger) { warnedHunger = true; HUD.Toast(Loc.T("You're getting hungry. (Right-click / F to eat)", "배가 고파요. (오른쪽 클릭 / F: 먹기)")); }
        if (hunger > 30f) warnedHunger = false;
        if (thirst < 20f && !warnedThirst) { warnedThirst = true; HUD.Toast(Loc.T("You're thirsty. Drink at the well (E).", "목이 말라요. 우물에서 물을 마셔요 (E).")); }
        if (thirst > 30f) warnedThirst = false;
    }

    string BuildStatusLine()
    {
        string weather = snowing ? "<color=#e4ecf4>" + Loc.T("Snow", "눈") + "</color>" : (raining ? "<color=#8fb8ff>" + Loc.T("Rain", "비") + "</color>" : "<color=#ffe28a>" + Loc.T("Sunny", "맑음") + "</color>");
        weather = $"<color={SeasonSystem.Color(CurrentSeason)}>{SeasonSystem.Name(CurrentSeason)}</color>  {weather}";
        string held = Hotbar.SelectedItem;
        string heldText = string.IsNullOrEmpty(held) ? "" : "   " + Loc.T("Holding: ", "손에 든 것: ") + ItemNames.Display(held);
        int capacity = WaterCapacity;
        string water = capacity > 0 ? "   " + Loc.T("Water", "물") + $" {waterCharges}/{capacity}" : "";
        return "<color=#ff8fb1>" + Loc.T("Heart", "마음") + $"</color> {heart:0}/{maxHeart:0}   {weather}{water}{heldText}";
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
        if (amount > 0) Sfx.Play("coin", 1f, 0.1f);
        UpdateGoldUI();
        if (gold >= 1000) Album.Report("gold1000");
    }

    public void AddItem(string itemName, int count = 1)
    {
        itemName = ItemDB.Normalize(itemName);
        if (string.IsNullOrEmpty(itemName) || count <= 0) return;

        if (inventory.ContainsKey(itemName)) inventory[itemName] += count;
        else inventory.Add(itemName, count);
        InventoryChanged();
        Sfx.Play("pickup", 0.8f, 0.12f);

        if ((itemName == "Sunshine Shard" || itemName == "Earth's Breath") && !story.spirit.found) Spirit.Discover();
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
        HUD.Toast(Loc.T("You're too tired... Eat something or get some sleep.", "너무 지쳤어요... 뭘 좀 먹거나 잠을 자요."));
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
        Album.Report("drink");
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

        if (def.category == ItemCategory.Food && def.heart > 0f) Album.Report("eat_cooked", def.name);
        string msg = Loc.T($"Ate {def.name}.", $"{ItemNames.Display(def.name)}을(를) 먹었어요.");
        if (def.heart > 0f)
        {
            ChangeHeart(def.heart, null);
            msg += $"  <color=#ff8fb1>+{def.heart:0} " + Loc.T("Heart", "마음") + "</color>";
        }
        HUD.Toast(msg);
        UpdateStatusUI();
        return true;
    }

    public void ChangeHeart(float amount, string reason)
    {
        float before = heart;
        heart = Mathf.Clamp(heart + amount, 0f, maxHeart);
        if (amount >= 1f && heart > before) Sfx.Play("heart", 0.8f, 0.5f);
        if (amount >= 2f && heart > before && !string.IsNullOrEmpty(reason)) ColorWave.Request(false);   // 낮에 생긴 좋은 일 (아침 요약은 아래에서)
        WorldColor.SetHeart(heart / maxHeart);

        if (!string.IsNullOrEmpty(reason) && Mathf.Abs(amount) >= 0.5f)
        {
            // 숫자 대신 작은 하트 (오르면 분홍, 내리면 흐린 파랑): 마음은 점수가 아니라 느낌으로
            HUD.Toast((amount > 0 ? "<color=#ff8fb1>♥</color>  " : "<color=#9aa4c0>♡</color>  ") + reason);
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
            case 30: return Loc.T("<size=40>Color returns...</size>\n\nThe gray haze of the city is starting to lift.\nThe sky looks a little bluer today.", "<size=40>색이 돌아와요...</size>\n\n도시의 잿빛 안개가 걷히기 시작했어요.\n오늘은 하늘이 조금 더 파래 보여요.");
            case 50: return Loc.T("<size=40>Breathing again</size>\n\nYou notice the smell of the soil and the sound of the wind.\nYou feel like you can do a little more each day.", "<size=40>다시 숨이 쉬어져요</size>\n\n흙 냄새와 바람 소리가 들어오기 시작했어요.\n하루하루 조금씩 더 할 수 있을 것 같아요.");
            case 75: return Loc.T("<size=40>Warm days</size>\n\nThe village feels like home now.\nYour fields shine with color.", "<size=40>따뜻한 날들</size>\n\n이제 이 마을이 집처럼 느껴져요.\n밭이 색으로 반짝여요.");
            default: return Loc.T("<size=40>Happiness rebuilt</size>\n\nThe world is full of color again.\nYou built this life with your own hands.", "<size=40>다시 찾은 행복</size>\n\n세상이 다시 색으로 가득해요.\n이 삶은 내 손으로 일군 거예요.");
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
        Album.Report("harvest", itemName);
        if (blessed) Album.Report("golden", itemName);
        if (itemName == "Sunflower") ChangeHeart(2f * amount, Loc.T("The sunflower makes you smile.", "해바라기를 보니 웃음이 나요."));
        if (blessed) ChangeHeart(3f, Loc.T("A golden harvest!", "황금빛 수확!"));
        else if (today.harvested == amount) ChangeHeart(1f, Loc.T("Your first harvest of the day.", "오늘의 첫 수확."));
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

    public int Today => timeManager != null ? timeManager.day : 0;

    // 나눔 점수: 나눔 상자(하루 최대 5점), 주민 소원(+3). 60이 되면 별의 언덕이 열립니다.
    // fromShareBox가 true면 이미 StoryState.AddShare로 점수를 더한 상태입니다.
    public void AddKindness(int amount, bool fromShareBox = false)
    {
        if (amount <= 0) return;
        if (!fromShareBox) story.kindness += amount;

        int day = Today;
        if (story.donationDay != day)
        {
            story.donationDay = day;
            ChangeHeart(2f, Loc.T("Sharing warms your heart.", "나누니 마음이 따뜻해져요."));
        }

        if (story.kindness >= StarHill.KindnessToUnlock && story.Unlock("starhill"))
        {
            HUD.Announce(
                Loc.T("<size=40>Word of your kindness spreads</size>\n\n" +
                      $"{VillagerDB.Get("rosa").name} whispers: \"On clear nights, look for the glowing vine in the meadow below your farm road.\n" +
                      "It leads up to the hill where stars come down to rest.\"\n\n" +
                      "<size=24>Star Hill unlocked: clear nights after 20:00, the meadow east of the farm.</size>",
                      "<size=40>나눔 소문이 퍼졌어요</size>\n\n" +
                      $"{VillagerDB.Get("rosa").name}가 속삭여요: \"맑은 밤에 농장 큰길 아래 풀밭을 봐요. 빛나는 덩굴이 있을 거야.\n" +
                      "별들이 내려와 쉬는 언덕으로 이어져 있지.\"\n\n" +
                      "<size=24>별의 언덕이 열렸어요: 맑은 밤 20시 이후, 농장 동쪽 풀밭</size>"), 9f);
        }
    }

    public void ApplyHaggleResult(float bonus)
    {
        market.haggledToday = true;
        market.haggleBonus = Mathf.Max(market.haggleBonus, bonus);
        if (bonus > 0f) Album.Report("haggle");
        if (bonus > 0f) ChangeHeart(1f, Loc.T("A friendly chat at the market.", "장터에서 정답게 흥정했어요."));
    }

    // ===================================================================
    // 자원(나무/돌/사과) - 하루 동안 사라졌다가 다음 날 다시 자랍니다.
    // ===================================================================
    static string ResourceKey(string sceneName, GameObject go)
    {
        Vector3 p = go.transform.position;
        return $"{sceneName}:{go.tag}:{Mathf.RoundToInt(p.x * 10f)},{Mathf.RoundToInt(p.y * 10f)}";
    }

    public bool IsResourceCleared(string sceneName, GameObject go) => clearedResources.Contains(ResourceKey(sceneName, go));

    // 개발용 단축키(DevKeys)에서 하루 넘기기
    public void DevEndDay() => EndDay(false);

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
            HUD.Toast(Loc.T("It's too early to sleep. (You can sleep after 18:00)", "아직 잠들기엔 일러요. (18시부터 잘 수 있어요)"));
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

    // 일기에 쓰려고 기록: 이웃과 이야기, 쉼터에서 쉼
    public void NoteTalk() { today.talked++; }
    public void NoteRest() { today.rested = true; }
    public void NotePumasi() { today.pumasi = true; }
    public void NoteSaecham() { today.saecham = true; }
    public void NoteMasil() { today.masil = true; }
    public void NoteRite(string diaryLine) { today.riteNote = diaryLine; }

    void EndDay(bool passedOut)
    {
        var facts = new DayFacts { passedOut = passedOut, meals = today.mealsEaten, harvested = today.harvested, talked = today.talked,
                                   drank = today.drankWater, exhausted = today.exhausted, starved = today.starved, rested = today.rested,
                                   pumasi = today.pumasi, saecham = today.saecham, masil = today.masil, riteNote = today.riteNote };
        bool forestWas = story.forest != null && story.forest.open, lakeWas = story.lake != null && story.lake.open, hillWas = story.hill != null && story.hill.open;
        int hour = timeManager != null ? timeManager.hour : 22;
        var lines = new List<string>();
        float delta = 0f;

        if (passedOut)
        {
            delta -= 8f;
            lines.Add(Loc.T("Passed out from exhaustion", "지쳐서 쓰러졌어요") + "  <color=#ff8080>-8</color>");
            int lost = Mathf.Min(gold / 10, 100);
            if (lost > 0)
            {
                gold -= lost;
                lines.Add(Loc.T($"Someone carried you home... you lost {lost}G", $"누군가 집까지 데려다줬어요... {lost}G를 잃었어요"));
            }
        }
        else if (hour < 24)
        {
            facts.onTime = true;
            delta += 4f;
            lines.Add(Loc.T("Went to bed on time", "제때 잠자리에 들었어요") + "  <color=#9dff9d>+4</color>");
            Album.Report("sleep_early");
        }
        else
        {
            facts.lateNight = true;
            delta -= 2f;
            lines.Add(Loc.T("Stayed up too late", "너무 늦게까지 깨어 있었어요") + "  <color=#ff8080>-2</color>");
        }

        if (today.mealsEaten > 0) { delta += 3f; lines.Add(Loc.T("Ate proper meals", "밥을 챙겨 먹었어요") + "  <color=#9dff9d>+3</color>"); }
        if (today.drankWater) { delta += 1f; lines.Add(Loc.T("Drank fresh water", "시원한 물을 마셨어요") + "  <color=#9dff9d>+1</color>"); }
        if (!today.exhausted) { delta += 3f; lines.Add(Loc.T("Didn't overwork", "무리하지 않았어요") + "  <color=#9dff9d>+3</color>"); }
        else { delta -= 3f; lines.Add(Loc.T("Worked until exhausted", "기력이 바닥날 때까지 일했어요") + "  <color=#ff8080>-3</color>"); }
        if (today.starved) { delta -= 5f; lines.Add(Loc.T("Went hungry or thirsty", "배고프거나 목마른 채로 지냈어요") + "  <color=#ff8080>-5</color>"); }
        if (today.harvested > 0) { delta += 2f; lines.Add(Loc.T($"Harvested {today.harvested} crops", $"작물 {today.harvested}개를 거뒀어요") + "  <color=#9dff9d>+2</color>"); }
        // K-2: 아궁이에 불을 땠으면 방이 뜨끈 (구들), 장독은 그날 날씨와 뚜껑으로 돌봄을 셈
        story.jang.Ensure();
        if (story.jang.fireToday && !passedOut) { facts.warm = true; delta += 2f; lines.Add(Loc.T("Slept on a warm floor", "뜨끈한 방바닥에서 잤어요") + "  <color=#9dff9d>+2</color>"); }
        story.jang.AdvanceNight(raining || snowing);
        // K-4: 대보름 소원지에 쓴 대로 산 날
        story.rites.Ensure();
        if (Rites.WishKept(story.rites.wish, facts)) { delta += 1f; facts.wishLine = Rites.WishKeptLine(story.rites.wish); }

        ChangeHeart(delta, null);

        // 밤사이 변화
        hunger = Mathf.Max(0f, hunger - 15f);
        thirst = Mathf.Max(0f, thirst - 20f);
        health = Mathf.Min(maxHealth, health + 30f);

        // ---- 마음의 계절: 아침에만 바뀜 ----
        Season before = CurrentSeason;
        Season cap = demoMode ? Season.Winter : Season.Autumn;
        // K-4: 겨울 → 봄은 정월대보름 밤을 보낸 뒤에만. 마음이 봄 문턱에 닿으면 대보름 날이 정해짐
        bool springReady = SeasonSystem.ForHeart(heart) >= Season.Spring;
        int newDay = (timeManager != null ? timeManager.day : 0) + 1;
        bool holdSpring = before == Season.Winter && Rites.HoldSpring(story.rites);
        if (holdSpring && springReady && Rites.Schedule(story.rites, newDay))
            lines.Add(Loc.T($"Spring is at the door. The village will greet it on Jeongwol Daeboreum (day {story.rites.boreumDay}).",
                            $"봄이 문 앞에 와 있어요. 마을은 정월대보름({story.rites.boreumDay}일째)에 봄을 맞아요."));
        bool demoEndsNow = demoMode && springReady && story.rites.boreumDone && !story.IsUnlocked("demo_end");
        Season after = holdSpring ? Season.Winter : SeasonSystem.Evaluate(heart, before, cap);
        story.season = (int)after;
        if ((int)after > story.highestSeason) story.highestSeason = (int)after;
        // 색 물결: 아침 요약을 닫으면 퍼짐 (계절이 바뀐 아침은 크게)
        if (after > before) ColorWave.Request(true);
        else if (delta >= 4f) ColorWave.Request(false);
        if (after >= Season.Autumn) story.Unlock("season_autumn");

        // ---- 마음을 따라가는 날씨 (겨울에는 눈: 밭을 적시지 않음) ----
        bool wet = rng.NextDouble() < SeasonSystem.RainChance(after, delta);
        snowing = wet && after == Season.Winter;
        raining = wet && after != Season.Winter;

        // 새로운 날
        int earthLevel = SpiritDB.PerkLevel(story.spirit, "earth");
        int matured = farm.AdvanceDay(raining, 0.1f * earthLevel, rng, after);
        // 미나의 품 (K-3): 맑은 아침이면 가끔 몰래 밭에 물을 주고 감
        int nextDay = (timeManager != null ? timeManager.day : 0) + 1;
        if (!raining && story.IsUnlocked("favor_water") && Neighbors.MinaWatersToday(nextDay))
        {
            int w = farm.WaterSome(Neighbors.MinaWaters);
            if (w > 0) lines.Add(Loc.T($"Small footprints by the field: Mina watered {w} crop(s) this morning", $"밭가에 작은 발자국: 미나가 아침에 작물 {w}개에 물을 주고 갔어요"));
        }
        // 텃밭: 작물 없이 밤을 보낸 뙈기는 쉰 땅, 퇴비 더미는 두 개마다 퇴비 한 포대
        int compostBefore = farm.compostReady;
        int rested = Plots.AdvanceNight(farm, FarmMap.PlotList);
        if (rested > 0) lines.Add(Loc.T($"{rested} plot(s) rested overnight (rich soil for the next crop)", $"텃밭 {rested}뙈기가 하룻밤 쉬었어요 (다음 작물은 땅심 보너스)"));
        if (farm.compostReady > compostBefore) lines.Add(Loc.T($"The compost heap made {farm.compostReady - compostBefore} bag(s) of compost", $"퇴비 더미에서 퇴비 {farm.compostReady - compostBefore}포대가 다 됐어요"));
        int bloomLevel = SpiritDB.PerkLevel(story.spirit, "bloom");
        if (bloomLevel > 0)
        {
            delta += bloomLevel;
            lines.Add(Loc.T("The dokkaebi fire kept the room warm all night", "도깨비불이 밤새 방을 따뜻하게 해 줬어요") + $"  <color=#9dff9d>+{bloomLevel}</color>");
            ChangeHeart(bloomLevel, null);
        }
        market.StartNewDay(rng, after);
        clearedResources.Clear();
        today = new DayLog();
        energy = passedOut ? MaxEnergyNow * 0.5f : MaxEnergyNow;
        if (timeManager != null) timeManager.AdvanceDay();

        // 숲: 봄이 오면 길이 열리고, 물 받은 묘목이 자라고, 새 채집물이 생김
        float forestDelta = ForestArea.Morning(this, raining, timeManager != null ? timeManager.day : 0, lines, out string forestHeadline);
        LakeArea.Morning(this, timeManager != null ? timeManager.day : 0, out string lakeHeadline);
        if (lakeHeadline != null) forestHeadline = forestHeadline == null ? lakeHeadline : forestHeadline + "\n" + lakeHeadline;
        HillArea.Morning(this, out string hillHeadline);
        if (hillHeadline != null) forestHeadline = forestHeadline == null ? hillHeadline : forestHeadline + "\n" + hillHeadline;
        if (forestDelta != 0f)
        {
            delta += forestDelta;
            ChangeHeart(forestDelta, null);
        }

        SaveGame();

        int day = timeManager != null ? timeManager.day : 0;
        string weatherLine = snowing ? Loc.T("Soft snow is falling.", "포근한 눈이 내려요.") : (raining ? Loc.T("It's raining. The fields are watered.", "비가 와요. 밭에 물이 들었어요.") : Loc.T("A clear morning.", "맑은 아침이에요."));
        if (delta >= 6f && !raining && !snowing) weatherLine = Loc.T("A clear morning. Yesterday was a good day.", "맑은 아침이에요. 어제는 좋은 하루였어요.");
        else if (delta < 0f && (raining || snowing)) weatherLine += Loc.T(" The sky feels as heavy as you do.", " 하늘도 내 마음만큼 무거워요.");
        string header = "<size=42>" + Loc.T($"{SeasonSystem.Name(after)}, Day {day}", $"{SeasonSystem.Name(after)}, {day}일째") + $"</size>\n{weatherLine}";
        if (matured > 0) header += "\n" + Loc.T($"{matured} crop(s) are ready to harvest!", $"거둘 작물이 {matured}개 있어요!");

        if (after > before)
        {
            header = $"<size=44><color={SeasonSystem.Color(after)}>" + Loc.T($"{SeasonSystem.Name(after)} has come", $"{SeasonSystem.Name(after)}이 왔어요") + "</color></size>\n" +
                     $"<size=24>{SeasonSystem.Arrival(after)}\n{NewCropsText(after)}</size>\n\n<size=30>" + Loc.T($"Day {day}", $"{day}일째") + "</size>";
        }
        else if (after < before)
        {
            header = "<size=42><color=#c8d8ff>" + Loc.T("A cold snap...", "꽃샘추위...") + "</color></size>\n<size=24>" +
                     Loc.T($"You pushed yourself too hard. It feels like {SeasonSystem.Name(after)} again.\nSome crops stopped growing for now. They'll wait for you.",
                           $"너무 무리했어요. 다시 {SeasonSystem.Name(after)} 같아요.\n몇몇 작물이 잠시 자라기를 멈췄어요. 기다려 줄 거예요.") +
                     "</size>\n\n<size=30>" + Loc.T($"Day {day}", $"{day}일째") + "</size>";
        }
        if (forestHeadline != null) header += $"\n<size=24>{forestHeadline}</size>";
        if (raining) Album.Report("rain_morning");

        // 어제의 일기 (점수 대신 1인칭 두세 줄) + 마음의 말. 밭·숲 소식은 숫자를 빼고 아래에 작게
        facts.delta = delta;
        string diary = string.Join("\n", DayDiary.Lines(facts, day - 1));
        var notes = new List<string>();
        foreach (string l in lines)
        {
            if (l.Contains("<color=#9dff9d>+") || l.Contains("<color=#ff8080>-"))
            {
                // 점수 줄(잠, 밥, 물, 무리, 수확)은 일기가 대신함. 숲 빈터처럼 소식인 줄만 숫자를 떼고 남김
                if (!IsScoreLine(l)) notes.Add(System.Text.RegularExpressions.Regex.Replace(l, @"\s*<color=#[0-9a-f]{6}>[+-]\d+</color>", ""));
            }
            else notes.Add(l);
        }
        string news = notes.Count > 0 ? "\n\n<size=22><color=#c8c0b0>" + string.Join("\n", notes) + "</color></size>" : "";
        string preview = BuildTodayPreview(after, day);
        HUD.Announce($"{header}\n\n<size=22><color=#e8d8b0>" + Loc.T("— Yesterday's diary —", "— 어제의 일기 —") + $"</color></size>\n<size=26>{diary}</size>\n" +
                     $"<size=24><color=#ff8fb1>{DayDiary.Mood(delta)}</color></size>{news}{preview}", 9f);

        // 아침 마을 방송 (확성기): 장날, 날씨, 계절, 새로 열린 길
        BroadcastBanner.Queue(VillageBroadcast.Lines(new MorningNews
        {
            day = day, before = before, after = after, raining = raining, snowing = snowing,
            mejuSeason = after == Season.Winter && story.jang.Fermenting() == 0 && story.jang.hung.Count == 0,
            lidsOpen = story.jang.lidsOpen,
            helpWanted = Neighbors.HelpWanted(story, VillagerDB.All, day),
            rite = Rites.Broadcast(story.rites, day),
            forestOpened = !forestWas && story.forest != null && story.forest.open,
            lakeOpened = !lakeWas && story.lake != null && story.lake.open,
            hillOpened = !hillWas && story.hill != null && story.hill.open,
        }));

        if (demoEndsNow)
        {
            story.Unlock("demo_end");
            HUD.Announce(Loc.T("<size=44><color=#ffb6d0>Spring is coming...</color></size>\n\n" +
                               "You made it through the winter.\nThe rest of the year is waiting in the full game.\n\n" +
                               "<size=28>Thank you for playing the demo!</size>\n<size=24>[Enter] Wishlist on Steam</size>",
                               "<size=44><color=#ffb6d0>봄이 오고 있어요...</color></size>\n\n" +
                               "겨울을 잘 견뎌 냈어요.\n남은 한 해는 정식판에서 기다리고 있어요.\n\n" +
                               "<size=28>데모를 플레이해 주셔서 고마워요!</size>\n<size=24>[Enter] 스팀 찜하기</size>"), 30f);
        }
        else if (demoMode && story.IsUnlocked("demo_end"))
        {
            HUD.Toast(Loc.T("Demo: spring is waiting in the full game. [Enter] while this shows: Wishlist", "데모: 봄은 정식판에서 기다리고 있어요. 이 알림이 보일 때 [Enter]: 찜하기"), 4f);
        }
        if (after != before) PixelArtWorld.RefreshSeasonal();

        OnNewDay?.Invoke();
        InventoryChanged();
        StoryEvents.OnNewDay(day);
        StoryEvents.CheckEnding();
    }

    // 하루 평가의 점수 줄 (일기가 대신 이야기함)
    static bool IsScoreLine(string l)
    {
        string[] keys = { "Slept on a warm", "뜨끈한 방바닥", "Passed out", "지쳐서", "Went to bed", "제때", "Stayed up", "늦게까지", "Ate proper", "챙겨 먹", "Drank fresh", "시원한 물",
                          "Didn't overwork", "무리하지", "Worked until", "기력이 바닥", "Went hungry", "배고프거나", "Harvested", "거뒀어요" };
        foreach (string k in keys) if (l.Contains(k)) return true;
        return false;
    }

    string NewCropsText(Season s)
    {
        var names = new List<string>();
        foreach (CropDef c in CropDB.All) if (c.minSeason == s) names.Add(c.harvestItem);
        return names.Count > 0 ? "<color=#9dff9d>" + Loc.T("Now growing: ", "이제 심을 수 있어요: ") + string.Join(", ", names.ConvertAll(n => ItemNames.Display(n))) + "</color>" : "";
    }

    // 내일 예고: 아침 요약 끝에 '오늘 할 만한 일'을 보여줘서 하루 더 하고 싶게 만듭니다.
    string BuildTodayPreview(Season season, int today)
    {
        var tips = new List<string>();
        // 장날 소식은 이장님 방송이 알려 줌 (VillageBroadcast)
        int ripe = farm.RipeNow();
        if (ripe > 0) tips.Add(Loc.T($"{ripe} crop(s) ripe in the field", $"밭에 다 익은 작물 {ripe}개"));
        int tomorrow = farm.RipeningTomorrow(season);
        if (tomorrow > 0) tips.Add(Loc.T($"{tomorrow} crop(s) ripen tomorrow if you water them today", $"오늘 물을 주면 내일 익을 작물 {tomorrow}개"));
        int toNext = SeasonSystem.HeartToNext(heart, season);
        if (toNext > 0 && toNext <= 8 && !(demoMode && season == Season.Winter && story.IsUnlocked("demo_end")))
            tips.Add(Loc.T($"{SeasonSystem.Name(season + 1)} feels close", $"{SeasonSystem.Name(season + 1)}이 가까이 와 있어요"));
        int todayN = timeManager != null ? timeManager.day : 0;
        string masil = Neighbors.MasilTonight(story, VillagerDB.All, todayN);
        if (masil != null) tips.Add(Loc.T($"{masil} would like a visit this evening (masil)", $"저녁에 {masil}에게 마실 가 볼까요"));
        if (StarHill.IsUnlocked && !raining && !snowing) tips.Add(Loc.T("Clear skies tonight: the star vine will grow", "오늘 밤 맑음: 별 덩굴이 자라요"));
        int left = AlbumDB.PerSeason - Album.Count(story, season);
        if (left > 0) tips.Add(Loc.T($"{left} {SeasonSystem.Name(season)} photo(s) left in your album [P]", $"앨범에 남은 {SeasonSystem.Name(season)} 사진 {left}장 [P]"));
        if (tips.Count == 0) return "";
        if (tips.Count > 3) tips.RemoveRange(3, tips.Count - 3);
        return "\n\n<size=24><color=#ffd84a>" + Loc.T("Today", "오늘") + "</color>  " + string.Join("  ·  ", tips) + "</size>";
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
            HUD.Announce(Loc.T("<size=42>You collapsed...</size>\n\nTake better care of yourself.\nWaking up from your last save.", "<size=42>쓰러졌어요...</size>\n\n몸을 좀 더 아껴 줘요.\n마지막 저장에서 다시 깨어나요."), 3f);
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
        HUD.Toast(Loc.T("Started a new game.", "새 게임을 시작했어요."));
        SceneManager.LoadScene("Farm");
    }

    void ReloadFromSave()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        if (LoadGame())
        {
            HUD.Toast(Loc.T("Loaded the last save.", "마지막 저장을 불러왔어요."));
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
        market.StartNewDay(rng, Season.Winter);
        today = new DayLog();
        story = new StoryState();
        story.Unlock(FirstDayGuide.OnKey);      // 첫날 안내는 새 게임에서만
        clearedResources.Clear();
        raining = false;
        snowing = false;
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
            snowing = snowing,
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
        snowing = data.snowing;

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
        Spirit.EnsureSpawned();
        PixelArtWorld.RefreshSeasonal();
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
