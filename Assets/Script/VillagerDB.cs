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

// 회색 주민 정의 (한국 시골 사람들). 소원을 하나 이뤄줄 때마다 색이 1/3씩 돌아옵니다.
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
    public string[] seasonLines = new string[4];   // 마음의 계절별 한마디 (겨울, 봄, 여름, 가을)
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

    // 한국어/영어: 대사는 Loc.T로 지금 언어를 골라 만들고, 언어가 바뀌면 다시 만듭니다.
    static string L(string en, string ko) => Loc.T(en, ko);

    static VillagerDB()
    {
        Build();
        Loc.Changed += () => { All.Clear(); Build(); };
    }

    static void Build()
    {
        // ---------------- Grandma Boksun: the rice mill and rice-cake shop (Market) ----------------
        All.Add(new VillagerDef
        {
            id = "rosa",
            name = L("Grandma Boksun", "복순 할머니"),
            scene = "Market",
            position = new Vector2(-6.0f, 1.0f),
            intro = L("Oh my, a new face! Forgive me, dear... Since my husband passed, the mill has gone quiet and everything faded. Even my rice cakes taste gray.",
                      "아이고, 처음 보는 얼굴이네. 미안해요... 영감 가고 나서 방앗간 소리가 멎더니, 세상이 다 바랬어. 떡 맛까지 잿빛이야."),
            grayIdle = new[] { L("The mill used to rumble from dawn. Now it's just me and the clock.", "새벽부터 방앗간이 쿵쿵 울렸는데. 요즘은 나랑 시계 소리뿐이야."),
                               L("I steam rice cakes out of habit, then don't eat them.", "버릇처럼 떡을 찌고는, 정작 먹지를 않아.") },
            colorIdle = new[] { L("Come by anytime! The rice cakes are still warm.", "아무 때나 와요! 떡이 아직 따끈해."),
                                L("You look thin. Did you eat? Eat, eat!", "얼굴이 반쪽이네. 밥은 먹었어? 먹어, 먹어!"),
                                L("He would have liked you, you know.", "우리 영감이 봤으면 참 좋아했을 텐데.") },
            restored = L("I can see the colors again... Thank you, dear. Truly.", "색이 다시 보여... 고마워요. 정말로."),
            seasonLines = new[]
            {
                L("Winter nights are long when you're alone. Come in, warm your hands.", "혼자 있으면 겨울밤이 길어. 들어와서 손 좀 녹여."),
                L("Spring! Time for mugwort rice cakes. Smell that?", "봄이네! 쑥떡 해 먹을 때야. 냄새 좋지?"),
                L("Too hot to light the steamer at noon. I work at dawn now.", "한낮엔 너무 더워서 시루를 못 올려. 새벽에 일해."),
                L("Autumn was his favorite. New rice, new rice cakes.", "가을을 제일 좋아했지. 햅쌀로 햇떡 찌는 맛에."),
            },
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Carrot", 3),
                    request = L("Would you bring me 3 Carrots? I haven't fried a pancake in so long.", "당근 세 개만 가져다줄래? 전 부쳐 본 지가 언젠지 몰라."),
                    thanks = L("Lovely carrots! Here, some rice cakes. And let me show you my carrot pancakes.", "당근 좋네! 자, 떡 좀 가져가. 그리고 내 당근전 부치는 법 알려 줄게."),
                    heart = 3f, rewardItems = Items("Rice Cake", 3),
                    unlock = "recipe_carrot_cake", unlockText = L("New recipe: Carrot Pancake (needs a Campfire)", "새 요리: 당근전 (모닥불 필요)"),
                },
                new WishDef
                {
                    items = Items("Corn", 2, "Apple", 2),
                    request = L("The children used to come for snacks after school. I'd need 2 Corn and 2 Apples.", "애들이 학교 끝나면 간식 먹으러 오곤 했어. 옥수수 두 개랑 사과 두 개만 있으면 돼."),
                    thanks = L("The whole market smells sweet again! Take this for your trouble.", "장터에 다시 단내가 나네! 수고했으니 이거 받아."),
                    heart = 3f, gold = 150,
                },
                new WishDef
                {
                    items = Items("Strawberry Jam", 1),
                    request = L("Strawberry jam... he made it every summer. Could you make some? I'd like to taste it once more.", "딸기잼... 영감이 여름마다 만들었는데. 한 병만 만들어 줄래? 한 번만 더 맛보고 싶어."),
                    thanks = L("...It tastes just like his. Look at me, crying like a girl. Here, take these.", "...영감이 만든 맛이랑 똑같네. 주책이지, 이 나이에 울고. 자, 이거 받아."),
                    heart = 8f, gold = 300, rewardItems = Items("Carrot Pancake", 2),
                },
            },
        });

        // ---------------- Mr. Dongsu: the hardware store with a forge out back (Market) ----------------
        All.Add(new VillagerDef
        {
            id = "theo",
            name = L("Mr. Dongsu", "동수 아저씨"),
            scene = "Market",
            position = new Vector2(-1.4f, 1.0f),
            intro = L("Hmph. The forge behind the store's been cold for a year. Nothing worth making anymore.", "흠. 가게 뒤 대장간 화덕이 식은 지 일 년이야. 만들 맛이 안 나."),
            grayIdle = new[] { L("Hmph.", "흠."), L("The hammer's heavier than it used to be.", "망치가 예전보다 무거워.") },
            colorIdle = new[] { L("Fire's roaring today. Feels good.", "오늘은 불이 잘 붙네. 좋다."),
                                L("Bring me good stone and I'll make you good tools.", "좋은 돌 가져오면 좋은 연장 만들어 주지."),
                                L("...Thanks. For before. Don't make it weird.", "...고맙다. 저번 일. 괜히 어색하게 굴지 말고.") },
            restored = L("Listen to that. The forge is singing again.", "들리나. 화덕이 다시 노래한다."),
            seasonLines = new[]
            {
                L("Cold hands, cold forge. Same every winter.", "손도 차고 화덕도 차고. 겨울마다 똑같아."),
                L("Snow's off the roof. Guess I'll fix the gutters.", "지붕 눈 다 녹았네. 홈통이나 고쳐야겠다."),
                L("Too hot at noon. I hammer at dawn now.", "한낮엔 못 해. 새벽에 두드린다."),
                L("Autumn's for sharp sickles. Fields need them before the frost.", "가을엔 낫을 벼려야지. 서리 오기 전에 다들 찾거든."),
            },
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Wood", 10),
                    request = L("If you want to be useful, bring 10 Wood. Maybe I'll light the forge.", "쓸모 있고 싶으면 나무 열 개 가져와. 화덕에 불이나 붙여 보게."),
                    thanks = L("...Fire's going. Strange feeling. Here, I'll show you how to make a copper watering can.", "...불이 붙었네. 기분이 묘하다. 자, 구리 물뿌리개 만드는 법 알려 주지."),
                    heart = 2f,
                    unlock = "recipe_copper_can", unlockText = L("New recipe: Copper Watering Can", "새 제작법: 구리 물뿌리개"),
                },
                new WishDef
                {
                    items = Items("Stone", 15),
                    request = L("The chimney's cracked. Need 15 Stone to rebuild it.", "굴뚝에 금이 갔어. 다시 쌓으려면 돌 열다섯 개가 필요해."),
                    thanks = L("Solid stone. I made this with the first good fire in a year.", "돌 좋네. 일 년 만에 제대로 된 불로 만든 거다."),
                    heart = 3f, gold = 200, rewardItems = Items("Fertile Hoe", 1),
                },
                new WishDef
                {
                    items = Items("Sunshine Shard", 1, "Earth's Breath", 1),
                    request = L("My daughter used to say the forge sings when nature's spirit touches it. Bring me a Sunshine Shard and an Earth's Breath.",
                                "딸아이가 그랬지. 자연의 기운이 닿으면 화덕이 노래한다고. 햇살 조각이랑 대지의 숨결 하나씩 가져와 봐."),
                    thanks = L("...She was right. You hear that? Here. Don't argue, just take it.", "...그 애 말이 맞았네. 들리지? 자, 말 말고 받아."),
                    heart = 8f, gold = 500,
                },
            },
        });

        // ---------------- Mina: the neighbour's kid (Farm, by the stream) ----------------
        All.Add(new VillagerDef
        {
            id = "mina",
            name = L("Mina", "미나"),
            scene = "Farm",
            position = new Vector2(5.0f, -2.4f),
            intro = L("...Hi. Grandma says not to talk to strangers. Everything here is boring. Everything's gray.", "...안녕. 할머니가 모르는 사람이랑 말하지 말랬어. 여긴 다 심심해. 다 회색이야."),
            grayIdle = new[] { L("The stream is boring. The fish are boring.", "개울도 심심하고 물고기도 심심해."),
                               L("Grandpa just sleeps all day now.", "할아버지는 요즘 하루 종일 주무셔.") },
            colorIdle = new[] { L("Did you catch anything big? Show me!", "큰 거 잡았어? 보여 줘!"),
                                L("Grandpa smiled today! A real smile!", "할아버지가 오늘 웃었어! 진짜로!"),
                                L("When I grow up I'm gonna have a farm like yours.", "나도 크면 이런 밭 가꿀 거야.") },
            restored = L("Everything looks so bright now! Is this what you see all the time?", "이제 다 반짝반짝해! 어른들은 맨날 이렇게 보여?"),
            seasonLines = new[]
            {
                L("The stream froze! ...It's still boring though.", "개울이 얼었어! ...그래도 심심해."),
                L("Look, the frogs are back! Spring is the best!", "봐, 개구리 돌아왔다! 봄이 최고야!"),
                L("Summer means fishing every day! Grandpa says so!", "여름엔 맨날 낚시야! 할아버지가 그랬어!"),
                L("The leaves are crunchy! Listen! *crunch crunch*", "낙엽이 바삭바삭해! 들어 봐! 바삭바삭"),
            },
            wishes = new[]
            {
                new WishDef
                {
                    items = Items("Apple", 2),
                    request = L("If you give me 2 Apples, I'll give you Grandpa's old fishing rod. He doesn't use it anymore.", "사과 두 개 주면 할아버지 옛날 낚싯대 줄게. 이제 안 쓰셔."),
                    thanks = L("Deal! Here! You can fish in the stream. Stand close and press E!", "약속! 여기! 개울에서 낚시할 수 있어. 가까이 서서 E 눌러!"),
                    heart = 2f, rewardItems = Items("Fishing Rod", 1),
                    unlock = "fishing", unlockText = L("Fishing unlocked! New recipe: Grilled Fish", "낚시를 할 수 있어요! 새 요리: 생선구이"),
                },
                new WishDef
                {
                    items = Items("Carp", 1),
                    request = L("Can you catch a Carp? A big orange one. I want to show Grandpa!", "잉어 잡을 수 있어? 크고 주황색인 거. 할아버지한테 보여 줄래!"),
                    thanks = L("Woah, it's huge! Grandpa's gonna flip! Here, I found these seeds in his shed.", "우와, 엄청 커! 할아버지 깜짝 놀라겠다! 자, 창고에서 찾은 씨앗이야."),
                    heart = 3f, rewardItems = Items("Strawberry Seed", 3),
                },
                new WishDef
                {
                    items = Items("Sunshine Shard", 1, "Strawberry", 2),
                    request = L("Grandma says sunflowers follow the sun. Bring me a Sunshine Shard and 2 Strawberries and I'll show you something!", "할머니가 해바라기는 해를 따라간대. 햇살 조각 하나랑 딸기 두 개 가져오면 뭐 보여 줄게!"),
                    thanks = L("Look! Sunflower seeds! Grandpa says they're for you. And the market will sell them now too!", "봐! 해바라기 씨앗! 할아버지가 가져가래. 이제 장터에서도 판대!"),
                    heart = 8f, rewardItems = Items("Sunflower Seed", 5),
                    unlock = "shop_sunflower", unlockText = L("Sunflower Seeds are now sold at the market", "이제 장터에서 해바라기 씨앗을 팔아요"),
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
        foreach (var s in stacks) parts.Add(Loc.Ko ? $"{ItemNames.Display(s.itemName)} {s.count}개" : $"{s.count} {s.itemName}");
        return string.Join(", ", parts);
    }
}
