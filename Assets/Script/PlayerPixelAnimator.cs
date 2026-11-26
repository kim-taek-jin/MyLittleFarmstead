using UnityEngine;

// 플레이어 픽셀 아트 걷기 애니메이션 (아래/위/옆 × 3프레임)과 행동 동작 (D-2: 괭이질, 물 주기, 수확 × 2프레임).
// PixelArtWorld가 플레이어에 자동으로 붙여 줍니다. 원래 스프라이트 크기에 맞춰 그립니다.
public class PlayerPixelAnimator : MonoBehaviour
{
    public float framesPerSecond = 8f;

    private SpriteRenderer view;
    private Rigidbody2D rb;
    private Sprite[] down, up, side;
    private Sprite[] current;
    private bool facingLeft;
    private float timer;
    private int step;

    // 걷기: 1 → 0 → 2 → 0
    private static readonly int[] WalkCycle = { 1, 0, 2, 0 };

    // ---- 행동 동작: 두 프레임 (준비 → 내리침/붓기/뽑기). 그동안은 걷지 않음
    public static PlayerPixelAnimator Instance { get; private set; }
    public static bool Busy => Instance != null && Instance.actionT > 0f;
    const float ActionFirst = 0.11f, ActionSecond = 0.2f;
    private float actionT;
    private Sprite actionA, actionB;

    void Awake() { Instance = this; }

    // 마실 (K-3): 평상에 앉은 모습. 끌 때까지 그대로
    private bool sitting;
    public static void Sit(bool on) { if (Instance != null) Instance.sitting = on; }

    // action: "hoe", "water", "harvest". target: 동작을 하는 곳 (그쪽을 바라봄)
    public static void PlayAction(string action, Vector2 target)
    {
        if (Instance == null || Instance.view == null) return;
        Instance.Face(target);
        string dir = Instance.current == Instance.side ? "side" : Instance.current == Instance.up ? "up" : "down";
        Sprite a = PixelArt.Load($"Player/player_{action}_{dir}_0"), b = PixelArt.Load($"Player/player_{action}_{dir}_1");
        if (a == null || b == null) return;
        Instance.actionA = a; Instance.actionB = b;
        Instance.actionT = ActionFirst + ActionSecond;
    }

    void Face(Vector2 target)
    {
        Vector2 d = target - (Vector2)transform.position;
        if (d.sqrMagnitude < 0.04f) return;                // 발밑이면 지금 방향 그대로
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { current = side; facingLeft = d.x < 0f; }
        else current = d.y > 0f ? up : down;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        down = LoadSet("down");
        up = LoadSet("up");
        side = LoadSet("side");
        if (down == null || up == null || side == null)
        {
            enabled = false;
            return;
        }

        SpriteRenderer original = GetComponent<SpriteRenderer>();
        view = PixelArt.Overlay(original, down[0], native: true);   // 배경과 같은 픽셀 크기 (밭 한 칸 높이)
        if (view == null)
        {
            enabled = false;
            return;
        }
        current = down;
        // 주민도 플레이어와 같은 크기로 그리도록 기록
        PixelArt.CharacterScale = Mathf.Abs(view.transform.lossyScale.y);
    }

    static Sprite[] LoadSet(string direction)
    {
        var set = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            set[i] = PixelArt.Load($"Player/player_{direction}_{i}");
            if (set[i] == null) return null;
        }
        return set;
    }

    void Update()
    {
        if (view == null) return;

        if (sitting)
        {
            Sprite sit = PixelArt.Load("Player/player_sit");
            if (sit != null) { view.sprite = sit; view.flipX = false; return; }
        }

        if (actionT > 0f)
        {
            actionT -= Time.deltaTime;
            view.sprite = actionT > ActionSecond ? actionA : actionB;
            view.flipX = current == side && facingLeft;
            return;
        }

        Vector2 v = rb != null ? rb.linearVelocity : Vector2.zero;
        bool moving = v.sqrMagnitude > 0.01f;

        if (moving)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            {
                current = side;
                facingLeft = v.x < 0f;
            }
            else
            {
                current = v.y > 0f ? up : down;
            }

            timer += Time.deltaTime;
            if (timer >= 1f / framesPerSecond)
            {
                timer = 0f;
                step = (step + 1) % WalkCycle.Length;
            }
            view.sprite = current[WalkCycle[step]];
        }
        else
        {
            step = 0;
            timer = 0f;
            view.sprite = current[0];
        }
        view.flipX = current == side && facingLeft;
    }
}
