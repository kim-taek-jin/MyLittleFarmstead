using UnityEngine;

// 플레이어 픽셀 아트 걷기 애니메이션 (아래/위/옆 × 3프레임).
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
        view = PixelArt.Overlay(original, down[0], 0.9f);
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
