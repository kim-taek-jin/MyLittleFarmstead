using System.Collections.Generic;
using UnityEngine;

// K-3: 이야기 한 장(章). 심부름 대신 "같이 하는 일" 하나 (품앗이, 새참, 마실, 그리고 그 사람만의 일)
public enum ChapterKind { Pumasi, Saecham, Masil, Fish, Letter }

public class ChapterDef
{
    public ChapterKind kind;
    public string invite;                 // 이 장이 열려 있을 때 낮에 말을 걸면 하는 말
    public string[] scene;                // 장면 대사 (품앗이를 마쳤을 때, 새참을 먹을 때, 마실에서)
    public string hint;                   // 생활 일지·오늘 미리보기: 다음에 할 일
    public string spot;                   // 품앗이 자리: "steamer", "forge", "sled", "briquettes"
    public string[] steps;                // 품앗이: [E] 한 번마다 하는 일 (보통 셋)
    public string needItem;               // 새참: 이 음식이어야 함 (null이면 아무 음식)
    public int gold;
    public ItemStack[] rewardItems = new ItemStack[0];
    public string unlock;                 // 해금 키 (레시피, 가게, 낚시)
    public string unlockText;
    public string favor;                  // 품앗이로 돌려받는 것 (해금 키: favor_jang, favor_hoe, favor_water, favor_hands)
    public string favorText;
    public float heart;
}

