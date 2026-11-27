using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// F-1: 봄 장면들. 규칙은 Spring (SpringState.cs), 그림은 Tools/pixelart/spring.py.
//  - 마당 모판 (볍씨 뿌리기, 물 주기)         farm.json 앵커 "seedbed"
//  - 큰 논 위 물꼬 (논물 대기 품앗이)          앵커 "sluice"
//  - 모내기 줄 자리 (모내기 날, 한 포기씩)     앵커 "monaegi"
//  - 논 그림: 물 댄 논 / 모 심은 논 (농장 지도 크기의 덧그림)
//  - 삼짇날 화전놀이: 뒷산 옹달샘 자리 (ForestArea가 [E]를 넘겨줌)
public static class SpringScenes
{
    static DataManager Data => DataManager.instance;
    static StoryState Story => Data.story;
    static SpringState Sp => Data.story.spring;
    public static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
    public static int Hour => GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;

    public static Vector2 SeedbedAt => FarmMap.Anchor("seedbed", new Vector2(5.3f, -1.1f));
    public static Vector2 SluiceAt => FarmMap.Anchor("sluice", new Vector2(2.7f, -7.25f));
    public static Vector2 PlantAt => FarmMap.Anchor("monaegi", new Vector2(7.0f, -6.6f));
    public static readonly string[] Crowd = { "rosa", "theo", "mina", "myeong", "mayor" };

    static string Name(string id) => VillagerDB.Get(id)?.name ?? id;

    public static void Create(Scene scene, Material mat)
    {
        if (scene.name != "Farm" || !FarmMap.Active) return;
        Make(scene, "Seedbed", SpringSpot.Kind.Seedbed, SeedbedAt, mat, new Vector2(1.4f, 0.35f));
        Make(scene, "Sluice", SpringSpot.Kind.Sluice, SluiceAt, mat, Vector2.zero);
        SpringSpot line = Make(scene, "MonaegiLine", SpringSpot.Kind.Plant, PlantAt, mat, Vector2.zero);
        line.MakeCrowd(mat);

        var paddy = new GameObject("PaddySpring");
        SceneManager.MoveGameObjectToScene(paddy, scene);
        paddy.AddComponent<PaddyView>().Setup(mat);
    }

