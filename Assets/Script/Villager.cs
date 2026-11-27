using System.Collections.Generic;
using UnityEngine;

// 회색 주민 (K-3: 이웃). E로 말을 걸면, 그 사람과 "같이 하는 일"로 이야기가 한 장씩 넘어가고 색이 1/3씩 돌아옵니다.
//  - 낮: 이야기 장의 부탁 (품앗이 / 새참 / 그 사람만의 일). 음식을 들고 말을 걸면 새참을 같이 먹음
//  - 저녁 (18~22시): 그 사람의 자리(평상, 의자)에 앉아 있음 → 마실 (하룻저녁에 한 집)
// 규칙은 Neighbors (유니티 없이 테스트됨), 보상과 장면은 NeighborScenes.
// WorldSpawner가 씬이 열릴 때 코드로 생성하므로 씬에 따로 배치할 필요가 없습니다.
public class Villager : MonoBehaviour
{
    public string villagerId;

    public static Villager InRange { get; private set; }
    static readonly List<Villager> all = new List<Villager>();
    public static Villager Find(string id) { foreach (Villager v in all) if (v != null && v.villagerId == id) return v; return null; }

    // 주민 정의는 언어가 바뀌면 다시 만들어지므로, 매번 ID로 찾습니다.
    private VillagerDef def => VillagerDB.Get(villagerId);
    private SpriteRenderer body;
    private SpriteRenderer bubble;
    private BoxCollider2D feet;
    private string shownKey;
    private int shownStage = -1;
    private int idleLine;
    private float pulse;
    private float baseScale = 1f;
    private Vector2 dayPos;
    private bool sittingOut;        // 저녁 자리에 나와 앉아 있음

    public static Villager Create(VillagerDef def, Material material, float characterScale)
    {
        GameObject go = new GameObject("Villager_" + def.id);
        Vector2 at = def.scene == "Farm" && FarmMap.Active ? FarmMap.Anchor(def.id, def.position) : InteriorRooms.DayPosition(def.id, def.position);   // H-2: 할머니는 방앗간 안
        go.transform.position = new Vector3(at.x, at.y, 0f);

        CircleCollider2D talkRange = go.AddComponent<CircleCollider2D>();
        talkRange.isTrigger = true;
        talkRange.radius = 1.0f;
        BoxCollider2D feet = go.AddComponent<BoxCollider2D>();   // 발밑만 막아서 자연스럽게 비켜 지나가도록
        feet.size = new Vector2(0.3f * characterScale, 0.14f * characterScale);
        feet.offset = new Vector2(0f, -0.41f * characterScale);

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(go.transform, false);
        bodyGO.transform.localScale = Vector3.one * characterScale;
        SpriteRenderer sr = bodyGO.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 10;

        GameObject bubbleGO = new GameObject("Bubble");
        bubbleGO.transform.SetParent(go.transform, false);
        bubbleGO.transform.localPosition = new Vector3(0.19f * characterScale, 0.57f * characterScale, 0f);
        bubbleGO.transform.localScale = Vector3.one * characterScale;
        SpriteRenderer bsr = bubbleGO.AddComponent<SpriteRenderer>();
        if (material != null) bsr.sharedMaterial = material;
        bsr.sprite = PixelArt.Load("World/bubble");
        bsr.sortingOrder = 30;

        Villager v = go.AddComponent<Villager>();
        v.villagerId = def.id;
        v.body = sr;
        v.bubble = bsr;
        v.feet = feet;
        v.baseScale = characterScale;
        v.dayPos = at;
        v.RefreshVisual();
        return v;
    }

    void Awake() { all.Add(this); }

    DataManager Data => DataManager.instance;
    VillagerProgress Progress => Data != null ? Data.story.Get(villagerId) : null;
    static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
    static int Hour => GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;
    public Sprite Portrait => body != null ? body.sprite : null;

    public void Pulse() { pulse = 1f; RefreshVisual(); }

    void RefreshVisual()
    {
        if (Progress == null || body == null || def == null) return;
        int stage = Neighbors.Stage(def, Progress);   // F-1: 넘긴 장의 비율 (장이 계절마다 이어짐)
        string key = sittingOut ? $"Villagers/{villagerId}_sit_{stage}" : $"Villagers/{villagerId}_{stage}";
        if (key != shownKey)
        {
            Sprite s = PixelArt.Load(key) ?? PixelArt.Load($"Villagers/{villagerId}_{stage}");
            if (s != null) body.sprite = s;
            if (shownStage >= 0 && stage > shownStage) pulse = 1f;
            shownStage = stage;
            shownKey = key;
        }
    }

    // 저녁이면 자기 자리(평상, 의자)로 나가 앉음
    void UpdatePlace()
    {
        bool evening = def.eveningPosition != Vector2.zero && Neighbors.IsEvening(Hour);
        if (evening == sittingOut) return;
        sittingOut = evening;
        Vector2 at = evening ? InteriorRooms.EveningPosition(def.id, def.eveningPosition) : dayPos;   // H-2: 이장님 저녁은 회관 난롯가
        transform.position = new Vector3(at.x, at.y, 0f);
        if (feet != null) feet.enabled = !evening;      // 앉아 있을 땐 옆자리를 막지 않음
        RefreshVisual();
    }

