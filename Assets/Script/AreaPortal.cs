using UnityEngine;

// 같은 씬 안의 다른 지역으로 걸어 들어가는 입구 (농장의 꽃 아치 ↔ 숲 입구).
// 씬을 다시 불러오지 않고 플레이어만 옮깁니다. 카메라는 CameraFollow가 새 지역으로 바로 맞춥니다.
public class AreaPortal : MonoBehaviour
{
    public Vector2 destination;
    public string arriveMessage;

    public static AreaPortal Create(string name, Rect area, Vector2 destination, string arriveMessage = null)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(Mathf.Max(0.4f, area.width), Mathf.Max(0.4f, area.height));
        AreaPortal portal = go.AddComponent<AreaPortal>();
        portal.destination = destination;
        portal.arriveMessage = arriveMessage;
        return portal;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (DataManager.instance != null && DataManager.instance.IsGameOver) return;
        Rigidbody2D body = other.attachedRigidbody;
        Transform player = body != null ? body.transform : other.transform;
        player.position = new Vector3(destination.x, destination.y, player.position.z);
        if (body != null)
        {
            body.position = destination;
            body.linearVelocity = Vector2.zero;
        }
        if (!string.IsNullOrEmpty(arriveMessage)) HUD.Toast(arriveMessage, 3f);
    }
}