    static SpringSpot Make(Scene scene, string name, SpringSpot.Kind kind, Vector2 at, Material mat, Vector2 feet)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        SpringSpot spot = go.AddComponent<SpringSpot>();
        spot.kind = kind;
        spot.at = at;
        spot.view = AreaKit.SpriteAt(go.transform, "Art", null, at, kind == SpringSpot.Kind.Sluice ? -40 : 6, mat);
        if (feet != Vector2.zero)
        {
            spot.feet = go.AddComponent<BoxCollider2D>();
            spot.feet.size = feet;
            spot.feet.offset = new Vector2(0f, feet.y * 0.5f);
        }
        spot.Refresh();
        return spot;
    }

    // ---------------------------------------------------------------- 삼짇날 화전놀이 (ForestArea의 옹달샘 자리에서)
    public static bool TrySamjit()
    {
        if (Data == null || !Spring.SamjitNow(Story, Today, Hour)) return false;
        int azaleas = Data.GetCount("Azalea");
        var lines = new List<string>
        {
            Loc.T("You came! Sit, sit. The lid's hot already.", "왔어? 앉아, 앉아. 솥뚜껑 벌써 달궈 놨어."),
        };
        if (azaleas >= Spring.HwajeonAzaleas)
        {
            Data.RemoveItem("Azalea", Spring.HwajeonAzaleas);
            lines.Add(Loc.T("And you picked azaleas! Look at that color. Press one into each pancake, like this.", "진달래도 따 왔네! 색 좀 봐. 반죽마다 한 장씩, 이렇게 꾹 눌러."));
        }
        else
            lines.Add(Loc.T("No azaleas? Never mind, I picked plenty on the way up. Press one into each pancake, like this.", "진달래 못 땄어? 괜찮아, 올라오면서 내가 잔뜩 땄어. 반죽마다 한 장씩, 이렇게 꾹 눌러."));
        lines.Add(Loc.T("(Little white rounds of rice dough sizzle on the upturned lid. A pink flower in the middle of each.)", "(뒤집은 솥뚜껑 위에서 하얀 찹쌀 반죽이 지글지글. 가운데마다 분홍 꽃 한 송이.)"));
        lines.Add(Loc.T("He used to say the first one is for the mountain, the second for whoever you're with. ...Go on, eat.", "영감이 그랬어. 첫 장은 산한테, 둘째 장은 옆에 있는 사람한테. ...어서 먹어."));
        lines.Add(Loc.T("(Everyone eats with their fingers. Mina burns her tongue. The village head laughs too loud.)", "(다들 손으로 집어 먹어요. 미나가 혀를 데고, 이장님이 너무 크게 웃어요.)"));
        lines.Add(Loc.T("There's one left. Give it to someone, dear.", "한 장 남았네. 누구 줘 봐."));
        Sfx.Play("fire_whoosh", 0.4f);
        HUD.Say(Name("rosa"), lines.ToArray(), RiteScenes.Face("rosa"), GiveLast);
        return true;
    }

    static void GiveLast()
    {
        var ids = new List<string>();
        foreach (string id in Crowd)
            if (Story.Get(id).met) ids.Add(id);
        if (ids.Count == 0) ids.Add("rosa");
        var options = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++) options[i] = Name(ids[i]);
        ChoiceMenu.Show(Loc.T("Who gets the last flower pancake?", "마지막 화전 한 장, 누구에게?"), options, i =>
        {
            string to = ids[Mathf.Clamp(i, 0, ids.Count - 1)];
            Spring.FinishSamjit(Sp, to);
            Data.AddItem("Azalea Hwajeon", 2);
            Data.NoteRite(Loc.T($"Samjitnal: azalea pancakes on the back hill. I gave the last one to {Name(to)}.", $"삼짇날 뒷산에서 진달래 화전을 부쳐 먹었다. 마지막 한 장은 {Name(to)}에게."));
            HUD.Say(Name(to), new[] { GiveLine(to) }, RiteScenes.Face(to), () =>
            {
                Data.ChangeHeart(4f, Loc.T("Flower pancakes on the hill, shared.", "꽃 부침개를 산에서 나눠 먹었어요."));
                ColorWave.Request(true);
                Album.Report("rite", "samjit");
            });
        });
    }

    static string GiveLine(string id)
    {
        switch (id)
        {
            case "rosa": return Loc.T("For me? ...He always gave me the last one too. (She eats it very slowly.)", "나 주는 거여? ...영감도 마지막 장은 꼭 나 줬는디. (아주 천천히 드세요.)");
            case "theo": return Loc.T("Hmph. ...I'll save half. For when she visits.", "흠. ...반은 남겨 둘란다. 걔 오면 주게.");
            case "mina": return Loc.T("For me?! I'm going to take it to Grandpa. He can have one bite.", "나?! 할아버지 갖다줄 거야. 한 입만 줄 거야.");
            case "myeong": return Loc.T("Me? I'm the one who sells things. Nobody gives me things. ...Thank you.", "나? 난 파는 사람인데. 나한테 뭘 주는 사람은 없는데. ...고마워.");
            default: return Loc.T("Ha! The village head gets the last one? I'll announce it tomorrow.", "허허! 이장한테 마지막 장을? 내일 방송으로 자랑해야겠네.");
        }
    }

    // 다음에 말을 걸 때 한 번 (Villager.Talk)
    public static string ThanksLine(string id)
    {
        switch (id)
        {
            case "rosa": return Loc.T("That pancake on the hill... I dreamed of him that night. Smiling. Thank you, dear.", "산에서 준 화전... 그날 밤 영감 꿈을 꿨어. 웃고 있더라. 고마워.");
            case "theo": return Loc.T("She called. I told her about the pancake. She laughed. ...First time I've heard her laugh in a year.", "걔가 전화했더라. 화전 얘기 했더니 웃더라. ...일 년 만에 웃는 소리 들었다.");
            case "mina": return Loc.T("Grandpa ate the WHOLE thing. He said it tasted like when he was little.", "할아버지가 다 먹었어. 한 입이라더니! 어릴 때 맛이래.");
            case "myeong": return Loc.T("I put the pancake on the counter and just looked at it all afternoon. Silly, right?", "그 화전, 계산대에 올려놓고 오후 내내 보기만 했어. 웃기지?");
            default: return Loc.T("I really did announce it. \"The last pancake went to the village head.\" Ha!", "진짜로 방송했어. \"마지막 화전은 이장한테 왔습니다.\" 허허!");
        }
    }

    // ---------------------------------------------------------------- 모내기
    public static void FinishMonaegi(SpringSpot line)
    {
        var options = new string[Spring.HopeIds.Length];
        for (int i = 0; i < options.Length; i++) options[i] = Spring.HopeText(Spring.HopeIds[i]);
        HUD.Say(Name("mayor"), new[]
        {
            Loc.T("That's the last row! Put your hands on your back and stretch. Oof.", "마지막 줄이여! 허리 펴고. 아이고."),
            Loc.T("They say you plant a wish with the rice. Whatever grows, you cut in autumn.", "모 심을 때 바람도 같이 심는다 안 하나. 뭐든 자란 건 가을에 거두는 거여."),
        }, RiteScenes.Face("mayor"), () =>
            ChoiceMenu.Show(Loc.T("What do you want to grow in yourself this year?", "올해 나에게 키우고 싶은 것 하나"), options, i =>
            {
                string hope = Spring.HopeIds[Mathf.Clamp(i, 0, Spring.HopeIds.Length - 1)];
                Spring.FinishMonaegi(Sp, hope);
                bool mine = Spring.SeedStage(Sp, Today) >= 3;
                var lines = new List<string>
                {
                    Loc.T("(Everyone sits on the levee. Grandma Boksun unwraps rice cakes and makgeolli for the grown-ups, sikhye for Mina.)",
                          "(다들 논두렁에 앉아요. 복순 할머니가 떡을 풀고, 어른들은 막걸리, 미나는 식혜.)"),
                    Loc.T("(The planted paddy shines. Rows of tiny green, all the way to the far levee.)", "(모를 심은 논이 반짝여요. 조그만 초록 줄이 저 끝 논두렁까지.)"),
                };
                if (mine) lines.Add(Loc.T("Some of those seedlings came from your seedbed. You can't tell which. That's fine.", "저 중엔 내 모판에서 키운 모도 있어요. 어느 건지는 몰라요. 그래도 좋아요."));
                lines.Add(Loc.T($"You planted \"{Spring.HopeText(hope)}\" with the rice. You'll see what grew in autumn.", $"모와 함께 '{Spring.HopeText(hope)}'을(를) 심었어요. 무엇이 자랐는지는 가을에."));
                HUD.Say(Loc.T("Rice planting", "모내기"), lines.ToArray(), null, () =>
                {
                    Data.NoteRite(Loc.T($"Rice planting with the whole village. I planted \"{Spring.HopeText(hope)}\" with it.", $"온 마을이 모를 심었다. 나는 '{Spring.HopeText(hope)}'을(를) 같이 심었다."));
                    Data.ChangeHeart(mine ? 6f : 5f, Loc.T("The paddy is planted. Summer is close.", "논에 모가 다 들어갔어요. 여름이 가까워요."));
                    ColorWave.Request(true);
                    Album.Report("rite", "monaegi");
                    line.Refresh();
                });
            }));
    }
}

