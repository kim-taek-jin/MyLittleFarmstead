using UnityEngine;

// 한 화면짜리 장면(집, 시장, 흥정)에서 Game 창이 배경 그림보다 넓거나 높으면 그림 밖이 보였습니다.
// 배경 그림 안쪽만 보이도록 카메라를 살짝 확대하고, 가운데를 배경 안에 맞춥니다.
// (농장처럼 카메라가 따라가는 장면은 CameraFollow가 맡으므로 건드리지 않습니다)
public class CameraFit : MonoBehaviour
{
    private Camera cam;
    private float baseSize;
    private Vector3 basePos;
    private Rect background;
    private bool found;
    private float nextSearch;

    public static void Attach(Camera camera)
    {
        if (camera == null || camera.GetComponent<CameraFit>() != null) return;
        CameraFit fit = camera.gameObject.AddComponent<CameraFit>();
        fit.cam = camera;
        fit.baseSize = camera.orthographicSize;
        fit.basePos = camera.transform.position;
    }

    void LateUpdate()
    {
        CameraFollow follow = GetComponent<CameraFollow>();
        if (follow != null && follow.enabled) return;   // 넓은 맵: CameraFollow 담당
        if (cam == null || !cam.orthographic) return;
        if (!found)
        {
            if (Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + 0.5f;
            found = FindBackground(out background);
            if (!found) return;
        }

        float halfH = Mathf.Min(baseSize, background.height * 0.5f, background.width * 0.5f / Mathf.Max(0.01f, cam.aspect));
        cam.orthographicSize = halfH;
        float halfW = halfH * cam.aspect;
        Vector3 p = basePos;
        p.x = Mathf.Clamp(p.x, background.xMin + halfW, background.xMax - halfW);
        p.y = Mathf.Clamp(p.y, background.yMin + halfH, background.yMax - halfH);
        transform.position = p;
    }

    // 픽셀 아트 배경(bg_로 시작하는 그림) 중 가장 큰 것
    static bool FindBackground(out Rect rect)
    {
        rect = default;
        float best = 0f;
        foreach (SeasonalArt art in FindObjectsByType<SeasonalArt>(FindObjectsSortMode.None))
        {
            if (art.baseKey == null || !art.baseKey.StartsWith("bg_")) continue;
            SpriteRenderer sr = art.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            Bounds b = sr.bounds;
            float area = b.size.x * b.size.y;
            if (area <= best) continue;
            best = area;
            rect = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }
        return best > 0f;
    }
}