// 회색 주민 정의 (한국 시골 사람들). 이야기 한 장을 넘길 때마다 색이 1/3씩 돌아옵니다.
public class VillagerDef
{
    public string id;
    public string name;
    public string scene;
    public Vector2 position;
    public Vector2 eveningPosition;       // 저녁(18~22시)에 앉아 있는 자리 (마실 가는 곳)
    public string eveningPlace;           // "방앗간 평상" 같은 이름 (안내용)
    public string intro;
    public string[] grayIdle;
    public string[] colorIdle;
    public string restored;    // 색을 모두 되찾았을 때 대사
    public string[] seasonLines = new string[4];   // 마음의 계절별 한마디 (겨울, 봄, 여름, 가을)
    public string[] masilLines;           // 이야기를 다 넘긴 뒤에도 저녁 마실에서 하는 말
    public ChapterDef[] chapters;
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
            eveningPosition = new Vector2(-6.7f, 0.75f),
            eveningPlace = L("the bench in front of the mill", "방앗간 앞 평상"),
            masilLines = new[]
            {
                L("Sit, sit. The night's long anyway. ...He used to sit right where you are.", "앉아, 앉아. 어차피 긴 밤인데. ...영감이 딱 자네 자리에 앉았었어."),
                L("Look at the stars. In the city you can't see them, can you?", "별 좀 봐. 도시에선 안 보이지?"),
                L("Did you eat dinner? Don't lie to me.", "저녁은 먹었어? 거짓말하지 말고."),
            },
            chapters = new[]
            {
                new ChapterDef
                {
                    kind = ChapterKind.Pumasi, spot = "steamer",
                    invite = L("Oh, perfect timing. I'm about to set rice cakes in the steamer, but these old arms aren't what they were. Would you lend me a hand?",
                               "아이고 마침 잘 왔네. 시루에 떡 안칠 건데, 이 늙은이 팔이 영 시원찮아. 좀 거들어 줄래?"),
                    hint = L("By day, help steam rice cakes at the mill (the steamer by the door)", "낮에 방앗간 문 옆 떡 시루 거들기"),
                    steps = new[] { L("You stoke the fire under the pot.", "솥 밑에 불을 지펴요."), L("You spread the rice flour in the steamer, layer by layer.", "시루에 쌀가루를 켜켜이 안쳐요."), L("Steam rises. You lift the lid together.", "김이 오르고, 둘이 같이 뚜껑을 열어요.") },
                    scene = new[]
                    {
                        L("Look at you, quick hands. Strange... someone doing what he used to do.", "손이 야무지네. 이상하다... 영감이 하던 걸 누가 해 주니."),
                        L("Here, take some rice cakes. And I'll show you how I fry carrot pancakes.", "자, 떡 가져가. 그리고 내 당근전 부치는 법도 알려 줄게."),
                        L("Pumasi gets paid back in work, you know. When your sauce jars need looking after, I'll come by.", "품앗이는 품으로 갚는 거여. 장독 돌볼 때 내가 가서 봐 줄게."),
                    },
                    rewardItems = Items("Rice Cake", 3), heart = 3f,
                    unlock = "recipe_carrot_cake", unlockText = L("New recipe: Carrot Pancake", "새 요리: 당근전"),
                    favor = "favor_jang", favorText = L("Grandma Boksun will help look after your sauce jars (deeper taste)", "복순 할머니가 장독을 같이 돌봐 줘요 (장맛이 더 깊어짐)"),
                },
                new ChapterDef
                {
                    kind = ChapterKind.Saecham, needItem = "Doenjang Stew",
                    invite = L("These days I can't bring myself to eat alone. If someone brought me a pot of doenjang stew, maybe...",
                               "요즘은 혼자 먹으려니 숟가락이 안 가. 누가 끓여 준 된장찌개라도 있으면 모를까..."),
                    hint = L("Bring her a doenjang stew for a work-break meal (saecham)", "된장찌개를 끓여 새참으로 가져가기"),
                    scene = new[]
                    {
                        L("You made this? With sauce you put down yourself? ...(She takes a spoonful and says nothing for a long while.)",
                          "이걸 자네가? 자네가 담근 장으로? ...(한 숟갈 뜨고는 한참 말이 없어요.)"),
                        L("First time since he passed. Sitting across from someone, eating.", "영감 가고 처음이네. 누구랑 마주 앉아서 밥 먹는 거."),
                        L("It's good. Truly.", "맛있다. 정말로."),
                    },
                    heart = 4f,
                },
                new ChapterDef
                {
                    kind = ChapterKind.Masil,
                    invite = L("Come by in the evening, if you like. I sit out on the bench in front of the mill.", "저녁에 놀러 와. 나 방앗간 앞 평상에 나와 앉아 있어."),
                    hint = L("In the evening, visit her on the bench in front of the mill (masil)", "저녁에 방앗간 앞 평상으로 마실 가기"),
                    scene = new[]
                    {
                        L("He never said much. When he opened the mill before dawn, all he'd say was \"It's cold, sleep a bit more.\"",
                          "영감은 말이 없었어. 새벽에 방앗간 문 열면서 \"추운데 더 자\" 그 한 마디가 다였지."),
                        L("I wanted to hear that so badly. Every morning.", "그 말이 그렇게 듣고 싶더라. 아침마다."),
                        L("But since you started coming by... the evenings aren't so long.", "근데 자네가 오니께... 저녁이 덜 길어."),
                    },
                    rewardItems = Items("Carrot Pancake", 2), heart = 6f,
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
            eveningPosition = new Vector2(6.3f, 1.35f),
            eveningPlace = L("the plastic chairs outside the corner store", "행복슈퍼 앞 평상"),
            masilLines = new[]
            {
                L("Sit. ...Want a sip? It's just sikhye. Don't look at me like that.", "앉아. ...한 모금 할래? 식혜야. 그런 눈으로 보지 마."),
                L("Quiet night. I like it.", "조용한 밤이다. 좋네."),
                L("The forge is banked for the night. It'll still be warm in the morning.", "화덕 불은 재워 놨어. 아침까지 따뜻할 거야."),
            },
            chapters = new[]
            {
                new ChapterDef
                {
                    kind = ChapterKind.Pumasi, spot = "forge",
                    invite = L("I'm thinking of lighting the forge again... but there's no one to work the bellows. You know how? Doesn't matter. You just pump.",
                               "화덕에 불 좀 넣어 볼까 하는데... 풀무질할 사람이 없어. 할 줄 알아? 몰라도 돼. 밟기만 하면 돼."),
                    hint = L("By day, work the bellows at the forge by the hardware store", "낮에 철물점 앞 화덕 풀무질 거들기"),
                    steps = new[] { L("You pump the bellows. The coals glow.", "풀무를 밟아요. 숯이 빨갛게 살아나요."), L("Again. Sparks fly up.", "한 번 더. 불똥이 튀어 올라요."), L("The fire roars. Mr. Dongsu picks up his hammer.", "불이 활활. 동수 아저씨가 망치를 들어요.") },
                    scene = new[]
                    {
                        L("...Good fire. First in a year.", "...불 좋다. 일 년 만이야."),
                        L("Pumasi's got to be paid back. Give me your hoe. ...There, sharpened. The ground won't feel so heavy now.", "품앗이니까 갚아야지. 괭이 줘 봐. ...됐다, 벼려 놨다. 이제 땅이 덜 무거울 거야."),
                        L("And I'll show you how to make a copper watering can, while the fire's hot.", "불 뜨거울 때 구리 물뿌리개 만드는 법도 알려 주지."),
                    },
                    heart = 3f,
                    unlock = "recipe_copper_can", unlockText = L("New recipe: Copper Watering Can", "새 제작법: 구리 물뿌리개"),
                    favor = "favor_hoe", favorText = L("Your hoe is sharpened: tilling takes less energy", "괭이를 벼렸어요: 땅을 갈 때 기력이 덜 들어요"),
                },
                new ChapterDef
                {
                    kind = ChapterKind.Masil,
                    invite = L("...If you're not busy tonight, I sit outside the corner store. Not that I'm asking.", "...오늘 저녁에 할 일 없으면, 나 행복슈퍼 앞에 앉아 있어. 오라는 건 아니고."),
                    hint = L("In the evening, sit with him outside the corner store (masil)", "저녁에 행복슈퍼 앞 평상으로 마실 가기"),
                    scene = new[]
                    {
                        L("I've got a daughter. In Seoul. Does design, or something like that.", "딸이 하나 있어. 서울 가서 디자인인가 뭔가 해."),
                        L("We had a big fight. She asked what's the point of being a blacksmith these days. I said things too. Three years, no word.",
                          "대장장이 해서 뭐 하냐고, 한 번 크게 싸웠지. 나도 못된 소리 했고. 그 뒤로 연락이 없어. 삼 년째야."),
                        L("...You came from the city too. Is it a place where you hold on alone?", "...자네도 도시에서 왔지. 거긴, 혼자 버티는 데야?"),
                    },
                    heart = 3f,
                },
                new ChapterDef
                {
                    kind = ChapterKind.Letter,
                    invite = L("I tried to write her a letter. These hands are good with iron, not with words. ...Would you write it for me?",
                               "편지를 써 보려고 했는데. 이 손이 쇠는 잘 다뤄도 글은 영... 자네가 대신 좀 써 줄래?"),
                    hint = L("By day, help him write a letter to his daughter", "낮에 딸에게 보낼 편지 대신 써 드리기"),
                    scene = new[]
                    {
                        L("...So I just post this. That's it?", "...이걸 부치면 되는 거지. 그게 다지?"),
                        L("Thanks. Here, the first decent hoe I've made in a year. It's yours. Don't argue.", "고맙다. 자, 일 년 만에 제대로 만든 괭이다. 자네 거야. 말 말고 받아."),
                    },
                    rewardItems = Items("Fertile Hoe", 1), heart = 6f,
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
            eveningPosition = new Vector2(-2.35f, -1.65f),
            eveningPlace = L("the bench in your yard", "우리 마당 평상"),
            masilLines = new[]
            {
                L("Grandma's snoring already. Hehe.", "할머니 벌써 코 골아. 헤헤."),
                L("Do you think the fish sleep under the ice?", "물고기도 얼음 밑에서 자?"),
                L("When I grow up I'll live in the city. ...Or maybe not.", "나 크면 도시 가서 살 거야. ...아닌가."),
            },
            chapters = new[]
            {
                new ChapterDef
                {
                    kind = ChapterKind.Fish,
                    invite = L("Grandpa's got an old fishing rod. Wanna fish together? Alone is boring. Stand close to the stream and press E. Catch one and show me!",
                               "할아버지 옛날 낚싯대 있는데, 같이 낚시할래? 혼자 하면 심심해. 개울 가까이 서서 E 눌러. 한 마리 잡으면 나 보여 줘!"),
                    hint = L("Catch a fish in the stream with Mina's rod", "미나의 낚싯대로 개울에서 한 마리 낚기"),
                    scene = new[]
                    {
                        L("Woah! You really caught one! Next time I'll catch a bigger one.", "우와! 진짜 잡았다! 다음엔 내가 더 큰 거 잡을 거야."),
                        L("Here, I found these seeds in Grandpa's shed. Plant them in spring!", "자, 할아버지 창고에서 찾은 씨앗. 봄에 심어!"),
                    },
                    rewardItems = Items("Strawberry Seed", 3), heart = 2f,
                    unlock = "fishing", unlockText = L("Fishing! New recipe: Grilled Fish", "낚시를 할 수 있어요! 새 요리: 생선구이"),
                },
                new ChapterDef
                {
                    kind = ChapterKind.Pumasi, spot = "sled",
                    invite = L("The stream froze! Grandpa made me a sled, but there's no one to push me... Push me? Please?",
                               "개울 얼었어! 할아버지가 썰매 만들어 줬는데, 밀어 줄 사람이 없어... 밀어 줄래? 응?"),
                    hint = L("Push Mina's sled on the frozen stream", "얼어붙은 개울에서 미나 썰매 밀어 주기"),
                    steps = new[] { L("You give the sled a push. Mina squeals.", "썰매를 쭉 밀어요. 미나가 꺅 소리를 질러요."), L("Faster! She digs in with her ice picks.", "더 빨리! 미나가 꼬챙이로 얼음을 찍어요."), L("One last long glide down the stream.", "마지막으로 개울 끝까지 쭈욱.") },
                    scene = new[]
                    {
                        L("One more! ...No, you're tired, right? Hehe.", "한 번 더! ...아니다, 힘들지? 헤헤."),
                        L("To say thanks, I'll water your field in the mornings sometimes. Secretly.", "고마우니까 내가 아침에 가끔 밭에 물 줄게. 몰래."),
                    },
                    heart = 3f,
                    favor = "favor_water", favorText = L("Mina will sometimes water your crops in the morning", "미나가 가끔 아침에 밭에 물을 줘요"),
                },
                new ChapterDef
                {
                    kind = ChapterKind.Masil,
                    invite = L("Grandma goes to sleep at nine. ...Can I come sit on your bench in the evening?", "할머니는 아홉 시면 주무셔. ...저녁에 너네 마당 평상에 가 있어도 돼?"),
                    hint = L("In the evening, wait for Mina on the bench in your yard", "저녁에 마당 평상에서 미나 기다리기"),
                    scene = new[]
                    {
                        L("I snuck out. Grandma doesn't know.", "몰래 나왔어. 할머니는 몰라."),
                        L("Mom and Dad work in the city. They said they'd come for New Year. They said that last year too.", "엄마 아빠는 도시에서 일해. 설에 온대. 작년에도 그랬는데 못 왔어."),
                        L("You came from the city too. Why is everyone there so busy?", "너도 도시에서 왔잖아. 도시 사람들은 왜 다 바빠?"),
                        L("...Anyway. Now I've got somewhere to go in the evening.", "...그래도 이제 저녁에 갈 데가 생겼다."),
                    },
                    rewardItems = Items("Sunflower Seed", 5), heart = 6f,
                    unlock = "shop_sunflower", unlockText = L("Sunflower Seeds are now sold at the market", "이제 장터에서 해바라기 씨앗을 팔아요"),
                },
            },
        });

        // ---------------- Village Head Park: the loudspeaker, the village hall (Market) ----------------
        All.Add(new VillagerDef
        {
            id = "mayor",
            name = L("Village Head Park", "박 이장님"),
            scene = "Market",
            position = new Vector2(-3.9f, 0.9f),
            eveningPosition = new Vector2(5.55f, -3.05f),
            eveningPlace = L("the bench outside the village hall", "마을회관 앞 의자"),
            intro = L("Well now, you must be the one who moved into the empty house! I'm Park, the village head. Not many young folks left around here... it's good to see a new face. I make the announcements every morning on that loudspeaker.",
                      "아이고, 빈집에 새로 이사 온 양반이구먼! 나는 이 마을 이장 박가요. 여기 젊은 사람이 별로 없어... 새 얼굴 보니 좋네. 아침마다 저 확성기로 방송하니께, 귀 좀 기울여 줘요잉."),
            grayIdle = new[] { L("Eating well? You look thin. City folks always do.", "밥은 잘 챙겨 먹고? 얼굴이 반쪽이네. 도시 사람들은 다 그래."),
                               L("If you hear the loudspeaker crackle, that's me. Don't laugh at my voice.", "확성기 지지직 하면 나여. 목소리 갖고 웃지 말고.") },
            colorIdle = new[] { L("This village used to be full of children. Now it's quiet... but you're here now.", "이 마을에도 애들이 바글바글했는디. 지금은 조용하지... 그래도 이제 자네가 있으니께."),
                                L("Need a hand with anything? One announcement and the whole village comes running.", "일손 필요하면 말혀. 방송 한 번이면 온 마을이 달려와.") },
            restored = L("Ha! Look at that, the village has color in it again.", "허허! 보소, 마을에 다시 색이 도네."),
            seasonLines = new[]
            {
                L("Winter's when we make meju for the sauces. Grandma Boksun knows how.", "겨울엔 메주를 쑤는 거여. 복순 할매가 잘 알지."),
                L("Spring! Time to open the sauce jars and let the sun in.", "봄이여! 장독 뚜껑 열고 볕 좀 쬐어 줘야지."),
                L("Hot, isn't it? Come sit in the pavilion when it gets too much.", "덥지? 너무 더우면 정자에 와서 앉아 있어."),
                L("Autumn's busy. Kimchi-making day is coming, everyone will need a hand.", "가을은 바빠. 곧 김장날인디, 다들 일손이 필요할 거여."),
            },
            masilLines = new[]
            {
                L("The hall's warm tonight, thanks to you. The old folks are playing hwatu inside.", "자네 덕에 회관이 뜨끈혀. 안에서 어르신들 화투 치시네."),
                L("Count the lit windows with me. ...One more than last year.", "불 켜진 집 같이 세 봐. ...작년보다 하나 늘었어."),
            },
            chapters = new[]
            {
                new ChapterDef
                {
                    kind = ChapterKind.Pumasi, spot = "briquettes",
                    invite = L("The old folks at the village hall need coal briquettes in for the winter, but my back, I tell you...",
                               "마을회관 어르신들 겨울 나시려면 연탄을 들여놔야 하는디, 내 허리가 말이여..."),
                    hint = L("By day, carry coal briquettes into the village hall", "낮에 마을회관 연탄 나르기"),
                    steps = new[] { L("You carry a stack of briquettes inside. Your hands go black.", "연탄을 한 줄 날라요. 손이 새까매져요."), L("Another stack. An old man inside says thank you.", "또 한 줄. 안에 계신 할아버지가 고맙다고 해요."), L("The last one. The hall's stove is lit.", "마지막 줄. 회관 난로에 불이 들어가요.") },
                    scene = new[]
                    {
                        L("Ah, that's a relief. Young hands are something else.", "아이고 시원하다. 젊은 사람 손이 이렇게 좋아."),
                        L("Pumasi gets paid back. When you need hands, one announcement and the whole village will come.", "품앗이는 갚아야제. 나중에 자네 일손 필요하면, 방송 한 번이면 다들 올 거여."),
                    },
                    heart = 3f,
                    favor = "favor_hands", favorText = L("The village head will call the neighbors when you need a hand", "이장님이 일손이 필요할 때 이웃을 불러 줘요"),
                },
                new ChapterDef
                {
                    kind = ChapterKind.Saecham,
                    invite = L("After the broadcast my throat's all scratchy. Came out without breakfast again...", "방송 끝나면 목이 칼칼혀. 아침도 못 먹고 나왔네..."),
                    hint = L("Bring the village head something to eat (any food)", "이장님께 새참 가져다드리기 (아무 음식)"),
                    scene = new[]
                    {
                        L("Well I'll be. When did someone last bring me food?", "아따, 이게 얼마 만에 남이 챙겨 준 밥이여."),
                        L("Being village head, everyone asks me to do something for them. Nobody asks if I've eaten.", "이장 일 하면 다들 나한테 뭐 해 달라고만 하제. 나 밥 먹었냐 묻는 사람은 없어."),
                    },
                    heart = 3f,
                },
                new ChapterDef
                {
                    kind = ChapterKind.Masil,
                    invite = L("I sit out by the village hall of an evening. Come keep an old man company.", "저녁엔 회관 앞에 나와 앉아 있어. 늙은이 말동무나 해 줘."),
                    hint = L("In the evening, visit him outside the village hall (masil)", "저녁에 마을회관 앞으로 마실 가기"),
                    scene = new[]
                    {
                        L("Fifteen years I've been village head. In that time, twelve houses went empty.", "내가 이장 맡은 지 십오 년이여. 그새 빈집이 열두 채가 됐어."),
                        L("The school closed too. Some nights I can't sleep, wondering if the village will just... disappear.", "학교도 닫았고. 이러다 마을이 없어지면 어쩌나, 밤에 잠이 안 와."),
                        L("But you came. A light came on in an empty house... you don't know how good that felt.", "근데 자네가 왔잖여. 빈집에 불이 켜지니께... 그게 그렇게 좋더라고."),
                    },
                    heart = 6f,
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