// 봄 자리 하나 (모판, 물꼬, 모내기 줄)
public class SpringSpot : MonoBehaviour
{
    public enum Kind { Seedbed, Sluice, Plant }
    public Kind kind;
    public Vector2 at;
    public SpriteRenderer view;
    public BoxCollider2D feet;
    readonly List<SpriteRenderer> crowd = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> tufts = new List<SpriteRenderer>();
    Material mat;
    const float Reach = 1.3f;

    static DataManager Data => DataManager.instance;
    static SpringState Sp => Data.story.spring;
    static int Today => SpringScenes.Today;
    static int Hour => SpringScenes.Hour;
    Vector2 Center => kind == Kind.Plant ? at : at + new Vector2(0f, 0.35f);

    static readonly Vector2[] CrowdAt = { new Vector2(-3.4f, 0.1f), new Vector2(-2.2f, 0.2f), new Vector2(2.2f, 0.15f), new Vector2(3.3f, 0.05f), new Vector2(4.6f, -1.2f) };

    public void MakeCrowd(Material material)
    {
        mat = material;
        for (int i = 0; i < SpringScenes.Crowd.Length; i++)
        {
            var go = new GameObject("Crowd_" + SpringScenes.Crowd[i]);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(at.x + CrowdAt[i].x, at.y + CrowdAt[i].y + 0.45f, 0f);
            go.transform.localScale = Vector3.one * PixelArt.CharacterScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = 10;
            sr.enabled = false;
            crowd.Add(sr);
        }
    }

