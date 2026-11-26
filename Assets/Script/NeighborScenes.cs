using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// K-3: 이웃과 같이 보내는 장면들 (새참, 마실, 편지, 이야기 장 넘기기)과 품앗이 자리.
// 규칙은 Neighbors, 대사는 VillagerDB의 ChapterDef.
public static class NeighborScenes
{
    static DataManager Data => DataManager.instance;
    static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;

    static Sprite PortraitOf(string id)
    {
        Villager v = Villager.Find(id);
        if (v != null && v.Portrait != null) return v.Portrait;
        int stage = Mathf.Clamp(Data.story.Get(id).wishIndex, 0, 3);
        return PixelArt.Load($"Villagers/{id}_{stage}");
    }

    // ---------------------------------------------------------------- 이야기 한 장 넘기기 (보상, 품, 색)
    public static void Finish(VillagerDef def, VillagerProgress p, ChapterDef c, System.Action onDone = null, string[] before = null)
    {
        Neighbors.Complete(p, Today);
        bool restored = Neighbors.Restored(def, p);
        Data.AddKindness(3);
        Album.Report("wish", def.id);
        if (restored) Album.Report("restored", def.id);

        var got = new List<string>();
        if (c.gold > 0) { Data.AddGold(c.gold); got.Add($"{c.gold}G"); }
        foreach (var s in c.rewardItems) { Data.AddItem(s.itemName, s.count); got.Add($"{ItemNames.Display(s.itemName)} x{s.count}"); }
        if (Data.story.Unlock(c.unlock) && !string.IsNullOrEmpty(c.unlockText)) got.Add(c.unlockText);

        var lines = new List<string>();
        if (before != null) lines.AddRange(before);
        lines.AddRange(c.scene);
        if (got.Count > 0) lines.Add("<color=#9dff9d>" + Loc.T("Received:", "받은 것:") + "</color> " + string.Join(", ", got));
        if (Data.story.Unlock(c.favor) && !string.IsNullOrEmpty(c.favorText))
            lines.Add("<color=#ffd84a>" + Loc.T("Pumasi returned: ", "품앗이: ") + "</color>" + c.favorText);
        if (restored) lines.Add(def.restored);
        if (c.heart > 0f) Data.ChangeHeart(c.heart, Loc.T($"{def.name} smiled.", $"{def.name}의 얼굴에 웃음이 번졌어요."));

        Villager.Find(def.id)?.Pulse();
        HUD.Say(def.name, lines.ToArray(), PortraitOf(def.id), () =>
        {
            if (restored)
            {
                HUD.Announce("<size=40>" + Loc.T($"{def.name}'s colors have returned!", $"{def.name}에게 색이 돌아왔어요!") + "</size>\n\n"
                    + Loc.T($"{Data.story.RestoredVillagers} / {VillagerDB.All.Count} neighbors", $"색을 되찾은 이웃 {Data.story.RestoredVillagers} / {VillagerDB.All.Count}"), 5f);
                Data.ChangeHeart(5f, Loc.T("The village feels a little brighter.", "마을이 조금 더 환해진 것 같아요."));
            }
            onDone?.Invoke();
            StoryEvents.CheckEnding();
        });
    }

    // ---------------------------------------------------------------- 새참
    public static void Saecham(VillagerDef def, VillagerProgress p, string food, bool chapter)
    {
        if (!Data.HasItem(food)) return;
        Data.RemoveItem(food, 1);
        p.saechamDay = Today;
        Data.story.saechamCount++;
        Data.NoteSaecham();
        Sfx.Play("pickup");
        if (chapter) { Finish(def, p, Neighbors.Next(def, p)); return; }
        Data.AddKindness(1);
        Data.ChangeHeart(2f, Loc.T("You shared a work-break meal.", "새참을 나눠 먹었어요."));
        string[] thanks =
        {
            Loc.T($"(You sit down together and share the {ItemNames.Display(food)}.)", $"(나란히 앉아 {ItemNames.With(food, "을", "를")} 나눠 먹어요.)"),
            Loc.T("Food tastes better when someone brings it. Thank you.", "누가 챙겨 준 밥은 더 맛있어. 고마워."),
        };
        HUD.Say(def.name, thanks, PortraitOf(def.id));
    }

