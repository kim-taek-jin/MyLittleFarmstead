using UnityEngine;

// 아직 열리지 않은 길 (숲길, 옛 기차역). 회색 가시덤불이 막고 있고, 부딪히면 표지판 말을 보여 줍니다.
// 맵 데이터의 'locked'는 이 길을 열 마음의 계절입니다. 길 너머 지역은 다음 단계에서 만듭니다.
public class LockedExit : MonoBehaviour
{
    public MapExit exit;
    private float nextMessage;

    public static LockedExit Create(MapExit exit, Rect mapBounds, Material material)
    {
        Rect area = exit.Area;
        var go = new GameObject("LockedExit_" + exit.to);
        go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
        LockedExit locked = go.AddComponent<LockedExit>();
        locked.exit = exit;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(Mathf.Max(0.6f, area.width), Mathf.Max(0.6f, area.height));

        Sprite sprite = PixelArtWorld.SeasonalSprite("thorns");
        if (sprite != null)
        {
            var art = new GameObject("PixelArt");
            art.transform.SetParent(go.transform, false);
            float scale = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            float w = sprite.rect.width / sprite.pixelsPerUnit * scale, h = sprite.rect.height / sprite.pixelsPerUnit * scale;
            // 덤불이 맵 밖으로 잘리지 않게 안쪽으로 당김
            float x = Mathf.Clamp(area.center.x, mapBounds.xMin + w * 0.5f - 0.3f, mapBounds.xMax - w * 0.5f + 0.3f);
            float y = Mathf.Clamp(area.center.y, mapBounds.yMin + h * 0.5f, mapBounds.yMax - h * 0.5f);
            art.transform.position = new Vector3(x, y, 0f);
            art.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 4;
            art.AddComponent<SeasonalArt>().baseKey = "thorns";
        }
        return locked;
    }

    public bool SeasonReached
    {
        get
        {
            DataManager data = DataManager.instance;
            if (data == null || !System.Enum.TryParse(exit.locked, out Season needed)) return false;
            return data.CurrentSeason >= needed;
        }
    }

    public string Message => SeasonReached
        ? "The gray thorns have started to loosen... Not yet, but soon."
        : exit.sign;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player") || Time.time < nextMessage) return;
        nextMessage = Time.time + 4f;
        HUD.Toast(Message, 3.5f);
    }
}
