using UnityEngine;

// 시장의 나눔 상자. 들고 있는 먹을거리(작물, 음식, 물고기)를 E로 1개, Shift+E로 전부 나눕니다.
// 나눔 점수가 30이 되면 '별의 언덕'이 열립니다.
public class DonationBox : MonoBehaviour
{
    private bool inRange;

    public static DonationBox Create(Vector2 position, Material material, float scale)
    {
        GameObject go = new GameObject("DonationBox");
        go.transform.position = position;
        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.0f;
        BoxCollider2D body = go.AddComponent<BoxCollider2D>();
        body.size = new Vector2(0.7f, 0.35f);
        body.offset = new Vector2(0f, -0.35f);

        GameObject art = new GameObject("Art");
        art.transform.SetParent(go.transform, false);
        art.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sprite = PixelArt.Load("World/donation_box");
        sr.sortingOrder = 9;
        return go.AddComponent<DonationBox>();
    }

    static bool IsShareable(string item)
    {
        ItemDef def = ItemDB.Get(item);
        return def != null && (def.category == ItemCategory.Crop || def.category == ItemCategory.Food || def.category == ItemCategory.Fish);
    }

    void Update()
    {
        DataManager data = DataManager.instance;
        if (!inRange || data == null || data.IsGameOver || HUD.DialogueOpen || Villager.InRange != null) return;
        if (!Input.GetKeyDown(KeyCode.E) || Time.frameCount - HUD.DialogueClosedFrame <= 1) return;

        string item = Hotbar.SelectedItem;
        if (string.IsNullOrEmpty(item) || !IsShareable(item))
        {
            HUD.Toast("Hold crops, food or fish to share them with the village.", 2.5f);
            return;
        }

        int room = data.story.ShareRoom(data.Today);
        if (room <= 0)
        {
            HUD.Toast("The sharing box is full for today. Come back tomorrow!", 2.5f);
            return;
        }

        // 하루 상한까지 필요한 만큼만 받습니다 (점수 없이 아이템만 사라지지 않도록)
        int perItem = Mathf.Max(1, ItemDB.Get(item).sellPrice / 10);
        bool all = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int needed = Mathf.CeilToInt(room / (float)perItem);
        int count = Mathf.Min(data.GetCount(item), all ? needed : 1);
        if (count <= 0) return;

        data.RemoveItem(item, count);
        int got = data.story.AddShare(data.Today, perItem * count);
        int left = data.story.ShareRoom(data.Today);
        HUD.Toast($"You shared {item} x{count}.  <color=#ff9fc4>Kindness +{got}</color>  ({data.story.kindness}/{StarHill.KindnessToUnlock})" +
                  (left <= 0 ? "  <color=#c8c0b0>That's plenty for today.</color>" : ""), 3f);
        data.AddKindness(got, fromShareBox: true);
        Album.Report("share");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
        HUD.Toast("Sharing box: hold food and press E to share (Shift+E: all).", 2.5f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inRange = false;
    }
}