    public void Refresh()
    {
        if (Data == null) return;
        StoryState s = Data.story;
        bool spring = Spring.IsSpring(s);
        string key = null;
        switch (kind)
        {
            case Kind.Seedbed:
                if (spring && !Sp.monaegiDone && Spring.Day(s, Today) >= Spring.NoticeDay) key = "seedbed_" + Spring.SeedStage(Sp, Today);
                break;
            case Kind.Sluice:
                if (spring) key = "sluice";
                break;
            case Kind.Plant:
                bool now = Spring.MonaegiNow(s, Today, Hour);
                key = now ? "monaegi_line" : null;
                for (int i = 0; i < crowd.Count; i++)
                {
                    string id = SpringScenes.Crowd[i];
                    crowd[i].enabled = now && s.Get(id).met;
                    if (crowd[i].enabled) crowd[i].sprite = RiteScenes.Face(id);
                }
                // 심은 포기 (오늘 줄 앞에)
                while (tufts.Count < (now ? Sp.planted : 0)) tufts.Add(Tuft(tufts.Count));
                for (int i = 0; i < tufts.Count; i++) tufts[i].enabled = now && i < Sp.planted;
                break;
        }
        if (key == null) view.sprite = null;
        else if (kind == Kind.Plant) AreaKit.SetSprite(view, key, at + new Vector2(0f, -1.55f));
        else AreaKit.SetSprite(view, key, at);
        if (feet != null) feet.enabled = key != null;
    }

    SpriteRenderer Tuft(int i)
    {
        var go = new GameObject("Tuft");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingOrder = 5;
        AreaKit.SetSprite(sr, "rice_tuft", at + new Vector2(-1.5f + i * 0.6f, -1.25f));
        return sr;
    }

    void Update()
    {
        if (Data == null || Data.IsGameOver || TitleScreen.Blocking || PixelHUD.ViewOpen || PixelHUD.BagOpen) return;
        if (Time.frameCount % 30 == 0) Refresh();
        PlayerAction p = PlayerAction.instance;
        if (p == null || Vector2.Distance(p.transform.position, Center) > Reach || !AreaKit.PressedE() || Villager.InRange != null) return;
        Use();
        Refresh();
    }

