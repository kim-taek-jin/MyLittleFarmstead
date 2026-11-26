using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// K-4: 절기 의식의 장면들. 규칙은 RiteState / Rites (유니티 없이 테스트됨).
//  - 장터: 방앗간 앞 팥죽 솥 (동지 준비, 동짓날 팥죽 받기)
//  - 농장: 대문 (팥 뿌리기), 남쪽 빈 논의 달집 (쌓기, 대보름 밤 의식, 모인 이웃들)
//  - 주민에게 말을 걸 때: 동짓날 팥죽 나르기, 대보름 아침 미나와 부럼
public static class RiteScenes
{
    static DataManager Data => DataManager.instance;
    public static RiteState R { get { RiteState r = Data.story.rites; r.Ensure(); return r; } }
    public static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
    public static int Hour => GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;
    public static bool Helped => Data != null && Data.story.IsUnlocked("favor_hands");   // 이장님 품앗이

    public static readonly Vector2 PotAt = new Vector2(-7.6f, -0.55f);     // 장터: 방앗간 앞 길
    public static readonly Vector2 GateAt = new Vector2(2.0f, -2.55f);     // 농장: 마당 돌담의 열린 곳
    public static Vector2 DaljipAt => FarmMap.Anchor("daljip", new Vector2(18.5f, -9.4f));   // 농장 남동쪽, 벼를 벤 빈 논 (그루터기 S)

    // 대보름 밤에는 미나도 달집 둘레에 (마당 평상에는 없음)
    public static bool MinaAtDaljip => Data != null && Rites.Gathering(R, Today, Hour, Helped);

    public static Sprite Face(string id)
    {
        Villager v = Villager.Find(id);
        if (v != null && v.Portrait != null) return v.Portrait;
        return PixelArt.Load($"Villagers/{id}_{Mathf.Clamp(Data.story.Get(id).wishIndex, 0, 3)}");
    }

    static string Name(string id) => VillagerDB.Get(id)?.name ?? id;
    static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

    public static void Create(Scene scene, Material mat)
    {
        if (scene.name == "Market") Make(scene, "Rite_Pot", RiteSpot.Kind.Pot, PotAt, mat, new Vector2(0.8f, 0.3f));
        if (scene.name == "Farm")
        {
            Make(scene, "Rite_Gate", RiteSpot.Kind.Gate, GateAt, mat, Vector2.zero);
            Make(scene, "Rite_Daljip", RiteSpot.Kind.Daljip, DaljipAt, mat, new Vector2(1.2f, 0.4f));
        }
    }

