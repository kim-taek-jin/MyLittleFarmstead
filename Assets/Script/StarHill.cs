using UnityEngine;
using System.Collections.Generic;

// 별의 언덕. 나눔 점수 30이 되면 열리고, 맑은 밤(20시 이후)에 농장에 빛나는 덩굴이 자랍니다.
// 덩굴(E)을 타고 올라가면 언덕에서 떨어지는 별 조각을 줍고, 벤치(E)에서 별을 볼 수 있습니다.
// Farm 씬 안의 멀리 떨어진 곳(y +100)에 코드로 만들어지며, 카메라를 옮겨서 보여줍니다.
public class StarHill : MonoBehaviour
{
    public static bool Active { get; private set; }
    public const int KindnessToUnlock = 60;   // 나눔 상자 하루 최대 5점 + 주민 소원 3점씩 → 보통 15~20일째
    public const int StarsPerNight = 10;

    static readonly Vector2 LadderPos = new Vector2(8.25f, 2.9f);
    static readonly Vector2 HillCenter = new Vector2(0f, 100f);
    static readonly Vector2 HillArrive = new Vector2(0f, 96.9f);
    static readonly Vector2 VineDown = new Vector2(0f, 95.6f);
    static readonly Vector2 Bench = new Vector2(5.1f, 98.0f);
    static readonly Rect Meadow = new Rect(-8.4f, 95.4f, 16.8f, 3.4f);   // 플레이어가 걸을 수 있는 언덕 영역

    private SpriteRenderer ladder;
    private Material material;
    private float scale;
    private readonly List<Transform> stars = new List<Transform>();
    private readonly List<float> starFall = new List<float>();
    private float spawnTimer = 1.5f;
    private Vector3 cameraHome;

    static DataManager Data => DataManager.instance;

    public static bool IsUnlocked => Data != null && Data.story.IsUnlocked("starhill");

    public static bool IsOpenNow
    {
        get
        {
            if (!IsUnlocked || Data.TimeManager == null) return false;
            return Data.TimeManager.hour >= 20 && !Data.raining;
        }
    }

    public static StarHill Create(Material material, float characterScale)
    {
        GameObject root = new GameObject("StarHill");
        StarHill hill = root.AddComponent<StarHill>();
        hill.material = material;
        hill.scale = characterScale;

        // 농장의 덩굴 사다리
        GameObject l = new GameObject("StarVine");
        l.transform.SetParent(root.transform, false);
        l.transform.position = LadderPos;
        l.transform.localScale = Vector3.one * characterScale;
        hill.ladder = l.AddComponent<SpriteRenderer>();
        hill.ladder.sprite = PixelArt.Load("World/star_ladder");
        if (material != null) hill.ladder.sharedMaterial = material;
        hill.ladder.sortingOrder = 9;

        // 언덕 배경 (32px = 1유닛, 농장 배경과 같은 크기)
        GameObject bg = new GameObject("StarHillBackground");
        bg.transform.SetParent(root.transform, false);
        bg.transform.position = new Vector3(HillCenter.x, HillCenter.y, 5f);
        SpriteRenderer bsr = bg.AddComponent<SpriteRenderer>();
        bsr.sprite = PixelArt.Load("World/bg_starhill");
        bsr.sortingOrder = -100;
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (unlit != null) bsr.sharedMaterial = new Material(unlit);
        else if (material != null) bsr.sharedMaterial = material;
        return hill;
    }

    void Update()
    {
        if (Data == null) return;
        PlayerAction player = PlayerAction.instance;

        bool open = IsOpenNow;
        if (ladder != null) ladder.enabled = open;

        if (!Active)
        {
            if (open && player != null && Near(player.transform.position, LadderPos, 1.3f) && PressedE())
                Enter(player);
            return;
        }

        // ---- on the hill ----
        if (player == null) return;
        Vector3 p = player.transform.position;
        p.x = Mathf.Clamp(p.x, Meadow.xMin, Meadow.xMax);
        p.y = Mathf.Clamp(p.y, Meadow.yMin, Meadow.yMax);
        player.transform.position = p;

        UpdateStars(player);

        if (PressedE())
        {
            if (Near(p, VineDown, 1.4f)) Leave(player);
            else if (Near(p, Bench, 1.4f)) SitOnBench();
        }
    }