    // ---------------------------------------------------------------- 마실 (옆에 앉아서 이야기를 들음, 한 시간이 흐름)
    public static void Masil(VillagerDef def, VillagerProgress p, Villager who)
    {
        int today = Today;
        Data.story.masilDay = today;
        Data.story.masilCount++;
        Data.NoteMasil();
        PlayerAction player = PlayerAction.instance;
        Vector3 stood = player != null ? player.transform.position : Vector3.zero;
        Rigidbody2D rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (player != null)
        {
            if (rb != null) rb.simulated = false;           // 평상 위에 앉히는 동안 벽에 밀려나지 않게
            Vector2 seat = InteriorRooms.EveningPosition(def.id, def.eveningPosition) + new Vector2(0.72f, 0f);
            player.transform.position = new Vector3(seat.x, seat.y, stood.z);
            PlayerPixelAnimator.Sit(true);
        }
        System.Action standUp = () =>
        {
            if (player != null)
            {
                player.transform.position = stood;
                if (rb != null) rb.simulated = true;
            }
            PlayerPixelAnimator.Sit(false);
            GameTimeManager t = GameTimeManager.instance;
            if (t != null) t.SetTime(t.day, Mathf.Min(t.hour + 1, 23), t.minute);   // 이야기하다 보니 한 시간이
        };

        if (Neighbors.MasilChapter(def, p, today)) { Finish(def, p, Neighbors.Next(def, p), standUp); return; }

        Data.ChangeHeart(1f, Loc.T("A quiet evening with a neighbor.", "이웃과 조용한 저녁을 보냈어요."));
        string line = def.masilLines != null && def.masilLines.Length > 0 ? def.masilLines[today % def.masilLines.Length] : def.colorIdle[0];
        HUD.Say(def.name, new[] { Loc.T("(You sit side by side for a while.)", "(한동안 나란히 앉아 있어요.)"), line }, PortraitOf(def.id), standUp);
    }

    // ---------------------------------------------------------------- 동수 아저씨의 편지 (문장 두 개를 고름)
    public static void Letter(VillagerDef def, VillagerProgress p)
    {
        Sprite face = PortraitOf(def.id);
        string open1 = Loc.T("\"How are you doing? It's Dad.\"", "\"잘 지내니? 아빠다.\""), open2 = Loc.T("\"I'm sorry. It's Dad.\"", "\"미안하다. 아빠다.\"");
        string end1 = Loc.T("\"I miss you.\"", "\"보고 싶다.\""), end2 = Loc.T("\"Come anytime. I'll keep the fire going.\"", "\"언제든 와라. 불 피워 놓을게.\"");
        System.Action<string> second = first =>
            HUD.Ask(def.name, Loc.T("...And how should it end?", "...끝은 뭐라고 하지?"), face,
                    () => Write(def, p, first, end1), () => Write(def, p, first, end2), Loc.T("I miss you", "보고 싶다"), Loc.T("Come anytime", "언제든 와라"));
        HUD.Ask(def.name, Loc.T("How should it start? You write, I'll talk.", "첫 줄을 뭐라고 쓸까? 내가 말하면 자네가 받아 적어."), face,
                () => second(open1), () => second(open2), Loc.T("How are you?", "잘 지내니?"), Loc.T("I'm sorry.", "미안하다."));
    }

    static void Write(VillagerDef def, VillagerProgress p, string first, string last)
    {
        string middle = Loc.T("I lit the forge again. A neighbor worked the bellows for me. I remembered what you used to say, that it sings.",
                              "화덕에 다시 불을 넣었다. 이웃이 풀무질을 해 줬다. 네가 하던 말이 생각나더라. 화덕이 노래한다던.");
        HUD.Announce("<size=34>" + Loc.T("A letter", "편지") + "</size>\n\n" + first + "\n" + middle + "\n" + last, 9f);
        Finish(def, p, Neighbors.Next(def, p));
    }

    // ---------------------------------------------------------------- 미나와 낚시: 처음 한 마리를 낚으면
    public static void OnFishCaught()
    {
        VillagerDef mina = VillagerDB.Get("mina");
        if (mina == null || Data == null) return;
        VillagerProgress p = Data.story.Get("mina");
        ChapterDef c = Neighbors.Next(mina, p);
        if (c != null && c.kind == ChapterKind.Fish && p.met) Finish(mina, p, c);
    }

    // ---------------------------------------------------------------- 품앗이 자리 (떡 시루, 화덕 풀무, 썰매, 연탄)
    public struct Spot { public string id, who, scene; public Vector2 at; public Vector2 size; }
    public static readonly Spot[] Spots =
    {
        new Spot { id = "steamer",    who = "rosa",  scene = "Market", at = new Vector2(-7.8f, 0.95f), size = new Vector2(0.8f, 0.3f) },
        new Spot { id = "forge",      who = "theo",  scene = "Market", at = new Vector2(1.15f, 0.85f),  size = new Vector2(1.0f, 0.3f) },
        new Spot { id = "briquettes", who = "mayor", scene = "Market", at = new Vector2(4.35f, -3.35f), size = new Vector2(0.8f, 0.3f) },
        new Spot { id = "sled",       who = "mina",  scene = "Farm",   at = new Vector2(-6.0f, -3.55f), size = Vector2.zero },
    };

