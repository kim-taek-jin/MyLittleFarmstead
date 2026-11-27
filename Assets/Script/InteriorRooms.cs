using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// H-2: 가게와 마을회관의 안 (방앗간, 행복슈퍼, 마을회관).
// 방은 장터 장면 안, 장터 오른쪽 멀리(x 40, 52, 64)에 놓은 그림입니다 (새 장면을 만들지 않음 → 빌드 설정 손댈 것 없음).
//  - 배치 자료: Resources/Maps/rooms.json (Tools/pixelart/rooms.py가 그림과 같이 만듦, 웹 미리보기도 같은 파일)
//  - 들어가기: 장터의 문 앞에서 [E]. 나오기: 방의 문(앞벽 오른쪽)으로 걸어 나감
//  - 주인공이 어느 방 안에 서 있는지는 매 프레임 위치로 판단 (저장 후 다시 열어도 그대로 맞음)
//  - 방 안에 있으면 카메라가 그 방에 맞춰 당겨지고(CameraFit이 부름), 눈·비가 안 보임(WeatherFX)
[System.Serializable] public class RoomRect { public int x0, y0, x1, y1; }
[System.Serializable] public class RoomPoint { public string name; public float x, y; }

[System.Serializable]
public class RoomData
{
    public string id, nameEn, nameKo;
    public int w, h, doorX0, doorX1, wallH, frontH;
    public float originX, originY, entranceX, entranceY, exitX, exitY;
    public RoomRect[] furniture;
    public RoomPoint[] anchors;

    // 그림 픽셀 → 세계 좌표 (32 px = 1칸)
    public Vector2 World(float px, float py) => new Vector2(originX + (px - w * 0.5f) / 32f, originY + (h * 0.5f - py) / 32f);
    public Rect WorldRect => new Rect(originX - w / 64f, originY - h / 64f, w / 32f, h / 32f);
    public string Name => Loc.T(nameEn, nameKo);

    public bool TryAnchor(string name, out Vector2 at)
    {
        if (anchors != null)
            foreach (RoomPoint p in anchors)
                if (p.name == name) { at = World(p.x, p.y); return true; }
        at = Vector2.zero;
        return false;
    }
}

#pragma warning disable 0649   // JsonUtility가 채움
[System.Serializable] class RoomFile { public RoomData[] rooms; }
#pragma warning restore 0649

public static class InteriorRooms
{
    public const string SceneName = "Market";
    static RoomData[] rooms;

    public static RoomData[] All
    {
        get
        {
            if (rooms != null) return rooms;
            TextAsset text = Resources.Load<TextAsset>("Maps/rooms");
            rooms = text != null ? (JsonUtility.FromJson<RoomFile>(text.text)?.rooms ?? new RoomData[0]) : new RoomData[0];
            return rooms;
        }
    }

    public static RoomData Get(string id) { foreach (RoomData r in All) if (r.id == id) return r; return null; }

    public static Vector2 Anchor(string room, string name, Vector2 fallback)
    {
        RoomData r = Get(room);
        return r != null && r.TryAnchor(name, out Vector2 at) ? at : fallback;
    }

    // 주인공이 지금 서 있는 방 (없으면 null)
    public static RoomData Current
    {
        get
        {
            if (SceneManager.GetActiveScene().name != SceneName) return null;
            PlayerAction p = PlayerAction.instance;
            if (p == null) return null;
            Vector2 pos = p.transform.position;
            foreach (RoomData r in All) if (r.WorldRect.Contains(pos)) return r;
            return null;
        }
    }
    public static bool Inside => Current != null;

    // ---------------------------------------------------------------- 사람과 물건의 자리 (방 안으로 옮긴 것)
    public static Vector2 DayPosition(string villagerId, Vector2 fallback) =>
        villagerId == "rosa" ? Anchor("mill", "keeper", fallback) :                          // 복순 할머니: 낮엔 방앗간 안
        villagerId == "myeong" ? Anchor("store", "keeper", fallback) : fallback;             // 명숙 아주머니: 행복슈퍼 계산대 (F-1)

