using UnityEngine;

// 아직 열리지 않은 길 너머에 낀 잿빛 안개 (방향 C, 맵 5단계). 천천히 흘러가고,
// 그 길이 열린 뒤 처음 가까이 가면 위로 흩어지며 걷힙니다 (한 번 걷히면 story에 "fog_lifted_<지역>"이 남음).
// 그림: World/fog_bank, World/fog_bank_2 (Tools/pixelart/villagemap.py)
public class FogBank : MonoBehaviour
{
    const float LiftDistance = 9f;     // 이만큼 가까이 오면 걷히기 시작
    const float LiftSeconds = 3.5f;

    private SpriteRenderer[] parts;
    private Vector3[] basePos;
    private bool lift;
    private string unlockKey;
    private float liftT = -1f;
    private Transform player;

    public static string LiftKey(string region) => "fog_lifted_" + region;

    // area: 출구 자리, mapBounds: 농장 맵 경계 (안개가 맵 밖으로 나가지 않게)
    public static FogBank Create(string region, Rect area, Rect mapBounds, Material material, bool liftNow)
    {
        var go = new GameObject("Fog_" + region);
        FogBank fog = go.AddComponent<FogBank>();
        fog.lift = liftNow;
        fog.unlockKey = LiftKey(region);

        // 출구에서 맵 바깥쪽으로 조금 밀어서, 덤불/갈대 너머에 안개가 끼어 보이게
        Vector2 c = area.center;
        Vector2 outward = (c - mapBounds.center).normalized;
        Vector2 at = c + outward * 0.8f;
        Vector2[] offsets = { new Vector2(-1.3f, 0.4f), new Vector2(1.0f, -0.3f), new Vector2(0.1f, 1.1f) };
        fog.parts = new SpriteRenderer[offsets.Length];
        fog.basePos = new Vector3[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            Sprite sprite = PixelArt.Load(i % 2 == 0 ? "World/fog_bank" : "World/fog_bank_2");
            if (sprite == null) continue;
            var part = new GameObject("Puff" + i);
            part.transform.SetParent(go.transform, false);
            float scale = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            float w = sprite.rect.width / sprite.pixelsPerUnit * scale * 0.5f, h = sprite.rect.height / sprite.pixelsPerUnit * scale * 0.5f;
            Vector2 p = at + offsets[i];
            p.x = Mathf.Clamp(p.x, mapBounds.xMin + w - 0.5f, mapBounds.xMax - w + 0.5f);
            p.y = Mathf.Clamp(p.y, mapBounds.yMin + h - 0.5f, mapBounds.yMax - h + 0.5f);
            part.transform.position = new Vector3(p.x, p.y, 0f);
            part.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = part.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 7;               // 덤불(4)과 사람 위에
            fog.parts[i] = sr;
            fog.basePos[i] = part.transform.position;
        }
        return fog;
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null) continue;
            // 천천히 좌우로 흐름 (픽셀 단위로 끊어서 번지지 않게)
            float dx = Mathf.Round(Mathf.Sin(t * 0.25f + i * 1.9f) * 0.35f * 32f) / 32f;
            float dy = liftT >= 0f ? liftT / LiftSeconds * (1.2f + i * 0.4f) : 0f;
            parts[i].transform.position = basePos[i] + new Vector3(dx, dy, 0f);
        }

        if (!lift) return;
        if (liftT < 0f)
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            if (transform.childCount == 0 || Vector2.Distance(player.position, transform.GetChild(0).position) > LiftDistance) return;
            liftT = 0f;
            DataManager data = DataManager.instance;
            if (data != null) data.story.Unlock(unlockKey);    // 한 번 걷히면 다시는 안 낌
        }
        liftT += Time.deltaTime;
        float a = 1f - Mathf.Clamp01(liftT / LiftSeconds);
        a = Mathf.Round(a * 4f) / 4f;                           // 네 단계로 옅어짐 (부드러운 그라데이션 대신)
        foreach (SpriteRenderer sr in parts)
            if (sr != null) sr.color = new Color(1f, 1f, 1f, a);
        if (liftT >= LiftSeconds) Destroy(gameObject);
    }
}
