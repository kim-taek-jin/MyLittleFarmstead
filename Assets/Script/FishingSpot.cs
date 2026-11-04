using UnityEngine;

// 농장 연못 낚시. 낚싯대를 가지고 E → 기다렸다가 "!" 가 뜨면 바로 E.
// 마음(Heart)이 높을수록 황금 잉어가 잘 잡힙니다.
public class FishingSpot : MonoBehaviour
{
    enum State { Idle, Waiting, Bite }

    public float castEnergy = 3f;
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
                timer = biteWindow;
                HUD.Toast("<size=40><color=#ffd84a>!!</color></size>  Something's biting! Press E!", biteWindow);
            }
        }
        else if (state == State.Bite)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                state = State.Idle;
                HUD.Toast("It got away...", 2f);
            }
        }

        if (!inRange || Villager.InRange != null || HUD.DialogueOpen || Time.frameCount - HUD.DialogueClosedFrame <= 1) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        switch (state)
        {
            case State.Idle: Cast(); break;
            case State.Waiting:
                state = State.Idle;
                HUD.Toast("Too early! The fish swam away.", 2f);
                break;
            case State.Bite: Catch(); break;
        }
    }

    void Cast()
    {
        if (!Data.HasItem("Fishing Rod"))
        {
            HUD.Toast("You need a fishing rod. Maybe someone nearby has an old one?", 3f);
            return;
        }
        if (!Data.UseEnergy(castEnergy)) return;
        state = State.Waiting;
        timer = Random.Range(1.5f, 4.5f);
        HUD.Toast("You cast your line... wait for it.", 2f);
    }

    void Catch()
    {
        state = State.Idle;
        float heart01 = Data.heart / Data.maxHeart;
        float roll = Random.value;
        float koi = 0.03f + 0.07f * heart01;
        string fish;
        if (roll < koi) fish = "Golden Koi";
        else if (roll < koi + 0.07f) fish = "Old Boot";
        else if (roll < koi + 0.07f + 0.35f) fish = "Carp";
        else fish = "Minnow";

        bool first = Data.story.FishCount(fish) == 0;
        Data.AddItem(fish, 1);
        if (fish != "Old Boot") Data.story.RecordFish(fish);

        switch (fish)
        {
            case "Golden Koi":
                HUD.Announce("<size=42><color=#ffd84a>Golden Koi!</color></size>\n\nIt shimmers like sunlight on water.", 4f);
                if (first) Data.ChangeHeart(5f, "A once-in-a-lifetime catch!");
                break;
            case "Old Boot":
                HUD.Toast("You caught... an Old Boot. Well, it's something.", 3f);
                break;
            default:
                HUD.Toast($"You caught a {fish}!" + (first ? "  <color=#9dff9d>(new!)</color>" : ""), 3f);
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
        if (Data != null && Data.HasItem("Fishing Rod")) HUD.Toast("Press E to fish.", 2f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
        if (state != State.Idle)
        {
            state = State.Idle;
            HUD.Toast("You reeled in your line.", 1.5f);
        }
    }
}
