using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // public�� ���̸� Unity �����Ϳ��� ���� ������ �� �ֽ��ϴ�.
    public float moveSpeed = 5f; // �ʴ� 5��ŭ�� �ӵ��� ������

    // ������ �������� ���� ������Ʈ ����
    private Rigidbody2D rb;

    // ������ ���۵� �� �ѹ��� ȣ��Ǵ� �Լ�
    void Start()
    {
        // Player ������Ʈ�� �پ��ִ� Rigidbody2D ������Ʈ�� �����ͼ� rb ������ �Ҵ�
        rb = GetComponent<Rigidbody2D>();
    }

    // �� �����Ӹ��� ȣ��Ǵ� �Լ�
    void Update()
    {
        // Ű���� �Է��� �޽��ϴ�. GetAxisRaw�� -1, 0, 1 ���� ��ȯ�մϴ�.
        float moveX = Input.GetAxisRaw("Horizontal"); // A, D �Ǵ� �¿� ȭ��ǥ Ű
        float moveY = Input.GetAxisRaw("Vertical");   // W, S �Ǵ� ���� ȭ��ǥ Ű

        // ������ ������ ��Ÿ���� ���� ����
        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;

        // Rigidbody2D�� �̿��� �÷��̾ ������ �����Դϴ�.
        // Time.deltaTime�� �����ִ� ������ ������ �ӵ��� ������� ������ �ӵ��� �����ϱ� �����Դϴ�.
        rb.linearVelocity = moveDirection * moveSpeed;
    }
}