    public static Vector2 EveningPosition(string villagerId, Vector2 fallback) =>
        villagerId == "mayor" ? Anchor("hall", "mayorSeat", fallback) : fallback;            // 이장님: 저녁엔 회관 난롯가

    // CameraFit이 방 안에서 부름: 카메라를 방에 맞춤
    public static void FrameCamera(Camera cam, RoomData r)
    {
        Rect rect = r.WorldRect;
        float halfH = Mathf.Min(rect.height * 0.5f, rect.width * 0.5f / Mathf.Max(0.01f, cam.aspect));
        cam.orthographicSize = halfH;
        cam.transform.position = new Vector3(rect.center.x, rect.center.y, cam.transform.position.z);
    }

    // ---------------------------------------------------------------- 장면을 열 때
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Build(scene);
        Build(SceneManager.GetActiveScene());
    }

    static void Build(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName || !PixelArt.Enabled) return;
        if (GameObject.Find("InteriorRooms") != null) return;
        var root = new GameObject("InteriorRooms");
        SceneManager.MoveGameObjectToScene(root, scene);
        Material mat = FindMaterial();
        foreach (RoomData r in All) BuildRoom(root.transform, r, mat);
        MoveShop(scene);
    }

    static void BuildRoom(Transform parent, RoomData r, Material mat)
    {
        var go = new GameObject("Room_" + r.id);
        go.transform.SetParent(parent, false);
        // 그림: 원래 픽셀 크기로, 방 가운데에
        Sprite sprite = PixelArt.Load("World/room_" + r.id);
        if (sprite != null)
        {
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = -50;
            float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            art.transform.localScale = new Vector3(s, s, 1f);
            art.transform.position = new Vector3(r.originX, r.originY, 0f);
        }
        // 벽: 뒷벽, 양옆, 앞벽 (문만 열림), 가구 발밑
        Vector2 a = r.World(6, r.wallH), b = r.World(r.w - 6, r.h - r.frontH);
        float left = a.x, back = a.y, right = b.x, front = b.y;
        float d0 = r.World(r.doorX0, 0).x, d1 = r.World(r.doorX1 + 1, 0).x;
        Box(go, left - 1f, back, right + 1f, back + 3f, false);
        Box(go, left - 3f, front - 3f, left, back + 3f, false);
        Box(go, right, front - 3f, right + 3f, back + 3f, false);
        Box(go, left - 1f, front - 3f, d0, front, false);
        Box(go, d1, front - 3f, right + 1f, front, false);
        Box(go, d0 - 1f, front - 4f, d1 + 1f, front - 0.8f, false);
        if (r.furniture != null)
            foreach (RoomRect f in r.furniture)
            {
                Vector2 p0 = r.World(f.x0, f.y1), p1 = r.World(f.x1, f.y0);
                Box(go, p0.x, p0.y, p1.x, p1.y, false);
            }
        // 문: 걸어 나가면 장터의 그 가게 문 앞
        var door = new GameObject("Door");
        door.transform.SetParent(go.transform, false);
        door.transform.position = new Vector3((d0 + d1) * 0.5f, front - 0.3f, 0f);
        BoxCollider2D trig = door.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(d1 - d0 - 0.1f, 0.5f);
        door.AddComponent<RoomDoor>().room = r;
        // 장터 쪽 문 앞: [E]로 들어가기
        var entrance = new GameObject("Entrance_" + r.id);
        entrance.transform.SetParent(parent, false);
        entrance.transform.position = new Vector3(r.entranceX, r.entranceY, 0f);
        entrance.AddComponent<RoomEntrance>().room = r;
    }

    static void Box(GameObject go, float x0, float y0, float x1, float y1, bool trigger)
    {
        BoxCollider2D c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = trigger;
        c.offset = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f) - (Vector2)go.transform.position;
        c.size = new Vector2(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
    }

    // 장 보기는 이제 행복슈퍼 안에서: 아주머니(Shopkeeper)를 계산대 뒤로. 바깥 천막 노점은 막고 안내만
    static void MoveShop(Scene scene)
    {
        RoomData store = Get("store");
        if (store == null || !store.TryAnchor("keeper", out Vector2 keeper)) return;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name != "Shopkeeper") continue;
            go.transform.position = new Vector3(keeper.x, keeper.y, go.transform.position.z);
            if (go.GetComponent<HideArt>() == null) go.AddComponent<HideArt>();   // F-1: 계산대엔 명숙 아주머니(Villager)가 섬 (원래 그림은 숨김)
            Vector3 s = go.transform.lossyScale;
            float sx = Mathf.Max(0.01f, Mathf.Abs(s.x)), sy = Mathf.Max(0.01f, Mathf.Abs(s.y));
            foreach (BoxCollider2D c in go.GetComponents<BoxCollider2D>())
            {
                if (c.isTrigger) { c.size = new Vector2(2.6f / sx, 2.2f / sy); c.offset = new Vector2(0f, -1.0f / sy); }
                else { c.size = new Vector2(1.0f / sx, 0.4f / sy); c.offset = new Vector2(0f, -0.55f / sy); }
            }
        }
        var stall = new GameObject("StallFront");
        SceneManager.MoveGameObjectToScene(stall, scene);
        BoxCollider2D solid = stall.AddComponent<BoxCollider2D>();
        solid.offset = new Vector2(3.92f, 2.15f);
        solid.size = new Vector2(3.6f, 0.8f);
        var note = new GameObject("StallNote");
        SceneManager.MoveGameObjectToScene(note, scene);
        note.transform.position = new Vector3(3.92f, 1.4f, 0f);
        BoxCollider2D t = note.AddComponent<BoxCollider2D>();
        t.isTrigger = true;
        t.size = new Vector2(3.4f, 0.8f);
        note.AddComponent<StallNote>();
    }

    static Material FindMaterial()
    {
        if (PlayerAction.instance != null)
        {
            SpriteRenderer sr = PlayerAction.instance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sharedMaterial != null) return sr.sharedMaterial;
        }
        return null;
    }

    public static void Teleport(Vector2 to)
    {
        PlayerAction p = PlayerAction.instance;
        if (p == null) return;
        p.transform.position = new Vector3(to.x, to.y, p.transform.position.z);
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.position = to; rb.linearVelocity = Vector2.zero; }
    }
}

