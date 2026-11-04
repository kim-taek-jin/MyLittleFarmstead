using UnityEngine;

// 시작 인트로의 도시 그림(City_0)처럼 '화면을 꽉 채워야 하는' 월드 그림.
// 원래는 화면이 1920×1080이고 카메라가 (0,0)일 때만 꽉 찼습니다. 이제 매 프레임 카메라 위치로 옮기고,
// 그림 비율을 유지한 채 화면을 빈틈없이 덮는 크기로 맞춥니다. (인트로 패널이 꺼지면 함께 꺼짐)
public class CoverCamera : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        SpriteRenderer sr = null;
        Transform art = transform.Find("PixelArt");             // 픽셀 아트로 바꿔 그린 그림이 있으면 그것 기준
        if (art != null) sr = art.GetComponent<SpriteRenderer>();
        if (sr == null || !sr.enabled) sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x, c.y, transform.position.z);

        Vector2 size = sr.bounds.size;
        if (size.x < 0.01f || size.y < 0.01f) return;
        float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
        float k = Mathf.Max(viewW / size.x, viewH / size.y) * 1.02f;   // 가장자리 틈이 없도록 살짝 더 크게
        if (Mathf.Abs(k - 1f) > 0.002f)
            transform.localScale = new Vector3(transform.localScale.x * k, transform.localScale.y * k, transform.localScale.z);
    }
}
