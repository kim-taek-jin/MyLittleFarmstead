using System.Collections.Generic;
using UnityEngine;

// 아이템 분류
public enum ItemCategory { Tool, Seed, Crop, Food, Resource, Special, Station, Fish }

// 도구 종류
public enum ToolType { None, Hoe, WateringCan, Scythe, FishingRod, Mattock }

// 아이템 하나의 정의(이름, 가격, 효과 등)를 한 곳에서 관리합니다.
public class ItemDef
{
    public string name;
    public ItemCategory category;
    public int sellPrice;              // 0이면 판매 불가
    public ToolType toolType = ToolType.None;
    public int toolTier = 1;           // 1 = 한 칸, 2 = 세 칸
    public int waterCapacity;          // 물뿌리개 용량
    public string cropId;              // 씨앗일 때 심어지는 작물 ID
    public float hunger;               // 먹었을 때 배고픔 회복량
    public float thirst;               // 먹었을 때 갈증 회복량
    public float heart;                // 먹었을 때 마음(행복) 회복량
    public string iconFallback;        // 전용 스프라이트가 없을 때 빌려 쓸 아이템 스프라이트
    public Color color = Color.white;  // 스프라이트가 아예 없을 때 쓰는 임시 아이콘 색
    public string description = "";

    public bool IsEdible => category != ItemCategory.Tool && category != ItemCategory.Seed && (hunger > 0f || thirst > 0f || heart > 0f);
}

// 게임에 존재하는 모든 아이템의 데이터베이스.
// 새 아이템은 아래 Register 목록에 한 줄 추가하면 핫바/상점/판매가에 자동 반영됩니다.
public static class ItemDB
{
    private static readonly Dictionary<string, ItemDef> items = new Dictionary<string, ItemDef>();
    private static readonly List<string> order = new List<string>();

    // 이름 변경/오타 호환 (예: 예전 씬에 저장된 이름)
    private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>
    {
        { "Potaoto Seed", "Potato Seed" },
        // 방향 C: 한국 시골 음식으로 바뀐 이름 (예전 저장 파일, 씬의 상점 목록)
        { "Bread", "Rice Cake" },
        { "Corn Soup", "Steamed Corn" },
        { "Carrot Cake", "Carrot Pancake" },
        { "Pumpkin Pie", "Pumpkin Porridge" },
    };

