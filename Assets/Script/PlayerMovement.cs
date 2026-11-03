using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // public으로 두면 Unity 에디터에서 값을 조절할 수 있습니다.
    public float moveSpeed = 5f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb == null) return;

        // 게임 오버 상태에서는 움직이지 않음
        if (DataManager.instance != null && DataManager.instance.IsGameOver)
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
    }
}
