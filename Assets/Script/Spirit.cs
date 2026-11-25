using UnityEngine;
using UnityEngine.SceneManagement;

// 색을 먹는 정령. 처음 자연의 정수를 얻으면 나타나 플레이어를 따라다닙니다.
// G키: 들고 있는 아이템을 먹임 → 색 기운이 쌓이고, 가장 많이 먹은 색으로 모습과 능력이 바뀝니다.
public class Spirit : MonoBehaviour
{
    public static Spirit Instance { get; private set; }

    private SpriteRenderer sr;
    private Vector3 velocity;
    private string shownKey;
    private float pulse;
    private float scale = 1f;

    static DataManager Data => DataManager.instance;
    static SpiritState State => Data != null ? Data.story.spirit : null;

    // 정령이 이미 발견됐으면 생성 (불러오기 / 씬 이동 후에도 호출 가능)
    public static void EnsureSpawned()
    {
        if (Instance != null || State == null || !State.found) return;

        GameObject go = new GameObject("[Spirit]");
        DontDestroyOnLoad(go);
        Spirit s = go.AddComponent<Spirit>();
        s.sr = go.AddComponent<SpriteRenderer>();
        s.sr.sortingOrder = 11;
        if (PlayerAction.instance != null)
        {
            SpriteRenderer psr = PlayerAction.instance.GetComponent<SpriteRenderer>();
            if (psr != null) s.sr.sharedMaterial = psr.sharedMaterial;
            go.transform.position = PlayerAction.instance.transform.position + new Vector3(-0.46f, 0.41f, 0f) * PixelArt.CharacterScale;
        }
        s.scale = PixelArt.CharacterScale;
        go.transform.localScale = Vector3.one * s.scale;
        Instance = s;
        s.RefreshSprite(true);
    }

    // 처음 정수를 주웠을 때 DataManager가 호출
    public static void Discover()
    {
        SpiritState st = State;
        if (st == null || st.found) return;
        st.found = true;
        Record(SpiritDB.FormKey(st));
        EnsureSpawned();
        Album.Report("spirit_found");
        HUD.Announce(
            Loc.T("<size=40>A tiny gray spirit</size>\n\n" +
            "Drawn by the light, a little gray wisp drifts out and starts following you.\n" +
            "It looks hungry for color.\n\n" +
            "<size=24><color=#ffd84a>[G]</color> Feed it the item you're holding.\n" +
            "Sunshine/Sunflower/Corn = sun, Earth's Breath/Carrot/Potato = earth,\nFish = water, Apple/Strawberry/Jam = bloom.</size>",
            "<size=40>조그만 잿빛 정령</size>\n\n" +
            "불빛에 이끌려 작은 잿빛 도깨비불이 둥실 나와서 졸졸 따라와요.\n" +
            "빛깔에 굶주린 것 같아요.\n\n" +
            "<size=24><color=#ffd84a>[G]</color> 손에 든 걸 먹여 보세요.\n" +
            "햇살 조각/해바라기/옥수수 = 해, 대지의 숨결/당근/감자 = 흙,\n물고기 = 물, 사과/딸기/잼 = 꽃.</size>"), 9f);
    }

    static void Record(string key)
    {
        SpiritState st = State;
        if (st != null && !st.formsSeen.Contains(key)) st.formsSeen.Add(key);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (State == null || !State.found)
        {
            Destroy(gameObject);   // 새 게임 등으로 정령이 없는 상태
            return;
        }

        PlayerAction player = PlayerAction.instance;
        bool hidden = player == null || SceneManager.GetActiveScene().name == "MiniGame";
        sr.enabled = !hidden;
        if (hidden) return;

        // 플레이어의 왼쪽 위에서 둥실둥실 따라다님
        float follow = PixelArt.CharacterScale;
        Vector3 target = player.transform.position + new Vector3(-0.46f * follow, (0.41f + Mathf.Sin(Time.time * 2.5f) * 0.06f) * follow, 0f);
        target.z = player.transform.position.z;
        if ((transform.position - target).sqrMagnitude > 100f) transform.position = target;   // 씬 이동 직후 순간이동
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 0.25f);
        sr.flipX = velocity.x < -0.05f;

        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 2.5f);
            float k = 1f + Mathf.Sin(pulse * Mathf.PI) * 0.25f;
            transform.localScale = new Vector3(scale * k, scale * k, 1f);
        }

        RefreshSprite(false);

        if (Input.GetKeyDown(KeyCode.G) && !HUD.DialogueOpen && !TitleScreen.Blocking && Data != null && !Data.IsGameOver) Feed(Hotbar.SelectedItem);
    }

    void RefreshSprite(bool force)
    {
        string key = SpiritDB.FormKey(State);
        if (!force && key == shownKey) return;
        Sprite s = PixelArt.Load("Spirit/" + key);
        if (s != null) sr.sprite = s;
        shownKey = key;
    }

    void Feed(string item)
    {
        if (string.IsNullOrEmpty(item) || !Data.HasItem(item))
        {
            HUD.Toast(Loc.T("Hold something in your hotbar to feed your spirit.", "정령에게 먹일 걸 손에 들어 주세요."), 2f);
            return;
        }
        if (!SpiritDB.CanEat(item))
        {
            HUD.Toast(Loc.T($"Your spirit sniffs the {item} and turns away.", $"정령이 {ItemNames.Display(item)} 냄새를 맡더니 고개를 돌려요."), 2f);
            return;
        }

        SpiritState st = State;
        int stageBefore = SpiritDB.Stage(st);
        string formBefore = SpiritDB.FormKey(st);

        Data.RemoveItem(item, 1);
        SpiritDB.Feed(st, item);
        pulse = 1f;

        int[] p = SpiritDB.PointsFor(item);
        string gained = "";
        string[] labels = { "<color=#ffd84a>" + Loc.T("Sun", "해") + "</color>", "<color=#9dff9d>" + Loc.T("Earth", "흙") + "</color>",
                            "<color=#8fd0ff>" + Loc.T("Water", "물") + "</color>", "<color=#ff9fc4>" + Loc.T("Bloom", "꽃") + "</color>" };
        for (int i = 0; i < 4; i++) if (p[i] > 0) gained += $" +{p[i]} {labels[i]}";
        HUD.Toast(Loc.T($"Your spirit munches the {item}!", $"정령이 {ItemNames.With(item, "을", "를")} 냠냠 먹었어요!") + gained, 2.5f);

        string formAfter = SpiritDB.FormKey(st);
        if (formAfter != formBefore)
        {
            bool isNew = !st.formsSeen.Contains(formAfter);
            Record(formAfter);
            string grew = SpiritDB.Stage(st) > stageBefore ? Loc.T("Your spirit grew!", "정령이 자랐어요!") : Loc.T("Your spirit changed its colors!", "정령의 빛깔이 바뀌었어요!");
            if (SpiritDB.Stage(st) >= 1) Album.Report("spirit_young");
            HUD.Announce($"<size=40>{grew}</size>\n\n{SpiritDB.FormName(formAfter)}" + (isNew ? "  <color=#9dff9d>" + Loc.T("(new form!)", "(새 모습!)") + "</color>" : "") +
                         $"\n<size=24>{SpiritDB.PerkText(st)}</size>\n\n<size=22>" + Loc.T("Forms found", "찾은 모습") + $": {st.formsSeen.Count} / {SpiritDB.AllForms.Length}</size>", 5f);
            if (isNew) Data.ChangeHeart(2f, Loc.T("Your spirit is glowing with color.", "정령이 빛깔로 반짝여요."));
        }
    }
}
