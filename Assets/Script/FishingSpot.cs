using UnityEngine;

// 농장 연못 낚시. 낚싯대를 가지고 E → 기다렸다가 "!" 가 뜨면 바로 E.
// 마음(Heart)이 높을수록 황금 잉어가 잘 잡힙니다.
public class FishingSpot : MonoBehaviour
{
    enum State { Idle, Waiting, Bite }

    public float castEnergy = 3f;
    public bool lake;   // 호수 낚시터면 호수 물고기 표 (LakeState.LakeCatch)
    public float biteWindow = 0.9f;

    private State state = State.Idle;
    private float timer;
    private bool inRange;

    public static FishingSpot Create(Vector2 position, float radius)
    {
        GameObject go = new GameObject("FishingSpot");
        go.transform.position = position;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;
        return go.AddComponent<FishingSpot>();
    }

    DataManager Data => DataManager.instance;

    void Update()
    {
        if (Data == null || Data.IsGameOver) return;

        if (state == State.Waiting)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                state = State.Bite;
                timer = biteWindow + 0.2f * SpiritDB.PerkLevel(Data.story.spirit, "water");
                HUD.Toast("<size=40><color=#ffd84a>!!</color></size>  " + Loc.T("Something's biting! Press E!", "입질이 왔어요! E를 누르세요!"), biteWindow);
            }
        }
        else if (state == State.Bite)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                state = State.Idle;
                HUD.Toast(Loc.T("It got away...", "놓쳤어요..."), 2f);
            }
        }

        if (!inRange || Villager.InRange != null || HUD.DialogueOpen || Time.frameCount - HUD.DialogueClosedFrame <= 1) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        switch (state)
        {
            case State.Idle: Cast(); break;
            case State.Waiting:
                state = State.Idle;
                HUD.Toast(Loc.T("Too early! The fish swam away.", "너무 일렀어요! 물고기가 달아났어요."), 2f);
                break;
            case State.Bite: Catch(); break;
        }
    }

    void Cast()
    {
        if (!Data.HasItem("Fishing Rod"))
        {
            HUD.Toast(Loc.T("You need a fishing rod. Maybe someone nearby has an old one?", "낚싯대가 있어야 해요. 근처에 묵은 낚싯대를 가진 사람이 있지 않을까요?"), 3f);
            return;
        }
        if (!Data.UseEnergy(castEnergy)) return;
        state = State.Waiting;
        timer = Random.Range(1.5f, 4.5f);
        HUD.Toast(Loc.T("You cast your line... wait for it.", "낚싯줄을 던졌어요... 기다려 봐요."), 2f);
    }

    void Catch()
    {
        state = State.Idle;
        float heart01 = Data.heart / Data.maxHeart;
        float roll = Random.value;
        string fish;
        if (lake)
        {
            int hour = GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;
            fish = LakeState.LakeCatch(roll, Data.story.lake.ZonesClear, hour, heart01);
        }
        else
        {
            float koi = 0.03f + 0.07f * heart01 + 0.02f * SpiritDB.PerkLevel(Data.story.spirit, "water");
            if (roll < koi) fish = "Golden Koi";
            else if (roll < koi + 0.07f) fish = "Old Boot";
            else if (roll < koi + 0.07f + 0.35f) fish = "Carp";
            else fish = "Minnow";
        }

        bool first = Data.story.FishCount(fish) == 0;
        Data.AddItem(fish, 1);
        if (fish != "Old Boot")
        {
            Data.story.RecordFish(fish);
            Album.Report("fish", fish);
        }

        switch (fish)
        {
            case "Golden Koi":
                HUD.Announce(Loc.T("<size=42><color=#ffd84a>Golden Koi!</color></size>\n\nIt shimmers like sunlight on water.",
                                   "<size=42><color=#ffd84a>황금 잉어!</color></size>\n\n물 위의 햇살처럼 반짝여요."), 4f);
                if (first) Data.ChangeHeart(5f, Loc.T("A once-in-a-lifetime catch!", "평생 한 번 낚을까 말까 한 대어!"));
                break;
            case "Old Boot":
                HUD.Toast(Loc.T("You caught... an Old Boot. Well, it's something.", "낚은 건... 낡은 장화. 뭐, 없는 것보단 낫죠."), 3f);
                break;
            default:
                HUD.Toast(Loc.T($"You caught a {fish}!", $"{ItemNames.With(fish, "을", "를")} 낚았어요!") + (first ? "  <color=#9dff9d>" + Loc.T("(new!)", "(처음!)") + "</color>" : ""), 3f);
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
        if (Data != null && Data.HasItem("Fishing Rod")) HUD.Toast(Loc.T("Press E to fish.", "E를 눌러 낚시하세요."), 2f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
        if (state != State.Idle)
        {
            state = State.Idle;
            HUD.Toast(Loc.T("You reeled in your line.", "낚싯줄을 감았어요."), 1.5f);
        }
    }
}