    bool HasSomethingToSay()
    {
        VillagerProgress p = Progress;
        if (p == null) return false;
        if (!p.met) return true;
        if (sittingOut) return Neighbors.CanMasil(Data.story, p, Today, Hour) && Neighbors.MasilChapter(def, p, Today);
        ChapterDef c = Neighbors.Next(def, p);
        return c != null && c.kind != ChapterKind.Masil && Neighbors.Ready(def, p, Today) && !(c.kind == ChapterKind.Pumasi && p.invited);
    }

    void Update()
    {
        if (def == null || Data == null) return;
        UpdatePlace();
        RefreshVisual();

        // 대보름 밤: 미나는 논의 달집 둘레에 가 있음 (RiteSpot이 그림)
        bool away = villagerId == "mina" && RiteScenes.MinaAtDaljip;
        body.enabled = !away;
        if (away)
        {
            if (bubble != null) bubble.enabled = false;
            if (InRange == this) InRange = null;
            return;
        }

        if (bubble != null)
        {
            bool show = HasSomethingToSay() && !HUD.DialogueOpen;
            bubble.enabled = show;
            if (show) bubble.transform.localPosition = new Vector3(0.19f * baseScale, (0.57f + Mathf.Sin(Time.time * 4f) * 0.02f) * baseScale, 0f);
        }

        // 색이 돌아올 때 톡 튀는 연출
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 2f);
            float k = 1f + Mathf.Sin(pulse * Mathf.PI) * 0.15f;
            body.transform.localScale = new Vector3(baseScale * k, baseScale * k, 1f);
        }

        bool canTalk = InRange == this && !HUD.DialogueOpen && Time.frameCount - HUD.DialogueClosedFrame > 1 && !Data.IsGameOver && !TitleScreen.Blocking;
        if (canTalk && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    string IdleLine()
    {
        string seasonLine = def.seasonLines != null && def.seasonLines.Length == 4 ? def.seasonLines[Data.story.season] : null;
        bool restored = Neighbors.Restored(def, Progress);
        string[] pool = restored ? def.colorIdle : def.grayIdle;
        string line = seasonLine != null && idleLine % 2 == 1 ? seasonLine : pool[idleLine % pool.Length];
        idleLine++;
        return line;
    }

    void Talk()
    {
        Data.NoteTalk();                       // 일기: "이웃과 이야기를 나눴다"
        VillagerProgress p = Progress;
        int today = Today, hour = Hour;
        ChapterDef c = Neighbors.Next(def, p);

        // 처음 만남
        if (!p.met)
        {
            Neighbors.Meet(p);
            Album.Report("villager_met", villagerId);
            var first = new List<string> { def.intro };
            if (c != null && c.kind != ChapterKind.Letter && c.season <= Neighbors.SeasonNow) first.AddRange(Invite(c, p));   // F-1: 다음 계절 이야기는 그 계절에
            HUD.Say(def.name, first.ToArray(), Portrait);
            return;
        }

        // 절기 의식 (K-4): 동짓날 팥죽, 대보름 아침 부럼
        if (RiteScenes.TryTalk(def)) return;

        // F-1: 삼짇날 마지막 화전을 받은 이웃이 한 번 고마워함
        if (Spring.ThanksDue(Data.story.spring, def.id))
        {
            Data.story.spring.hwajeonThanked = true;
            HUD.Say(def.name, new[] { SpringScenes.ThanksLine(def.id) }, Portrait);
            Data.ChangeHeart(1f, null);
            return;
        }

        // W-1: 목이 마른 채로 오면 복순 할머니가 식혜 한 그릇 (하루 한 번)
        if (WaterRules.CanGetSikhye(Data.story, def.id, today, Data.thirst / Mathf.Max(1f, Data.maxThirst)))
        {
            WaterRules.GiveSikhye(Data.story, today);
            HUD.Say(def.name, WaterRules.SikhyeLines(), Portrait, () =>
            {
                Data.thirst = Mathf.Min(Data.maxThirst, Data.thirst + WaterRules.SikhyeThirst);
                Data.hunger = Mathf.Min(Data.maxHunger, Data.hunger + WaterRules.SikhyeHunger);
                Data.NoteDrink();
                Sfx.Play("gulp", 0.8f);
                Data.ChangeHeart(WaterRules.SikhyeHeart, Loc.T("Grandma Boksun gave you a bowl of sikhye.", "복순 할머니가 식혜 한 그릇을 주셨어요."));
                Data.UpdateAllUI();
            });
            return;
        }

        // 저녁: 마실
        if (sittingOut)
        {
            if (Neighbors.CanMasil(Data.story, p, today, hour))
                HUD.Ask(def.name, Loc.T($"Sit with {def.name} for a while? (masil: one house a night)", $"{def.name} 옆에 앉아 있다 갈까요? (마실: 하룻저녁에 한 집)"), Portrait,
                        () => NeighborScenes.Masil(def, p, this), () => HUD.Say(def.name, new[] { IdleLine() }, Portrait),
                        Loc.T("Sit down", "앉기"), Loc.T("Just say hi", "인사만"));
            else
                HUD.Say(def.name, new[] { Loc.T("Already been visiting tonight? Go get some sleep.", "오늘 마실은 벌써 다녀왔지? 들어가 자.") }, Portrait);
            return;
        }

        // 낮: 음식을 들고 왔으면 새참
        string held = Hotbar.SelectedItem;
        ItemDef heldDef = string.IsNullOrEmpty(held) ? null : ItemDB.Get(held);
        bool food = heldDef != null && heldDef.category == ItemCategory.Food && Data.HasItem(held);
        if (Neighbors.SaechamChapter(def, p, today, hour, held, food))
        {
            HUD.Ask(def.name, c.invite + "\n<color=#ffd84a>" + Loc.T($"Share your {ItemNames.Display(held)} as a work-break meal?", $"{ItemNames.With(held, "을", "를")} 새참으로 같이 먹을까요?") + "</color>", Portrait,
                    () => NeighborScenes.Saecham(def, p, held, true), null, Loc.T("Eat together", "같이 먹기"), Loc.T("Not now", "나중에"));
            return;
        }

        // 이야기 장의 부탁
        if (c != null && Neighbors.Ready(def, p, today) && c.kind != ChapterKind.Masil)
        {
            if (c.kind == ChapterKind.Letter)
            {
                HUD.Say(def.name, new[] { c.invite }, Portrait, () => NeighborScenes.Letter(def, p));
                return;
            }
            if (!(c.kind == ChapterKind.Pumasi && p.invited) || idleLine % 2 == 0)
            {
                idleLine++;
                HUD.Say(def.name, Invite(c, p), Portrait);
                return;
            }
        }

        // 그냥 새참 (이야기와 상관없이, 주민마다 하루 한 번)
        if (Neighbors.CanSaecham(p, today, hour, food))
        {
            HUD.Ask(def.name, IdleLine() + "\n<color=#ffd84a>" + Loc.T($"Share your {ItemNames.Display(held)}?", $"{ItemNames.With(held, "을", "를")} 같이 나눠 먹을까요?") + "</color>", Portrait,
                    () => NeighborScenes.Saecham(def, p, held, false), null, Loc.T("Eat together", "같이 먹기"), Loc.T("Not now", "나중에"));
            return;
        }

        // F-1: 행복슈퍼 명숙 아주머니는 계산대에 있으니, 할 이야기가 없으면 바로 장 보기
        if (def.id == "myeong" && Neighbors.IsDay(hour) && InteriorRooms.Inside && Shop.OpenNow()) return;

        var lines = new List<string> { IdleLine() };
        if (c != null && c.kind == ChapterKind.Masil && Neighbors.Ready(def, p, today)) lines.Add(c.invite);
        HUD.Say(def.name, lines.ToArray(), Portrait);
    }

    // 부탁하는 말 + 무엇을 하면 되는지 (회색 글씨)
    string[] Invite(ChapterDef c, VillagerProgress p)
    {
        var lines = new List<string> { c.invite };
        switch (c.kind)
        {
            case ChapterKind.Pumasi:
                p.invited = true;
                break;
            case ChapterKind.Fish:
                if (!Data.HasItem("Fishing Rod"))
                {
                    Data.AddItem("Fishing Rod", 1);
                    Data.story.Unlock("fishing");
                    lines.Add("<color=#9dff9d>" + Loc.T("Received:", "받은 것:") + "</color> " + ItemNames.Display("Fishing Rod"));
                }
                break;
            case ChapterKind.Acorn:                           // F-1: 미나가 겨울 내내 모은 도토리
                if (!Data.HasItem("Acorn"))
                {
                    Data.AddItem("Acorn", 2);
                    lines.Add("<color=#9dff9d>" + Loc.T("Received:", "받은 것:") + "</color> " + ItemNames.Display("Acorn") + " x2");
                }
                break;
            case ChapterKind.Saecham:
                if (!string.IsNullOrEmpty(c.needItem) && !Data.HasItem(c.needItem))
                    lines.Add("<color=#c8c0b0>" + Loc.T($"(Hold a {ItemNames.Display(c.needItem)} and talk to them.)", $"({ItemNames.Display(c.needItem)}을(를) 손에 들고 말을 걸어요.)") + "</color>");
                break;
        }
        lines[lines.Count - 1] += "\n<color=#c8c0b0>" + c.hint + "</color>";
        return lines.ToArray();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        InRange = this;
        if (def != null) HUD.Toast(Loc.T($"Press E to talk to {def.name}.", $"E: {def.name}와(과) 이야기하기"), 2f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && InRange == this) InRange = null;
    }

    void OnDestroy()
    {
        all.Remove(this);
        if (InRange == this) InRange = null;
    }
}