    static ItemDB()
    {
        // --- 도구 ---
        Register(new ItemDef { name = "Hoe", category = ItemCategory.Tool, toolType = ToolType.Hoe, color = new Color(0.6f, 0.45f, 0.3f), description = "Till dirt into farmland." });
        Register(new ItemDef { name = "Fertile Hoe", category = ItemCategory.Tool, toolType = ToolType.Hoe, toolTier = 2, iconFallback = "Hoe", color = new Color(0.4f, 0.75f, 0.35f), description = "Tills 3 tiles at once." });
        Register(new ItemDef { name = "Watering Can", category = ItemCategory.Tool, toolType = ToolType.WateringCan, waterCapacity = 10, color = new Color(0.4f, 0.6f, 0.9f), description = "Waters crops. Refill at the well." });
        Register(new ItemDef { name = "Copper Watering Can", category = ItemCategory.Tool, toolType = ToolType.WateringCan, toolTier = 2, waterCapacity = 25, iconFallback = "Watering Can", color = new Color(0.85f, 0.5f, 0.25f), description = "Waters 3 tiles at once." });
        Register(new ItemDef { name = "Scythe", category = ItemCategory.Tool, toolType = ToolType.Scythe, color = new Color(0.75f, 0.75f, 0.8f), description = "Harvests ripe crops around the target." });

        // --- 씨앗 ---
        Register(new ItemDef { name = "Carrot Seed", category = ItemCategory.Seed, sellPrice = 5, cropId = "carrot", color = new Color(0.95f, 0.6f, 0.2f) });
        Register(new ItemDef { name = "Potato Seed", category = ItemCategory.Seed, sellPrice = 15, cropId = "potato", iconFallback = "Carrot Seed", color = new Color(0.75f, 0.6f, 0.4f) });
        Register(new ItemDef { name = "Corn Seed", category = ItemCategory.Seed, sellPrice = 12, cropId = "corn", iconFallback = "Carrot Seed", color = new Color(0.95f, 0.85f, 0.3f) });
        Register(new ItemDef { name = "Strawberry Seed", category = ItemCategory.Seed, sellPrice = 20, cropId = "strawberry", iconFallback = "Carrot Seed", color = new Color(0.9f, 0.25f, 0.35f) });

        // --- 작물 (날로 먹을 수도 있음) ---
        Register(new ItemDef { name = "Carrot", category = ItemCategory.Crop, sellPrice = 25, hunger = 10f, color = new Color(0.95f, 0.55f, 0.15f) });
        Register(new ItemDef { name = "Potato", category = ItemCategory.Crop, sellPrice = 80, hunger = 8f, color = new Color(0.75f, 0.6f, 0.4f) });
        Register(new ItemDef { name = "Corn", category = ItemCategory.Crop, sellPrice = 50, hunger = 12f, color = new Color(0.95f, 0.85f, 0.3f) });
        Register(new ItemDef { name = "Strawberry", category = ItemCategory.Crop, sellPrice = 120, hunger = 6f, thirst = 4f, heart = 1f, color = new Color(0.9f, 0.2f, 0.3f) });

        // --- 음식 ---
        Register(new ItemDef { name = "Apple", category = ItemCategory.Food, sellPrice = 8, hunger = 20f, thirst = 5f, color = new Color(0.85f, 0.15f, 0.15f) });
        Register(new ItemDef { name = "Rice Cake", category = ItemCategory.Food, sellPrice = 10, hunger = 40f, color = new Color(0.95f, 0.92f, 0.85f), description = "Chewy rice cake from the mill. Fills you up." });
        Register(new ItemDef { name = "Roasted Carrot", category = ItemCategory.Food, sellPrice = 60, hunger = 35f, heart = 2f, iconFallback = "Carrot", color = new Color(0.8f, 0.4f, 0.1f) });
        Register(new ItemDef { name = "Baked Potato", category = ItemCategory.Food, sellPrice = 110, hunger = 50f, heart = 2f, iconFallback = "Potato", color = new Color(0.65f, 0.45f, 0.25f) });
        Register(new ItemDef { name = "Steamed Corn", category = ItemCategory.Food, sellPrice = 130, hunger = 30f, thirst = 35f, heart = 3f, iconFallback = "Corn", color = new Color(0.95f, 0.8f, 0.4f), description = "Corn steamed in its husk, sweet and salty. A taste of summer." });
        Register(new ItemDef { name = "Strawberry Jam", category = ItemCategory.Food, sellPrice = 420, hunger = 20f, heart = 6f, iconFallback = "Strawberry", color = new Color(0.7f, 0.1f, 0.25f) });

        // --- 자원 ---
        Register(new ItemDef { name = "Wood", category = ItemCategory.Resource, sellPrice = 2, color = new Color(0.55f, 0.35f, 0.2f) });
        Register(new ItemDef { name = "Stone", category = ItemCategory.Resource, sellPrice = 5, color = new Color(0.55f, 0.55f, 0.6f) });

        // --- 자연의 정수 (특수) ---
        Register(new ItemDef { name = "Sunshine Shard", category = ItemCategory.Special, sellPrice = 100, color = new Color(1f, 0.9f, 0.3f), description = "Use on a crop: it ripens golden and brings extra joy." });
        Register(new ItemDef { name = "Earth's Breath", category = ItemCategory.Special, sellPrice = 100, color = new Color(0.4f, 0.8f, 0.5f), description = "Use on a crop: it grows by one day instantly." });

        // --- 낚시 (미나의 소원으로 낚싯대를 얻음) ---
        Register(new ItemDef { name = "Fishing Rod", category = ItemCategory.Tool, toolType = ToolType.FishingRod, color = new Color(0.7f, 0.55f, 0.35f), description = "Press E by the stream or the lake to fish." });
        Register(new ItemDef { name = "Minnow", category = ItemCategory.Fish, sellPrice = 15, color = new Color(0.6f, 0.65f, 0.75f) });
        Register(new ItemDef { name = "Carp", category = ItemCategory.Fish, sellPrice = 45, color = new Color(0.8f, 0.45f, 0.25f) });
        Register(new ItemDef { name = "Golden Koi", category = ItemCategory.Fish, sellPrice = 300, color = new Color(1f, 0.85f, 0.3f) });
        Register(new ItemDef { name = "Old Boot", category = ItemCategory.Resource, sellPrice = 1, color = new Color(0.45f, 0.3f, 0.2f) });
        Register(new ItemDef { name = "Grilled Fish", category = ItemCategory.Food, sellPrice = 90, hunger = 40f, heart = 2f, color = new Color(0.7f, 0.4f, 0.2f) });

        // --- 주민 보상 / 새 작물 ---
        Register(new ItemDef { name = "Carrot Pancake", category = ItemCategory.Food, sellPrice = 250, hunger = 30f, heart = 6f, color = new Color(0.9f, 0.6f, 0.3f), description = "Grandma Boksun's recipe. Crisp at the edges, warm in the middle." });
        Register(new ItemDef { name = "Sunflower Seed", category = ItemCategory.Seed, sellPrice = 15, cropId = "sunflower", color = new Color(1f, 0.85f, 0.2f) });
        Register(new ItemDef { name = "Sunflower", category = ItemCategory.Crop, sellPrice = 90, color = new Color(1f, 0.85f, 0.2f) });

        // --- 가을(결실의 계절) ---
        Register(new ItemDef { name = "Pumpkin Seed", category = ItemCategory.Seed, sellPrice = 20, cropId = "pumpkin", color = new Color(1f, 0.6f, 0.2f) });
        Register(new ItemDef { name = "Pumpkin", category = ItemCategory.Crop, sellPrice = 160, hunger = 10f, color = new Color(1f, 0.55f, 0.15f) });
        Register(new ItemDef { name = "Pumpkin Porridge", category = ItemCategory.Food, sellPrice = 320, hunger = 45f, heart = 4f, color = new Color(0.9f, 0.55f, 0.25f), description = "Golden pumpkin porridge with little rice cake balls. Tastes like a good year." });

        // --- 별의 언덕 ---
        // ---- 장 담그기 (K-2) ----
        Register(new ItemDef { name = "Soybeans", category = ItemCategory.Resource, sellPrice = 5, color = new Color(0.9f, 0.8f, 0.5f), description = "Dried soybeans for making meju. Boil them at the agungi." });
        Register(new ItemDef { name = "Salt", category = ItemCategory.Resource, sellPrice = 2, color = new Color(0.95f, 0.95f, 0.95f), description = "Coarse sea salt for sauce jars." });
        Register(new ItemDef { name = "Fresh Meju", category = ItemCategory.Special, sellPrice = 0, color = new Color(0.8f, 0.65f, 0.4f), description = "A block of mashed soybeans. Hang it on the meju rack by the house to dry." });
        Register(new ItemDef { name = "Dried Meju", category = ItemCategory.Special, sellPrice = 40, color = new Color(0.6f, 0.45f, 0.25f), description = "Dried hard and fragrant. Put it in a sauce jar with salt." });
        Register(new ItemDef { name = "Doenjang", category = ItemCategory.Resource, sellPrice = 60, color = new Color(0.55f, 0.35f, 0.15f), description = "Soybean paste from your own jar. The base of a good stew." });
        Register(new ItemDef { name = "Ganjang", category = ItemCategory.Resource, sellPrice = 50, color = new Color(0.2f, 0.1f, 0.05f), description = "Soy sauce from your own jar." });
        Register(new ItemDef { name = "Doenjang Stew", category = ItemCategory.Food, sellPrice = 180, hunger = 45f, thirst = 10f, heart = 5f, color = new Color(0.6f, 0.4f, 0.2f), description = "Bubbling doenjang stew with potato and carrot. Tastes like home." });
        Register(new ItemDef { name = "Braised Potatoes", category = ItemCategory.Food, sellPrice = 160, hunger = 35f, heart = 3f, color = new Color(0.7f, 0.5f, 0.25f), description = "Little potatoes simmered in your own soy sauce." });
        // K-4: 동지 팥죽 (할머니에게 받아 이웃에게 나름)
        Register(new ItemDef { name = "Red Bean Porridge", category = ItemCategory.Food, sellPrice = 0, hunger = 40f, heart = 3f, color = new Color(0.55f, 0.15f, 0.15f), description = "Winter solstice porridge with little rice balls. One bowl for every house." });
        // W-1: 마실 것
        Register(new ItemDef { name = "Barley Tea", category = ItemCategory.Food, sellPrice = 10, thirst = 50f, color = new Color(0.6f, 0.38f, 0.16f), description = "Cold bottled barley tea from the corner store. Take it anywhere." });
        Register(new ItemDef { name = "Homemade Barley Tea", category = ItemCategory.Food, sellPrice = 10, thirst = 60f, heart = 1f, color = new Color(0.62f, 0.36f, 0.14f), description = "Barley tea boiled at your own agungi, kept in an old glass bottle. Tastes like home." });
        Register(new ItemDef { name = "Roasted Barley", category = ItemCategory.Resource, sellPrice = 4, color = new Color(0.5f, 0.3f, 0.12f), description = "Roasted barley grains. Boil them at the agungi for barley tea." });
        // F-1: 봄
        Register(new ItemDef { name = "Azalea", category = ItemCategory.Resource, sellPrice = 8, color = new Color(0.9f, 0.4f, 0.65f), description = "Azalea blossoms from the back hill. For flower pancakes." });
        Register(new ItemDef { name = "Rice Flour", category = ItemCategory.Resource, sellPrice = 6, color = new Color(0.96f, 0.94f, 0.9f), description = "Glutinous rice flour from the corner store." });
        Register(new ItemDef { name = "Azalea Hwajeon", category = ItemCategory.Food, sellPrice = 70, hunger = 15f, heart = 2f, color = new Color(0.95f, 0.85f, 0.88f), description = "Little rice pancakes with an azalea pressed into each. Spring on a plate." });
        Register(new ItemDef { name = "Pepper Seedling", category = ItemCategory.Seed, sellPrice = 10, cropId = "pepper", color = new Color(0.3f, 0.6f, 0.3f), description = "A chili pepper seedling. Plant it in spring; it keeps fruiting into autumn." });
        Register(new ItemDef { name = "Red Pepper", category = ItemCategory.Crop, sellPrice = 35, hunger = 2f, color = new Color(0.85f, 0.12f, 0.1f), description = "Ripe red chili. Dried in autumn, it becomes gochugaru." });
        Register(new ItemDef { name = "Compost", category = ItemCategory.Special, sellPrice = 0, color = new Color(0.4f, 0.3f, 0.2f), description = "Rich compost from the heap. Spread it on a gray, hardened plot to bring it back to life." });
        Register(new ItemDef { name = "Star Fragment", category = ItemCategory.Special, sellPrice = 50, color = new Color(1f, 0.9f, 0.4f), description = "A piece of a falling star. The dokkaebi fire loves it (all four colors)." });

        // --- 설치/작업대 ---
        // ---- 숲 (맵 3단계) ----
        Register(new ItemDef { name = "Mattock", category = ItemCategory.Tool, toolType = ToolType.Mattock, color = new Color(0.6f, 0.6f, 0.65f), description = "Digs out stumps, boulders and fallen logs." });
        Register(new ItemDef { name = "Acorn", category = ItemCategory.Seed, color = new Color(0.7f, 0.5f, 0.3f), description = "Plant it in a forest clearing and water it for a few days." });
        Register(new ItemDef { name = "Wild Garlic", category = ItemCategory.Food, sellPrice = 20, hunger = 6f, color = new Color(0.9f, 0.95f, 0.85f) });
        Register(new ItemDef { name = "Fern Sprout", category = ItemCategory.Food, sellPrice = 25, hunger = 8f, color = new Color(0.45f, 0.65f, 0.3f) });
        Register(new ItemDef { name = "Morel", category = ItemCategory.Food, sellPrice = 60, hunger = 10f, color = new Color(0.7f, 0.55f, 0.35f) });
        Register(new ItemDef { name = "Spring Greens", category = ItemCategory.Food, sellPrice = 90, hunger = 25f, heart = 2f, color = new Color(0.45f, 0.75f, 0.35f), description = "Fresh from the forest floor." });
        Register(new ItemDef { name = "Morel Soup", category = ItemCategory.Food, sellPrice = 220, hunger = 35f, thirst = 20f, heart = 4f, color = new Color(0.8f, 0.6f, 0.35f), description = "Earthy and warm. Tastes like a walk in the woods." });

        // ---- 호수 (맵 4-1단계) ----
        Register(new ItemDef { name = "Bluegill", category = ItemCategory.Fish, sellPrice = 35, color = new Color(0.35f, 0.5f, 0.85f) });
        Register(new ItemDef { name = "Moonfin", category = ItemCategory.Fish, sellPrice = 110, color = new Color(0.55f, 0.55f, 0.85f), description = "Only bites at night." });
        Register(new ItemDef { name = "Glassfish", category = ItemCategory.Fish, sellPrice = 240, color = new Color(0.85f, 0.95f, 1f), description = "You can see right through it. It only lives in clear water." });
        Register(new ItemDef { name = "Wild Raspberry", category = ItemCategory.Food, sellPrice = 30, hunger = 5f, thirst = 3f, heart = 1f, color = new Color(0.85f, 0.15f, 0.3f) });
        Register(new ItemDef { name = "Watercress", category = ItemCategory.Food, sellPrice = 25, hunger = 6f, thirst = 4f, color = new Color(0.4f, 0.7f, 0.35f) });

        // ---- 노을 언덕 (맵 4-2단계) ----
        Register(new ItemDef { name = "Pear", category = ItemCategory.Food, sellPrice = 70, hunger = 12f, thirst = 8f, color = new Color(0.85f, 0.85f, 0.4f) });
        Register(new ItemDef { name = "Persimmon", category = ItemCategory.Food, sellPrice = 60, hunger = 12f, color = new Color(1f, 0.55f, 0.15f), description = "Sweet and soft. Hang it on the drying rack on Sunset Hill to make dried persimmons." });
        Register(new ItemDef { name = "Dried Persimmon", category = ItemCategory.Food, sellPrice = 260, hunger = 18f, heart = 3f, color = new Color(0.75f, 0.35f, 0.15f), description = "Chewy and sweet, dried slowly in the autumn wind." });

        Register(new ItemDef { name = "Campfire", category = ItemCategory.Station, sellPrice = 20, iconFallback = "Wood", color = new Color(1f, 0.45f, 0.1f), description = "Needed for cooking recipes." });
    }