    static void Make(Scene scene, string name, RiteSpot.Kind kind, Vector2 at, Material mat, Vector2 feet)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        RiteSpot spot = go.AddComponent<RiteSpot>();
        spot.kind = kind;
        spot.at = at;
        spot.view = AreaKit.SpriteAt(go.transform, "Art", null, at, kind == RiteSpot.Kind.Gate ? 1 : 6, mat);
        if (feet != Vector2.zero)
        {
            spot.feet = go.AddComponent<BoxCollider2D>();
            spot.feet.size = feet;
            spot.feet.offset = new Vector2(0f, feet.y * 0.5f);
        }
        if (kind == RiteSpot.Kind.Daljip) spot.MakeCrowd(mat);
        spot.Refresh();
    }

    // ---------------------------------------------------------------- 주민에게 말을 걸 때 (Villager.Talk가 먼저 물어봄)
    public static bool TryTalk(VillagerDef def)
    {
        if (Data == null) return false;
        RiteState r = R;
        int day = Today, hour = Hour;

        // 동짓날: 할머니에게 팥죽 받기
        if (def.id == "rosa" && Rites.CanTakePot(r, day)) { TakePot(); return true; }
        // 동짓날: 이웃에게 한 그릇
        if (Rites.CanDeliver(r, day, def.id) && Data.HasItem("Red Bean Porridge"))
        {
            Data.RemoveItem("Red Bean Porridge", 1);
            bool done = Rites.Deliver(r, def.id);
            Sfx.Play("pickup");
            HUD.Say(def.name, new[] { Rites.DongjiLine(def.id) }, Face(def.id), done ? (System.Action)FinishDongji : null);
            return true;
        }
        // 대보름 아침: 미나와 부럼
        if (def.id == "mina" && Rites.BureomTime(r, day, hour)) { Bureom(); return true; }
        return false;
    }

    // ---------------------------------------------------------------- 동지
    public static void TakePot()
    {
        RiteState r = R;
        r.dongjiPot = true;
        Data.AddItem("Red Bean Porridge", Rites.Bowls);
        HUD.Say(Name("rosa"), new[]
        {
            Loc.T("Five bowls. One for each house: the Dongsu place, little Mina, the village head... and me, of course.",
                  "다섯 그릇이여. 집집마다 한 그릇씩. 동수네, 미나, 이장... 나도 한 그릇 받아야지."),
            Loc.T("And the last one, scatter it at your own gate. Red beans keep the bad things out.", "남은 한 그릇은 자네 집 대문에 뿌려. 팥이 나쁜 걸 못 들어오게 막아."),
        }, Face("rosa"));
        HUD.Toast(Loc.T($"Received: Red Bean Porridge x{Rites.Bowls}", $"받은 것: 팥죽 {Rites.Bowls}그릇"), 3f);
    }

    public static void FinishDongji()
    {
        Data.NoteRite(Loc.T("On the solstice I carried porridge from house to house. The longest night, and I wasn't cold.",
                            "동짓날 집집마다 팥죽을 날랐다. 가장 긴 밤인데 춥지 않았다."));
        Data.ChangeHeart(4f, Loc.T("Every house got a bowl of porridge.", "집집마다 팥죽 한 그릇씩 돌아갔어요."));
        ColorWave.Request(false);
        HUD.Announce("<size=40>" + Loc.T("Winter solstice", "동지") + "</size>\n\n" +
                     Loc.T("The longest night of the year.\nFrom tomorrow, the days grow longer again.", "일 년 중 밤이 가장 긴 날.\n내일부터는 해가 다시 길어져요."), 6f);
    }

    // ---------------------------------------------------------------- 대보름 아침: 부럼
    static void Bureom()
    {
        HUD.Say(Name("mina"), new[]
        {
            Loc.T("Grandma says to bite these. Walnuts and peanuts. They're called bureom.", "할머니가 이거 깨물래. 호두랑 땅콩. 부럼이래."),
            Loc.T("If you crack something hard with your teeth on the morning of Daeboreum, you won't get boils all year. ...I don't know what boils are either.",
                  "대보름 아침에 딱딱한 걸 이로 깨물면, 일 년 내내 부스럼이 안 난대. ...부스럼이 뭔지는 나도 몰라."),
            Loc.T("On three, together! One, two, three... *crack!*", "하나, 둘, 셋 하면 같이! 하나, 둘, 셋... 딱!"),
        }, Face("mina"), () =>
        {
            R.bureom = true;
            Sfx.Play("hit");
            Data.NoteTalk();
            Data.ChangeHeart(1f, Loc.T("Crack! No boils this year.", "딱! 올해는 부스럼 없이."));
            Data.NoteRite(Loc.T("Mina and I cracked walnuts with our teeth this morning. Hers went flying.", "아침에 미나랑 호두를 깨물었다. 미나 호두는 저 멀리 튀어 나갔다."));
        });
    }

    // ---------------------------------------------------------------- 대보름 밤: 쥐불놀이 → 달맞이 → 태우기 → 소원지
    public static void Ceremony(RiteSpot daljip)
    {
        RiteState r = R;
        if (!r.jwibul)
        {
            HUD.Say(Name("mina"), new[]
            {
                Loc.T("I put embers in the can! Swing it round and round, like this!", "깡통에 숯 넣었어! 이렇게 빙빙 돌려 봐!"),
                Loc.T("(Hold the wire and swing. A ring of fire spins in the dark.)", "(철사를 잡고 돌려요. 어둠 속에 불 동그라미가 돌아요.)"),
            }, Face("mina"), () => daljip.StartCoroutine(daljip.Jwibul(() => { r.jwibul = true; Moonrise(daljip); })));
            return;
        }
        Moonrise(daljip);
    }

    static void Moonrise(RiteSpot daljip)
    {
        HUD.Say(Name("mayor"), new[]
        {
            Loc.T("There! The moon's up! Light it, light it!", "달 떴다! 불 붙여라!"),
            Loc.T("Everyone, make a wish when the flames go up.", "다들 불 올라갈 때 소원 빌어!"),
        }, Face("mayor"), () =>
        {
            R.lit = true;
            daljip.Refresh();
            Sfx.Play("open");
            PixelHUD.ShowView(new List<string> { "rite_moon" },
                Loc.T("<color=#ffd84a>The first full moon of the year</color>   The moon house burns, and the village stands around it.",
                      "<color=#ffd84a>정월대보름 달</color>   달집이 타오르고, 마을 사람들이 둘레에 서 있어요."),
                Loc.T("[E] Continue", "[E] 계속"), Burn);
        });
    }

    static void Burn()
    {
        var options = new string[Rites.BurnIds.Length];
        for (int i = 0; i < options.Length; i++) options[i] = Rites.BurnText(Rites.BurnIds[i]);
        ChoiceMenu.Show(Loc.T("You brought something from the city. What goes into the fire?", "도시에서 가져온 것 하나. 무엇을 달집에 넣을까요?"), options, i =>
        {
            R.burned = Rites.BurnIds[i];
            HUD.Say(Loc.T("Me", "나"), new[] { Rites.BurnLine(R.burned) }, null, Wish);
        });
    }

    static void Wish()
    {
        var options = new string[Rites.WishIds.Length];
        for (int i = 0; i < options.Length; i++) options[i] = Rites.WishText(Rites.WishIds[i]);
        ChoiceMenu.Show(Loc.T("A wish paper. One wish, just for you.", "소원지. 나를 위한 바람 하나."), options, i =>
        {
            RiteState r = R;
            r.wish = Rites.WishIds[i];
            Rites.Finish(r);
            Data.NoteRite(Loc.T($"I put {LowerFirst(Rites.BurnText(r.burned))} in the moon house fire, and wrote \"{Rites.WishText(r.wish)}\" on a wish paper.",
                                $"달집에 {Rites.BurnText(r.burned)}을(를) 넣었다. 소원지에는 '{Rites.WishText(r.wish)}'라고 적었다."));
            HUD.Say(Name("rosa"), new[]
            {
                Loc.T($"Tied your wish on? \"{Rites.WishText(r.wish)}\"... good. The fire will carry it up.", $"소원지 매달았제? '{Rites.WishText(r.wish)}'... 좋다. 불이 다 가지고 올라갈 거여."),
            }, Face("rosa"), () => HUD.Say(Name("mayor"), new[]
            {
                Loc.T("Well then. Spring will be here tomorrow morning. You'll see.", "자, 이제 내일 아침이면 봄이여. 두고 봐."),
            }, Face("mayor"), () =>
            {
                Data.ChangeHeart(6f, Loc.T("The village greeted the first full moon together.", "마을이 다 같이 정월대보름 달을 맞았어요."));
                ColorWave.Request(true);
                Album.Report("rite", "daeboreum");
            }));
        });
    }
}

