using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // public으로 두면 Unity 에디터에서 값을 조절할 수 있습니다.
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb == null) return;

        // 게임 오버 상태에서는 움직이지 않음
        if ((DataManager.instance != null && DataManager.instance.IsGameOver) || PauseMenu.Open || HUD.AlbumOpen || PixelHUD.BagOpen || PixelHUD.ViewOpen
            || PlayerPixelAnimator.Busy)                     // 괭이질, 물 주기, 수확 동작 중에는 잠깐 멈춤
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // WASD 또는 방향키 입력 (-1, 0, 1)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;

        // 배고픔/갈증이 바닥나면 몸이 무거워집니다.
        float speed = moveSpeed;
        DataManager data = DataManager.instance;
        if (data != null && (data.hunger <= 0f || data.thirst <= 0f)) speed *= 0.6f;

        rb.linearVelocity = moveDirection * speed;

        // 발소리: 걷는 동안 일정한 간격으로
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f) { Sfx.Play("step", 0.5f, 0.1f, 0.15f); stepTimer = 1.6f / speed; }
        }
        else stepTimer = 0f;
    }
}