    private static void Register(ItemDef def)
    {
        items[def.name] = def;
        order.Add(def.name);
    }

    public static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return aliases.TryGetValue(name, out string real) ? real : name;
    }

    // 등록되지 않은 아이템도 안전하게 다룰 수 있도록 기본 정의를 만들어 돌려줍니다.
    public static ItemDef Get(string name)
    {
        name = Normalize(name);
        if (string.IsNullOrEmpty(name)) return null;
        if (items.TryGetValue(name, out ItemDef def)) return def;

        def = new ItemDef { name = name, category = ItemCategory.Resource, sellPrice = 1, color = Color.gray };
        items[name] = def;
        order.Add(name);
        return def;
    }

    public static bool Exists(string name) => !string.IsNullOrEmpty(name) && items.ContainsKey(Normalize(name));

    public static bool IsTool(string name) => Get(name)?.category == ItemCategory.Tool;
    public static bool IsSeed(string name) => Get(name)?.category == ItemCategory.Seed;
    public static bool IsEdible(string name) => Get(name)?.IsEdible ?? false;
    public static ToolType GetToolType(string name) => Get(name)?.toolType ?? ToolType.None;

    public static bool CanSell(string name)
    {
        ItemDef def = Get(name);
        return def != null && def.category != ItemCategory.Tool && def.sellPrice > 0;
    }

    // 핫바에 표시할 아이템인지 (도구, 씨앗, 먹을 것, 정수)
    public static bool ShowInHotbar(string name)
    {
        ItemDef def = Get(name);
        if (def == null) return false;
        switch (def.category)
        {
            case ItemCategory.Tool:
            case ItemCategory.Seed:
            case ItemCategory.Food:
            case ItemCategory.Crop:
            case ItemCategory.Special:
                return true;
            default:
                return false;
        }
    }

    // 핫바 정렬 순서: 도구 → 씨앗 → 음식 → 작물 → 정수, 같은 분류 안에서는 등록 순서
    public static int SortKey(string name)
    {
        ItemDef def = Get(name);
        int group;
        switch (def.category)
        {
            case ItemCategory.Tool: group = 0; break;
            case ItemCategory.Seed: group = 1; break;
            case ItemCategory.Food: group = 2; break;
            case ItemCategory.Crop: group = 3; break;
            case ItemCategory.Special: group = 4; break;
            default: group = 5; break;
        }
        return group * 1000 + order.IndexOf(def.name);
    }
}