    void Use()
    {
        StoryState s = Data.story;
        switch (kind)
        {
            case Kind.Seedbed:
                if (!Spring.IsSpring(s) || Sp.monaegiDone) return;
                if (Spring.CanSow(s, Today))
                {
                    Spring.Sow(Sp, Today);
                    Sfx.Play("plant");
                    HUD.Toast(Loc.T("You scatter rice seed over the trays (the village head gave every house a bag). Water it in the days ahead.",
                                    "모판에 볍씨를 뿌렸어요 (이장님이 집집마다 나눠 준 볍씨). 며칠 물을 주세요."), 4f);
                    return;
                }
                if (Sp.seedSown < 0)
                {
                    HUD.Toast(Loc.T("An empty seedbed. Rice seed goes in once the village starts on the paddies.", "빈 모판. 마을이 논일을 시작하면 볍씨를 뿌려요."), 3f);
                    return;
                }
                int stage = Spring.SeedStage(Sp, Today);
                if (stage < 3 && Data.WaterCapacity > 0 && Data.waterCharges > 0 && Spring.WaterSeedbed(Sp, Today))
                {
                    Data.waterCharges--;
                    Data.UpdateAllUI();
                    Sfx.Play("water");
                    HUD.Toast(Spring.SeedText(Spring.SeedStage(Sp, Today)), 2.5f);
                    return;
                }
                if (stage < 3 && Sp.seedLastWater == Today) { HUD.Toast(Loc.T("You already watered the seedbed today.", "오늘은 모판에 물을 줬어요."), 2.5f); return; }
                if (stage < 3 && (Data.WaterCapacity == 0 || Data.waterCharges <= 0)) { HUD.Toast(Loc.T("You need water in your watering can (the well).", "물뿌리개에 물이 있어야 해요 (우물)."), 2.5f); return; }
                HUD.Toast(Spring.SeedText(stage) + (stage >= 3 ? "  " + Loc.T($"Rice planting is on day {s.seasonStartDay + Spring.MonaegiDay - 1}.", $"모내기는 {s.seasonStartDay + Spring.MonaegiDay - 1}일째.") : ""), 3f);
                return;

            case Kind.Sluice:
                if (Spring.CanWaterPaddy(s, Today))
                {
                    if (!Data.UseEnergy(2f)) return;
                    bool done = Spring.WaterStep(Sp);
                    Sfx.Play(done ? "tap_water" : "till", 0.8f);
                    string[] steps =
                    {
                        Loc.T("You clear the dead leaves out of the ditch.", "도랑의 낙엽을 걷어 내요."),
                        Loc.T("The village head pulls the board up at the sluice. Water rushes in.", "이장님이 물꼬 판자를 들어 올려요. 물이 콸콸 들어와요."),
                        Loc.T("You patch the levee where it leaks. The paddy fills, silver.", "새는 논두렁을 메워요. 논이 은빛으로 차올라요."),
                    };
                    HUD.Toast(steps[Mathf.Clamp(Sp.waterSteps - 1, 0, 2)], 3f);
                    if (done)
                    {
                        Data.NoteTalk();
                        Data.ChangeHeart(2f, Loc.T("Water in the paddies. The frogs will be loud tonight.", "논에 물이 들었어요. 오늘 밤 개구리가 시끄럽겠어요."));
                    }
                    return;
                }
                if (Sp.watered) HUD.Toast(Loc.T("Water trickles in through the sluice.", "물꼬로 물이 졸졸 들어가요."), 2.5f);
                else if (Spring.IsSpring(s)) HUD.Toast(Loc.T($"The sluice for the big paddy. The village lets water in from day {s.seasonStartDay + Spring.WaterFrom - 1}.", $"큰 논 물꼬. {s.seasonStartDay + Spring.WaterFrom - 1}일째부터 물을 대요."), 3f);
                return;

            case Kind.Plant:
                if (!Spring.MonaegiNow(s, Today, Hour)) return;
                if (!Data.UseEnergy(2f)) return;
                bool all = Spring.Plant(Sp);
                Sfx.Play("plant");
                string[] calls = { Loc.T("\"Line!\" The rope moves down a step.", "\"줄 넘어가요!\" 못줄이 한 칸 내려가요."), Loc.T("Three seedlings, pressed into the mud with your thumb.", "모 서너 포기를 엄지로 진흙에 꾹."), Loc.T("Mina is faster than you. Much faster.", "미나가 나보다 빨라요. 훨씬.") };
                HUD.Toast(calls[(Sp.planted - 1) % calls.Length] + $"  ({Sp.planted}/{Spring.PlantSteps})", 2f);
                if (all) SpringScenes.FinishMonaegi(this);
                return;
        }
    }
}