// 장터에서 가게 문 앞: [E]로 들어감
public class RoomEntrance : MonoBehaviour
{
    public RoomData room;
    bool near;
    const float Reach = 1.0f;

    void Update()
    {
        PlayerAction p = PlayerAction.instance;
        if (p == null || DataManager.instance == null || DataManager.instance.IsGameOver || TitleScreen.Blocking) return;
        bool now = Vector2.Distance(p.transform.position, transform.position) < Reach && !InteriorRooms.Inside;
        if (now && !near) HUD.Toast(Loc.T($"[E] Go into {room.Name}", $"[E] {room.Name} 안으로"), 2f);
        near = now;
        if (!near || Villager.InRange != null || !AreaKit.PressedE()) return;
        Vector2 arrive = room.TryAnchor("arrive", out Vector2 a) ? a : room.World(room.w * 0.5f, room.h * 0.5f);
        Sfx.Play(room.id == "store" ? "door_bell" : "door_slide", 0.8f);   // 가게 유리문 종 / 미닫이
        InteriorRooms.Teleport(arrive);
    }
}

// 방의 문: 걸어 나가면 장터의 그 가게 앞
public class RoomDoor : MonoBehaviour
{
    public RoomData room;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Sfx.Play(room.id == "store" ? "door_bell" : "door_slide", 0.8f);
        InteriorRooms.Teleport(new Vector2(room.exitX, room.exitY));
    }
}

// 바깥 천막 노점: 이제 물건은 가게 안에서
public class StallNote : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            HUD.Toast(Loc.T("The corner store's stall. Step inside the store to buy and sell.", "행복슈퍼 앞 노점. 사고팔기는 가게 안에서 해요."), 2.5f);
    }
}
