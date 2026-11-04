using System.Collections.Generic;
using UnityEngine;

// 소원 하나: 가져다줄 아이템과 보상
public class WishDef
{
    public ItemStack[] items;
    public string request;     // 부탁할 때 대사
    public string thanks;      // 이뤄줬을 때 대사
    public int gold;
    public float heart;
    public ItemStack[] rewardItems = new ItemStack[0];
    public string unlock;      // 해금 키 (레시피/상점/기능)
    public string unlockText;  // 해금 안내 문구
}

// 회색 주민 정의. 소원을 하나 이뤄줄 때마다 색이 1/3씩 돌아옵니다.
public class VillagerDef
{
    public string id;
    public string name;
    public string scene;
    public Vector2 position;
    public string intro;
    public string[] grayIdle;
    public string[] colorIdle;
    public string restored;    // 색을 모두 되찾았을 때 대사
    public WishDef[] wishes;
}

public static class VillagerDB
{
    public static readonly List<VillagerDef> All = new List<VillagerDef>();

    // 해금 키 → 시장에서 새로 파는 아이템
    public static readonly Dictionary<string, ItemForSale> ShopUnlocks = new Dictionary<string, ItemForSale>
    {
        { "shop_sunflower", new ItemForSale { itemName = "Sunflower Seed", price = 60 } },
    };

    static ItemStack[] Items(params object[] pairs)
    {
        var list = new ItemStack[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new ItemStack((string)pairs[i * 2], (int)pairs[i * 2 + 1]);
        return list;
    }

    static VillagerDB()
    {
        // ---------------- Rosa: the baker (Market, in front of the bakery) ----------------
        All.Add(new VillagerDef
        {
            id = "rosa",
            name = "Grandma Rosa",
            scene = "Market",
            position = new Vector2(-6.0f, 1.0f),
            intro = "Oh, a new face! Forgive me, dear... Since my husband passed, the colors just faded away. Even my bread tastes gray.",
            grayIdle = new[] { "The oven feels so cold these days.", "I used to bake every morning. Now I just... sit." },
            colorIdle = new[] { "Come by anytime, dear! The bread's still warm.", "You look tired. Did you eat today? Eat, eat!", "He'd have liked you, you know." },
            restored = "I can see the colors again... Thank you, dear. Truly.",
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Carrot", 3),
                    request = "Would you bring me 3 Carrots? I'd like to bake something sweet again.",
                    thanks = "These are lovely! Here, some bread for you. And let me teach you my Carrot Cake.",
                    heart = 3f, rewardItems = Items("Bread", 3),
                    unlock = "recipe_carrot_cake", unlockText = "New recipe: Carrot Cake (needs a Campfire)",
                },
                new WishDef
                {
                    items = Items("Corn", 2, "Apple", 2),
                    request = "The children loved my corn muffins. I'd need 2 Corn and 2 Apples.",
                    thanks = "Mmm, the whole market smells like muffins again! Take this for your trouble.",
                    heart = 3f, gold = 150,
                },
                new WishDef
                {
                    items = Items("Strawberry Jam", 1),
                    request = "Strawberry jam... he made it every summer. Could you make some? I'd like to taste it once more.",
                    thanks = "...It tastes just like his. Oh, look at me, crying like a girl. Here, take these cakes.",
                    heart = 8f, gold = 300, rewardItems = Items("Carrot Cake", 2),
                },
            },
        });

        // ---------------- Theo: the blacksmith (Market, in front of the tool shop) ----------------
        All.Add(new VillagerDef
        {
            id = "theo",
            name = "Theo the Smith",
            scene = "Market",
            position = new Vector2(-1.4f, 1.0f),
            intro = "Hmph. The forge has been cold for a year. Nothing worth making anymore.",
            grayIdle = new[] { "Hmph.", "Hammer's heavier than it used to be." },
            colorIdle = new[] { "Fire's roaring today. Feels good.", "Bring me good stone and I'll make you good tools.", "...Thanks. For before. Don't make it weird." },
            restored = "Listen to that. The forge is singing again.",
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Wood", 10),
                    request = "If you want to be useful, bring 10 Wood. Maybe I'll light the forge.",
                    thanks = "...Fire's going. Strange feeling. Here, I'll show you how to make a copper watering can.",
                    heart = 2f,
                    unlock = "recipe_copper_can", unlockText = "New recipe: Copper Watering Can",
                },
                new WishDef
                {
                    items = Items("Stone", 15),
                    request = "Chimney's cracked. Need 15 Stone to rebuild it.",
                    thanks = "Solid stone. Here, I made this with the first good fire in a year.",
                    heart = 3f, gold = 200, rewardItems = Items("Fertile Hoe", 1),
                },
                new WishDef
                {
                    items = Items("Sunshine Shard", 1, "Earth's Breath", 1),
                    request = "My daughter used to say the forge sings when nature's spirit touches it. Bring me a Sunshine Shard and an Earth's Breath.",
                    thanks = "...She was right. You hear that? Here. Don't argue, just take it.",
                    heart = 8f, gold = 500,
                },
            },
        });

        // ---------------- Mina: the child (Farm, by the pond) ----------------
        All.Add(new VillagerDef
        {
            id = "mina",
            name = "Mina",
            scene = "Farm",
            position = new Vector2(5.0f, -2.4f),
            intro = "...Hi. I'm not supposed to talk to strangers. Everything here is boring. Everything's gray.",
            grayIdle = new[] { "The pond is boring. The fish are boring.", "Grandpa just sleeps all day now." },
            colorIdle = new[] { "Did you catch anything big? Show me!", "Grandpa smiled today! A real smile!", "When I grow up I'm gonna have a farm like yours." },
            restored = "Everything looks so bright now! Is this what you see all the time?",
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Apple", 2),
                    request = "If you give me 2 Apples, I'll give you Grandpa's old fishing rod. He doesn't use it anymore.",
                    thanks = "Deal! Here! You can fish in this pond. Stand close and press E!",
                    heart = 2f, rewardItems = Items("Fishing Rod", 1),
                    unlock = "fishing", unlockText = "Fishing unlocked! New recipe: Grilled Fish",
                },
                new WishDef
                {
                    items = Items("Carp", 1),
                    request = "Can you catch a Carp? A big orange one. I want to show Grandpa!",
                    thanks = "Woah, it's huge! Grandpa's gonna flip! Here, I found these seeds in his shed.",
                    heart = 3f, rewardItems = Items("Strawberry Seed", 3),
                },
                new WishDef
                {
                    items = Items("Sunshine Shard", 1, "Strawberry", 2),
                    request = "Grandma said sunflowers follow the sun. Bring me a Sunshine Shard and 2 Strawberries and I'll show you something!",
                    thanks = "Look! Sunflower seeds! Grandpa says they're for you. And the market will sell them now too!",
                    heart = 8f, rewardItems = Items("Sunflower Seed", 5),
                    unlock = "shop_sunflower", unlockText = "Sunflower Seeds are now sold at the market",
                },
            },
        });
    }

    public static VillagerDef Get(string id)
    {
        foreach (var v in All) if (v.id == id) return v;
        return null;
    }

    public static string ItemsText(ItemStack[] stacks)
    {
        var parts = new List<string>();
        foreach (var s in stacks) parts.Add($"{s.count} {s.itemName}");
        return string.Join(", ", parts);
    }
}