    static bool Near(Vector3 a, Vector2 b, float r) => ((Vector2)a - b).sqrMagnitude <= r * r;

    static bool PressedE() =>
        Input.GetKeyDown(KeyCode.E) && !HUD.DialogueOpen && Time.frameCount - HUD.DialogueClosedFrame > 1 && !Data.IsGameOver;

    void Enter(PlayerAction player)
    {
        Active = true;
        Camera cam = Camera.main;
        if (cam != null)
        {
            cameraHome = cam.transform.position;
            cam.transform.position = new Vector3(HillCenter.x, HillCenter.y, cameraHome.z);
        }
        player.transform.position = new Vector3(HillArrive.x, HillArrive.y, player.transform.position.z);
        if (Data.story.starsDay != Data.TimeManager.day)
        {
            Data.story.starsDay = Data.TimeManager.day;
            Data.story.starsTonight = 0;
        }
        HUD.Toast("You climb the glowing vine... the whole sky opens up.", 3f);
    }

    void Leave(PlayerAction player)
    {
        Active = false;
        Camera cam = Camera.main;
        if (cam != null) cam.transform.position = cameraHome;
        player.transform.position = new Vector3(LadderPos.x - 0.6f, LadderPos.y - 1.0f, player.transform.position.z);
        foreach (Transform s in stars) if (s != null) Destroy(s.gameObject);
        stars.Clear();
        starFall.Clear();
    }

    void SitOnBench()
    {
        if (Data.story.benchDay == Data.TimeManager.day)
        {
            HUD.Toast("You already watched the stars tonight. It was lovely.", 2f);
            return;
        }
        Data.story.benchDay = Data.TimeManager.day;
        Data.ChangeHeart(3f, "You sat on the bench and watched the stars.");
    }

    void UpdateStars(PlayerAction player)
    {
        // 떨어지는 중인 별 → 땅에 닿으면 반짝
        for (int i = stars.Count - 1; i >= 0; i--)
        {
            Transform s = stars[i];
            if (s == null) { stars.RemoveAt(i); starFall.RemoveAt(i); continue; }
            if (starFall[i] > 0f)
            {
                starFall[i] = Mathf.Max(0f, starFall[i] - Time.deltaTime * 6f);
                s.localPosition = new Vector3(s.localPosition.x, s.localPosition.y - Time.deltaTime * 6f, 0f);
            }
            float tw = 1f + Mathf.Sin(Time.time * 6f + i) * 0.12f;
            s.localScale = Vector3.one * scale * 0.6f * tw;

            if (starFall[i] <= 0f && ((Vector2)player.transform.position - (Vector2)s.position).sqrMagnitude < 0.5f)
            {
                Destroy(s.gameObject);
                stars.RemoveAt(i);
                starFall.RemoveAt(i);
                Data.AddItem("Star Fragment", 1);
                Data.story.starsTonight++;
                Data.story.starsTotal++;
                int left = StarsPerNight - Data.story.starsTonight;
                HUD.Toast(left > 0 ? $"You caught a Star Fragment! ({Data.story.starsTonight}/{StarsPerNight} tonight)" : "That was the last falling star tonight.", 2f);
            }
        }

        if (Data.story.starsTonight + stars.Count >= StarsPerNight || stars.Count >= 3) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = Random.Range(2f, 4f);

        float x = Random.Range(Meadow.xMin + 0.5f, Meadow.xMax - 0.5f);
        float y = Random.Range(Meadow.yMin + 0.3f, Meadow.yMax - 0.3f);
        float drop = 4f;
        GameObject star = new GameObject("FallingStar");
        star.transform.SetParent(transform, false);
        star.transform.localPosition = new Vector3(x, y + drop, 0f);
        SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.Load("Items/star_fragment");
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 12;
        stars.Add(star.transform);
        starFall.Add(drop);
    }

    void OnDestroy()
    {
        Active = false;   // 씬이 바뀌면(기절 등) 언덕 상태도 끝
    }
}