// 의식 자리 하나: 팥죽 솥 / 대문 / 달집
public class RiteSpot : MonoBehaviour
{
    public enum Kind { Pot, Gate, Daljip }
    public Kind kind;
    public Vector2 at;
    public SpriteRenderer view;
    public BoxCollider2D feet;
    readonly List<SpriteRenderer> crowd = new List<SpriteRenderer>();
    float flicker;

    static DataManager Data => DataManager.instance;
    static RiteState R => RiteScenes.R;
    const float Reach = 1.5f;

    // 대보름 밤 달집 둘레에 모인 이웃들 (그림만; 이야기는 의식 장면에서)
    static readonly string[] CrowdIds = { "rosa", "theo", "mayor", "mina" };
    static readonly Vector2[] CrowdAt = { new Vector2(-1.5f, -0.2f), new Vector2(1.5f, -0.1f), new Vector2(-0.9f, -0.95f), new Vector2(0.95f, -0.95f) };

    public void MakeCrowd(Material mat)
    {
        for (int i = 0; i < CrowdIds.Length; i++)
        {
            var go = new GameObject("Crowd_" + CrowdIds[i]);
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
        RiteState r = R;
        string key = null;
        switch (kind)
        {
            case Kind.Pot:
                key = Rites.DongjiWindow(RiteScenes.Today) && !r.dongjiDone ? "rite_pot" : null;
                break;
            case Kind.Gate:
                key = r.dongjiDelivered != null && r.dongjiDelivered.Contains("gate") && Data.CurrentSeason == Season.Winter ? "rite_redbeans" : null;
                break;
            case Kind.Daljip:
                if (r.boreumDone) key = Data.CurrentSeason == Season.Winter ? (RiteScenes.Today <= r.boreumDay + 1 && r.lit && RiteScenes.Hour >= 19 ? FireFrame() : "daljip_ash") : null;
                else if (r.lit) key = FireFrame();
                else if (Rites.Scheduled(r) && RiteScenes.Today >= r.boreumDay - Rites.Notice)
                    key = Rites.Built(r, RiteScenes.Helped) ? "daljip_built" : "daljip_frame";
                break;
        }
        JangSpots.SetArt(view, key, at);
        if (feet != null) feet.enabled = key != null;
        if (kind == Kind.Daljip)
        {
            bool gathered = Rites.Gathering(r, RiteScenes.Today, RiteScenes.Hour, RiteScenes.Helped) && !r.boreumDone;
            for (int i = 0; i < crowd.Count; i++)
            {
                crowd[i].enabled = gathered;
                if (gathered) crowd[i].sprite = RiteScenes.Face(CrowdIds[i]);
            }
        }
    }

    string FireFrame() => (Mathf.FloorToInt(flicker * 6f) % 2 == 0) ? "daljip_fire_0" : "daljip_fire_1";

    void Update()
    {
        if (Data == null || Data.IsGameOver || TitleScreen.Blocking || PixelHUD.ViewOpen || PixelHUD.BagOpen) return;
        flicker += Time.deltaTime;
        if (Time.frameCount % 10 == 0) Refresh();
        PlayerAction p = PlayerAction.instance;
        if (p == null || Vector2.Distance(p.transform.position, at + new Vector2(0f, kind == Kind.Daljip ? -0.2f : 0.3f)) > Reach) return;
        if (Villager.InRange != null || !AreaKit.PressedE()) return;
        Use();
        Refresh();
    }

    void Use()
    {
        RiteState r = R;
        int day = RiteScenes.Today, hour = RiteScenes.Hour;
        switch (kind)
        {
            case Kind.Pot:
                if (Rites.CanPrep(r, day, hour))
                {
                    if (!Data.UseEnergy(2f)) return;
                    bool done = Rites.PrepStep(r);
                    PlayerPixelAnimator.PlayAction("harvest", at);
                    ActionFX.Dust(at);
                    string[] steps =
                    {
                        Loc.T("You sort the red beans, picking out the little stones.", "팥을 고르며 작은 돌을 골라내요."),
                        Loc.T("You press the boiled beans through a sieve. The kitchen smells sweet.", "삶은 팥을 체에 내려요. 단내가 퍼져요."),
                        Loc.T("You roll little rice-flour balls (saealsim) between your palms.", "찹쌀 반죽을 손바닥에 굴려 새알심을 빚어요."),
                    };
                    HUD.Toast(steps[Mathf.Clamp(r.dongjiPrep - 1, 0, 2)] + $"  ({r.dongjiPrep}/{Rites.PrepSteps})", 2.5f);
                    if (done) RememberWhileRolling();
                    return;
                }
                if (Rites.CanTakePot(r, day)) { RiteScenes.TakePot(); return; }
                HUD.Toast(r.dongjiPrep >= Rites.PrepSteps && !r.dongjiPot
                          ? Loc.T("The porridge will be ready on the solstice (day 7).", "팥죽은 동짓날(7일째)에 다 돼요.")
                          : Loc.T("A big pot of red bean porridge, bubbling.", "팥죽이 보글보글 끓는 큰 솥."), 2.5f);
                return;

            case Kind.Gate:
                if (Rites.CanDeliver(r, day, "gate") && Data.HasItem("Red Bean Porridge"))
                {
                    Data.RemoveItem("Red Bean Porridge", 1);
                    bool done = Rites.Deliver(r, "gate");
                    Sfx.Play("plant");
                    PlayerPixelAnimator.PlayAction("water", at);
                    HUD.Toast(Loc.T("You scatter red beans at the gate. Bad memories stop here.", "대문에 팥을 뿌려요. 나쁜 기억은 여기서 멈춰라."), 3f);
                    if (done) RiteScenes.FinishDongji();
                }
                return;

            case Kind.Daljip:
                bool helped = RiteScenes.Helped;
                if (Rites.CanBuild(r, day, helped))
                {
                    int have = Data.GetCount("Wood");
                    if (have <= 0) { HUD.Toast(Loc.T($"The moon house needs wood. ({r.daljipWood}/{Rites.WoodNeeded(helped)})", $"달집 쌓을 나무가 필요해요. ({r.daljipWood}/{Rites.WoodNeeded(helped)})"), 3f); return; }
                    int n = Rites.AddWood(r, have, helped);
                    Data.RemoveItem("Wood", n);
                    PlayerPixelAnimator.PlayAction("harvest", at);
                    ActionFX.Dust(at);
                    if (!Rites.Built(r, helped)) { HUD.Toast(Loc.T($"You stack the wood. ({r.daljipWood}/{Rites.WoodNeeded(helped)})", $"나무를 쌓았어요. ({r.daljipWood}/{Rites.WoodNeeded(helped)})"), 2.5f); return; }
                    Data.NotePumasi();
                    var lines = new List<string>();
                    if (helped) lines.Add(Loc.T("See? One announcement and the whole village came out. Pumasi pays back.", "봐, 방송 한 번 하니께 온 동네가 다 나왔지. 품앗이는 이렇게 돌아오는 거여."));
                    lines.Add(Loc.T("Grandma Boksun brought straw, Dongsu brought pine branches, and Mina tied the rope. It's a fine moon house.",
                                    "복순 할매는 짚을, 동수는 솔가지를 가져오고, 미나가 새끼줄을 묶었어. 달집 잘 섰다."));
                    lines.Add(Loc.T($"Seven o'clock on Daeboreum night (day {r.boreumDay}). Don't be late.", $"대보름({r.boreumDay}일째) 저녁 일곱 시여. 늦지 말고."));
                    HUD.Say(VillagerDB.Get("mayor")?.name, lines.ToArray(), RiteScenes.Face("mayor"));
                    return;
                }
                if (Rites.Built(r, helped) && Rites.Night(r, day, hour) && !r.boreumDone) { RiteScenes.Ceremony(this); return; }
                if (Rites.Built(r, helped) && !r.boreumDone)
                {
                    HUD.Toast(day < r.boreumDay ? Loc.T($"The moon house is ready. Daeboreum is day {r.boreumDay}, at seven in the evening.", $"달집이 다 섰어요. 대보름은 {r.boreumDay}일째 저녁 일곱 시.")
                                                : Loc.T("Come back at seven this evening.", "저녁 일곱 시에 다시 와요."), 3f);
                    return;
                }
                if (r.boreumDone) HUD.Toast(Loc.T("Where the moon house burned. The ashes are still warm.", "달집이 탄 자리. 재가 아직 따뜻해요."), 2.5f);
                return;
        }
    }

    // 새알심을 빚으며 떠올린 도시의 기억
    static void RememberWhileRolling()
    {
        var options = new string[Rites.MemoryIds.Length];
        for (int i = 0; i < options.Length; i++) options[i] = Rites.MemoryText(Rites.MemoryIds[i]);
        ChoiceMenu.Show(Loc.T("Rolling rice balls, something from the city comes back to you...", "새알심을 빚다 보니, 도시에서의 일이 하나 떠올라요..."), options, i =>
        {
            RiteState r = R;
            r.dongjiMemory = Rites.MemoryIds[i];
            Data.NoteRite(Rites.MemoryDiary(r.dongjiMemory));
            HUD.Say(VillagerDB.Get("rosa")?.name, new[]
            {
                Loc.T("You've gone quiet. ...You don't have to say. Rice balls are for rolling things up and letting them go.", "말이 없네. ...말 안 해도 돼. 새알심은 그런 거 동그랗게 빚어서 보내는 거여."),
                Loc.T("Come back on the solstice. I'll ladle you out the porridge.", "동짓날 와. 팥죽 퍼 줄게."),
            }, RiteScenes.Face("rosa"));
        });
    }

    // 쥐불놀이: 주인공 둘레로 불꽃이 원을 그리며 돎
    public IEnumerator Jwibul(System.Action done)
    {
        PlayerAction p = PlayerAction.instance;
        var sparks = new List<SpriteRenderer>();
        Sprite s = PixelArt.Load("World/jwibul_spark");
        for (int i = 0; i < 10; i++)
        {
            var go = new GameObject("Jwibul");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = 26;
            go.transform.localScale = Vector3.one * (PixelArt.PixelsPerUnit / (s != null ? s.pixelsPerUnit : 32f));
            sparks.Add(sr);
        }
        float t = 0f;
        Sfx.Play("swish");
        while (t < 2.6f)
        {
            t += Time.deltaTime;
            Vector3 c = p != null ? p.transform.position + new Vector3(0f, 0.1f, 0f) : (Vector3)at;
            for (int i = 0; i < sparks.Count; i++)
            {
                float a = t * 9f - i * 0.22f;
                sparks[i].transform.position = c + new Vector3(Mathf.Cos(a) * 0.75f, Mathf.Sin(a) * 0.75f, 0f);
                sparks[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - i / 10f) * Mathf.Clamp01((2.6f - t) * 2f));
            }
            yield return null;
        }
        foreach (SpriteRenderer sr in sparks) Destroy(sr.gameObject);
        done?.Invoke();
    }
}