    public static void CreateSpots(Scene scene, Material mat)
    {
        foreach (Spot s0 in Spots)
        {
            Spot s = s0;
            if (s.scene != scene.name) continue;
            if (s.id == "steamer") { s.at = InteriorRooms.Anchor("mill", "steamer", s.at); s.size = new Vector2(0.8f, 0.3f); }   // H-2: 떡 시루는 방앗간 안
            var go = new GameObject("Chore_" + s.id);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = new Vector3(s.at.x, s.at.y, 0f);
            if (s.size != Vector2.zero)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = s.size;
                box.offset = new Vector2(0f, s.size.y * 0.5f);
            }
            ChoreSpot spot = go.AddComponent<ChoreSpot>();
            spot.spot = s;
            spot.view = AreaKit.SpriteAt(go.transform, "Art", null, s.at, 6, mat);
            spot.Refresh();
        }
        if (scene.name == "Market")
        {
            // 마을회관 (그림은 장터 배경에 있음): 건물만 막음
            var hall = new GameObject("VillageHall");
            SceneManager.MoveGameObjectToScene(hall, scene);
            BoxCollider2D b = hall.AddComponent<BoxCollider2D>();
            b.offset = new Vector2(6.85f, -1.75f);
            b.size = new Vector2(3.1f, 2.2f);
        }
    }
}

// 품앗이 자리 하나: 부탁을 들은 뒤 여기서 [E]를 세 번
public class ChoreSpot : MonoBehaviour
{
    public NeighborScenes.Spot spot;
    public SpriteRenderer view;
    const float Reach = 1.4f;

    static DataManager Data => DataManager.instance;
    static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
    static int Hour => GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;
    VillagerDef Who => VillagerDB.Get(spot.who);
    VillagerProgress Progress => Data.story.Get(spot.who);

    public void Refresh()
    {
        bool working = Progress.chore > 0 && Neighbors.Next(Who, Progress)?.spot == spot.id;
        string key = "chore_" + spot.id + (working ? "_on" : "");
        if (spot.id == "sled" && Data.CurrentSeason != Season.Winter && !working) key = null;   // 썰매는 겨울에만
        JangSpots.SetArt(view, key, spot.at);
    }

    void Update()
    {
        if (Data == null || Data.IsGameOver || TitleScreen.Blocking || PixelHUD.ViewOpen || PixelHUD.BagOpen) return;
        if (Time.frameCount % 30 == 0) Refresh();
        PlayerAction p = PlayerAction.instance;
        if (p == null || Vector2.Distance(p.transform.position, spot.at + new Vector2(0f, 0.3f)) > Reach) return;
        if (Villager.InRange != null || !AreaKit.PressedE()) return;
        Use();
        Refresh();
    }

    void Use()
    {
        VillagerDef d = Who;
        VillagerProgress pr = Progress;
        ChapterDef c = Neighbors.Next(d, pr);
        if (!Neighbors.CanChoreAt(d, pr, Today, Hour, spot.id))
        {
            bool mine = c != null && c.kind == ChapterKind.Pumasi && c.spot == spot.id;
            HUD.Toast(mine && !pr.invited ? Loc.T($"Talk to {d.name} first.", $"먼저 {d.name}에게 말을 걸어 봐요.")
                      : mine && !Neighbors.IsDay(Hour) ? Loc.T("It's too late for that today.", "오늘은 늦었어요. 낮에 해요.")
                      : Describe(d), 2.5f);
            return;
        }
        if (!Data.UseEnergy(3f)) return;
        bool done = Neighbors.ChoreStep(d, pr, Today, spot.id, out int step);
        PlayerPixelAnimator.PlayAction(spot.id == "sled" ? "harvest" : "hoe", spot.at);
        ActionFX.Dust(spot.at);
        Sfx.Play("click");
        HUD.Toast(c.steps[Mathf.Clamp(step - 1, 0, c.steps.Length - 1)] + $"  ({step}/{c.steps.Length})", 2.5f);
        if (done)
        {
            Data.NotePumasi();
            NeighborScenes.Finish(d, pr, c);
        }
    }

    string Describe(VillagerDef d)
    {
        switch (spot.id)
        {
            case "steamer": return Loc.T($"{d.name}'s rice-cake steamer.", $"{d.name}의 떡 시루.");
            case "forge": return Loc.T($"{d.name}'s forge and bellows.", $"{d.name}의 화덕과 풀무.");
            case "briquettes": return Loc.T("Coal briquettes for the village hall.", "마을회관에 들일 연탄.");
            default: return Loc.T("Mina's wooden ice sled.", "미나의 나무 얼음썰매.");
        }
    }
}