// 논 덧그림: 봄에 물을 대면 물 댄 논, 모내기를 마치면 모 심은 논 (여름부터는 배경이 벼)
public class PaddyView : MonoBehaviour
{
    SpriteRenderer sr;
    string shown;
    float next;

    public void Setup(Material mat)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingOrder = -49;                       // 농장 바닥(-50) 바로 위, 밭 타일(0) 아래
        sr.enabled = false;
        Vector2 c = FarmMap.Map.Center;
        transform.position = new Vector3(c.x, c.y, 0f);
    }

    void Update()
    {
        if (Time.unscaledTime < next || DataManager.instance == null) return;
        next = Time.unscaledTime + 0.5f;
        SpringState sp = DataManager.instance.story.spring;
        string key = !Spring.IsSpring(DataManager.instance.story) ? null : sp.monaegiDone ? "paddy_planted" : sp.watered ? "paddy_flooded" : null;
        if (key == shown) return;
        shown = key;
        Sprite s = key != null ? PixelArt.Load("World/" + key) : null;
        sr.sprite = s;
        sr.enabled = s != null;
        if (s != null)
        {
            float k = PixelArt.PixelsPerUnit / s.pixelsPerUnit;
            transform.localScale = new Vector3(k, k, 1f);
        }
    }
}

// 삼짇날 뒷산 옹달샘: 화전 솥뚜껑과 둘러앉은 이웃 (그날 10~16시)
public class SamjitCrowd : MonoBehaviour
{
    readonly List<SpriteRenderer> people = new List<SpriteRenderer>();
    SpriteRenderer lid;
    Vector2 at;
    static readonly Vector2[] Around = { new Vector2(-1.2f, 0.3f), new Vector2(1.3f, 0.35f), new Vector2(-0.6f, -0.7f), new Vector2(0.7f, -0.75f), new Vector2(2.1f, -0.2f) };

    public static void Create(Transform parent, Vector2 seat, Material mat)
    {
        var go = new GameObject("Samjit");
        go.transform.SetParent(parent, false);
        SamjitCrowd c = go.AddComponent<SamjitCrowd>();
        c.at = seat;
        c.lid = AreaKit.SpriteAt(go.transform, "Lid", null, seat + new Vector2(0.1f, -0.45f), 6, mat);
        for (int i = 0; i < SpringScenes.Crowd.Length; i++)
        {
            var p = new GameObject("P_" + SpringScenes.Crowd[i]);
            p.transform.SetParent(go.transform, false);
            p.transform.position = new Vector3(seat.x + Around[i].x, seat.y + Around[i].y + 0.45f, 0f);
            p.transform.localScale = Vector3.one * PixelArt.CharacterScale;
            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = 10;
            sr.enabled = false;
            c.people.Add(sr);
        }
    }

    void Update()
    {
        if (Time.frameCount % 30 != 0 || DataManager.instance == null) return;
        StoryState s = DataManager.instance.story;
        bool on = Spring.SamjitNow(s, SpringScenes.Today, SpringScenes.Hour);
        if (on) AreaKit.SetSprite(lid, "hwajeon_lid", at + new Vector2(0.1f, -0.45f)); else lid.sprite = null;
        for (int i = 0; i < people.Count; i++)
        {
            string id = SpringScenes.Crowd[i];
            people[i].enabled = on && s.Get(id).met;
            if (people[i].enabled) people[i].sprite = RiteScenes.Face(id);
        }
    }
}
