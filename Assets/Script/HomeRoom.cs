using UnityEngine;
using UnityEngine.SceneManagement;

// H-1: 집 안 = 온돌방 (그림: Tools/pixelart/home.py → World/bg_home).
// Home 장면을 열 때 코드로 맞춤 (장면 파일은 건드리지 않음):
//  - 벽: 그림의 방 둘레 (뒷벽, 양옆 기둥, 앞벽). 앞벽 가운데 미닫이문만 열려 있음
//  - 나가는 곳: 아래쪽 미닫이문 (예전에는 오른쪽 끝 빨간 깔개) → 농장 집 마루 앞
//  - 잠자리: 아랫목에 깔린 요 (예전 침대 그림은 숨김). 그 위에서 E
//  - 자개장, 이불 얹은 반닫이, 밥상은 발밑만 막음
public static class HomeRoom
{
    public const string SceneName = "Home";

    // 방 그림은 288 x 162 px (9 x 5.06칸), 주인공 키(32px ≈ 1.6m)에 맞춘 크기 (1m ≈ 20px).
    // 세계 (0, 0)에 원래 픽셀 크기로 놓음 → 카메라(CameraFit)가 방에 맞춰 가까이 당겨짐.
    // 그림의 픽셀 → 세계 좌표: ((px - 144) / 32, (81 - py) / 32)
    public static readonly Vector2 Arrival = new Vector2(2.77f, -1.4f);   // 농장에서 들어오면 문 안쪽
    public static readonly Vector2 WakeSpot = new Vector2(-2.9f, 0f);     // 지쳐 쓰러지면 요 옆에서 눈을 뜸
    static readonly Vector2 Door = new Vector2(2.77f, -2.25f);            // 앞벽 오른쪽 미닫이문 (가운데 단축칸에 가리지 않게)
    static readonly Vector2 BeddingCenter = new Vector2(-3.72f, 0.06f);

    const float Left = -4.31f, Right = 4.31f, Back = 1.34f, Front = -1.97f, DoorX0 = 2.375f, DoorX1 = 3.156f;

    // PixelArtWorld가 집 배경을 놓을 때: 늘이지 않고 원래 픽셀 크기로, 가운데를 (0, 0)에
    public static SpriteRenderer PlaceRoom(SpriteRenderer original, Sprite sprite)
    {
        SpriteRenderer view = PixelArt.Overlay(original, sprite, native: true);
        if (view == null) return null;
        view.transform.position = new Vector3(0f, 0f, original.transform.position.z);
        return view;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Build(scene);
        Build(SceneManager.GetActiveScene());
    }

    static void Build(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName || !PixelArt.Enabled) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            switch (root.name)
            {
                case "FarmPortal":
                    // 아래쪽 미닫이문 (농장 쪽 도착 자리는 FarmMap이 맵 데이터대로 맞춤)
                    root.transform.position = new Vector3(Door.x, Door.y, root.transform.position.z);
                    root.transform.localScale = Vector3.one;
                    BoxCollider2D door = root.GetComponent<BoxCollider2D>();
                    if (door != null) { door.size = new Vector2(DoorX1 - DoorX0 - 0.1f, 0.6f); door.offset = Vector2.zero; }
                    HideAll(root);
                    break;
                case "Exit_0":
                    root.SetActive(false);                    // 예전 오른쪽 출구 표지판
                    break;
                case "Bed":
                    root.transform.position = new Vector3(BeddingCenter.x, BeddingCenter.y, root.transform.position.z);
                    root.transform.localScale = Vector3.one;
                    BoxCollider2D bed = root.GetComponent<BoxCollider2D>();
                    if (bed != null) { bed.size = new Vector2(1.3f, 1.7f); bed.offset = Vector2.zero; }
                    HideAll(root);                            // 요와 이불은 방 그림에 있음
                    break;
            }
        }
        if (GameObject.Find("HomeWalls") != null) return;
        var walls = new GameObject("HomeWalls");
        SceneManager.MoveGameObjectToScene(walls, scene);
        Wall(walls, Left - 1f, Back, Right + 1f, Back + 3f);                 // 뒷벽
        Wall(walls, Left - 3f, Front - 3f, Left, Back + 3f);                 // 왼쪽
        Wall(walls, Right, Front - 3f, Right + 3f, Back + 3f);               // 오른쪽
        Wall(walls, Left - 1f, Front - 3f, DoorX0, Front);                   // 앞벽 (문 왼쪽)
        Wall(walls, DoorX1, Front - 3f, Right + 1f, Front);                  // 앞벽 (문 오른쪽)
        Wall(walls, DoorX0 - 1f, Front - 4f, DoorX1 + 1f, Front - 0.8f);     // 문 밖으로 너무 나가지 않게 (문턱 아래)
        Wall(walls, -4.22f, 1.1f, -3.41f, Back);                             // 자개장
        Wall(walls, 3.37f, 1.15f, 4.19f, Back);                              // 이불 얹은 반닫이
        Wall(walls, 1.94f, 0.97f, 2.75f, Back);                              // 서안 (라디오, 편지)
        Wall(walls, -0.05f, -0.5f, 0.45f, -0.27f);                           // 밥상 (다리)
    }

    static void Wall(GameObject parent, float x0, float y0, float x1, float y1)
    {
        BoxCollider2D b = parent.AddComponent<BoxCollider2D>();
        b.offset = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        b.size = new Vector2(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
    }

    static void HideAll(GameObject go)
    {
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
        if (go.GetComponent<HideArt>() == null) go.AddComponent<HideArt>();   // 나중에 생기는 픽셀 그림 겹그림도
    }
}
