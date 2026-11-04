using UnityEngine;

// 같은 씬 안에 실행 중에 만드는 지역(숲, 호수)이 함께 쓰는 도구.
// 모든 그림은 배경과 같은 픽셀 크기(32px = 1유닛)로 그립니다.
public static class AreaKit
{
    // 지역 바닥 그림 (계절 그림 자동 교체). 마스크 안에서만 보이게 할 수도 있음
    public static SpriteRenderer Ground(Transform parent, string key, Vector2 center, int order, SpriteMaskInteraction maskMode, Material material)
    {
        Sprite sprite = PixelArtWorld.SeasonalSprite(key);
        var go = new GameObject(key);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = order;
        sr.maskInteraction = maskMode;
        if (sprite != null)
        {
            float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            go.transform.localScale = new Vector3(s, s, 1f);
        }
        go.AddComponent<SeasonalArt>().baseKey = key;
        return sr;
    }

    // 지나갈 수 없는 칸들을 가로로 이어진 상자 콜라이더로
    public static void Walls(Transform parent, MapData map, System.Func<char, bool> blocks)
    {
        var walls = new GameObject("Walls");
        walls.transform.SetParent(parent, false);
        for (int r = 0; r < map.rows.Length; r++)
        {
            string row = map.rows[r];
            int c = 0;
            while (c < row.Length)
            {
                if (!blocks(row[c])) { c++; continue; }
                int start = c;
                while (c < row.Length && blocks(row[c])) c++;
                Vector2Int left = map.CellOf(start, r);
                BoxCollider2D box = walls.AddComponent<BoxCollider2D>();
                box.size = new Vector2(c - start, 1f);
                box.offset = new Vector2(left.x + (c - start) * 0.5f, left.y + 0.5f);
            }
        }
    }

    // 밑동(bottom)을 맞춘 스프라이트
    public static SpriteRenderer SpriteAt(Transform parent, string name, string key, Vector2 bottom, int order, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = order;
        go.transform.position = new Vector3(bottom.x, bottom.y, 0f);
        if (key != null) SetSprite(sr, key, bottom);
        return sr;
    }

    public static void SetSprite(SpriteRenderer sr, string key, Vector2 bottom)
    {
        Sprite sprite = PixelArt.Load("World/" + key);
        sr.sprite = sprite;
        if (sprite == null) return;
        float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
        sr.transform.localScale = new Vector3(s, s, 1f);
        sr.transform.position = new Vector3(bottom.x, bottom.y + sprite.rect.height / sprite.pixelsPerUnit * s * 0.5f, 0f);
    }

    // 둥근 마스크 (이 안에서만 컬러 바닥이 보임)
    public static GameObject Mask(Transform parent, string key, Vector2 at)
    {
        var m = new GameObject("ColorMask");
        m.transform.SetParent(parent, false);
        m.transform.position = new Vector3(at.x, at.y, 0f);
        Sprite sprite = PixelArt.Load("World/" + key);
        SpriteMask mask = m.AddComponent<SpriteMask>();
        mask.sprite = sprite;
        if (sprite != null)
        {
            float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            m.transform.localScale = new Vector3(s, s, 1f);
        }
        return m;
    }

    // 채집물 하나 ("x,y,아이템"). area는 어느 지역의 목록에서 지울지 ("forest" / "lake")
    public static void SpawnForage(Transform parent, string entry, Material material, string area)
    {
        string[] parts = entry.Split(',');
        if (parts.Length < 3 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) return;
        string item = parts[2];
        var go = new GameObject("Forage_" + item);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x + 0.5f, y + 0.4f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = PixelArt.Load("World/" + PixelArt.Key(item) + "_ground");
        sr.sprite = sprite;
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 3;
        if (sprite != null)
        {
            float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            go.transform.localScale = new Vector3(s, s, 1f);
        }
        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.35f / Mathf.Max(0.01f, go.transform.localScale.x);
        ForagePickup pickup = go.AddComponent<ForagePickup>();
        pickup.cell = new Vector2Int(x, y);
        pickup.area = area;
    }

    public static int Today => GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;

    public static bool PressedE() =>
        Input.GetKeyDown(KeyCode.E) && !HUD.DialogueOpen && Time.frameCount - HUD.DialogueClosedFrame > 1;
}

// 지역 바닥의 채집물. 걸어서 닿으면 줍습니다.
public class ForagePickup : MonoBehaviour
{
    public Vector2Int cell;
    public string area = "forest";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        DataManager data = DataManager.instance;
        if (data == null) return;
        string item = area == "lake" ? data.story.lake.TakeForage(cell.x, cell.y) : data.story.forest.TakeForage(cell.x, cell.y);
        if (item != null)
        {
            data.AddItem(item);
            HUD.Toast(item == "Acorn" ? "You found an Acorn! Plant it in a clearing." : $"You found {item}.", 2.5f);
        }
        Destroy(gameObject);
    }
}
