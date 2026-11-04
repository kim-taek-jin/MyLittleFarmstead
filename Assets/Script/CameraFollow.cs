using UnityEngine;

// 넓은 맵에서 카메라가 플레이어를 따라갑니다. 맵 밖은 보이지 않게, 플레이어는 맵 밖으로 못 나가게 막습니다.
// FarmMap이 농장 씬의 Main Camera에 붙여 줍니다. 별의 언덕 위에서는 잠시 멈춥니다.
public class CameraFollow : MonoBehaviour
{
    public Rect bounds;
    [Tooltip("클수록 빨리 따라감")]
    public float smoothing = 10f;

    private Camera cam;
    private readonly System.Collections.Generic.List<Rect> regions = new System.Collections.Generic.List<Rect>();
    private Transform target;
    private Rigidbody2D targetBody;
    private bool placed;

    public static CameraFollow Attach(Camera camera, Rect mapBounds)
    {
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.bounds = mapBounds;
        follow.regions.Clear();
        follow.regions.Add(mapBounds);
        follow.cam = camera;
        follow.placed = false;
        return follow;
    }

    // 같은 씬 안의 다른 지역 (예: 숲). 플레이어가 들어가 있는 지역을 따라 카메라 범위가 바뀝니다.
    public void AddRegion(Rect region)
    {
        if (!regions.Contains(region)) regions.Add(region);
    }

    void PickRegion()
    {
        Vector2 p = target.position;
        foreach (Rect r in regions)
        {
            if (p.x < r.xMin - 1f || p.x > r.xMax + 1f || p.y < r.yMin - 1f || p.y > r.yMax + 1f) continue;
            if (r != bounds)
            {
                bounds = r;
                placed = false;   // 다른 지역으로 이동: 카메라를 바로 옮김
            }
            return;
        }
    }

    void LateUpdate()
    {
        if (StarHill.Active)
        {
            placed = false;   // 내려오면 바로 플레이어 위치로
            return;
        }
        if (target == null)
        {
            if (PlayerAction.instance == null) return;
            target = PlayerAction.instance.transform;
            targetBody = target.GetComponent<Rigidbody2D>();
        }
        if (cam == null) cam = GetComponent<Camera>();

        PickRegion();
        KeepInside();

        Vector2 goal = ClampView(target.position);
        Vector3 pos = transform.position;
        Vector2 now = placed ? Vector2.Lerp(pos, goal, 1f - Mathf.Exp(-smoothing * Time.deltaTime)) : goal;
        placed = true;
        // 픽셀 격자에 맞춰 움직여서 그림이 떨리지 않게 (32px = 1유닛)
        now.x = Mathf.Round(now.x * PixelArt.PixelsPerUnit) / PixelArt.PixelsPerUnit;
        now.y = Mathf.Round(now.y * PixelArt.PixelsPerUnit) / PixelArt.PixelsPerUnit;
        transform.position = new Vector3(now.x, now.y, pos.z);
    }

    // 맵 가장자리 안쪽으로만 다닐 수 있게 (북쪽 문 앞만 조금 더 위로 갈 수 있음)
    void KeepInside()
    {
        Vector2 p = target.position;
        bool farmGate = regions.Count > 0 && bounds == regions[0] && Mathf.Abs(p.x) < 0.7f;   // 농장 북쪽 흥정 문 앞
        float top = farmGate ? 0.35f : 0.8f;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(p.x, bounds.xMin + 0.3f, bounds.xMax - 0.3f),
            Mathf.Clamp(p.y, bounds.yMin + 0.5f, bounds.yMax - top));
        if (clamped == p) return;
        if (targetBody != null) targetBody.position = clamped;
        target.position = new Vector3(clamped.x, clamped.y, target.position.z);
    }

    Vector2 ClampView(Vector2 p)
    {
        float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 5f;
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);
        float x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
        float y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
        return new Vector2(x, y);
    }
}
