using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 넓어진 농장 (화면 4장, Assets/Resources/Maps/farm.json). 씬 파일은 건드리지 않고 실행 중에 만듭니다.
//  - 큰 농장 그림(bg_farmmap_<계절>)을 원래 배경 자리에 깔기   (PixelArtWorld가 PlaceGround를 부름)
//  - 빈 흙 칸과 치운 칸에 '흙' 타일 깔기 → 괭이로 갈 수 있음
//  - 묵은 밭의 잡초/그루터기/바위(Debris), 새 나무/돌/사과(씬의 자원을 복제) 배치
//  - 출구: 시장 포털을 길 끝으로 옮기고, 잠긴 길(숲, 옛 기차역)에는 회색 가시덤불
//  - 카메라가 플레이어를 따라가고, 플레이어는 맵 밖으로 나가지 못함
//  - 다른 씬에서 농장으로 오는 포털의 도착 위치를 맵에 맞춤
public static class FarmMap
{
    public const string SceneName = "Farm";

    public static MapData Map => MapData.Load("farm");
    public static bool Active => Map != null && PixelArt.Enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Build(scene);
        Build(SceneManager.GetActiveScene());
    }

    static void Build(Scene scene)
    {
        if (!scene.IsValid() || !Active) return;
        MapData map = Map;
        PatchArrivals(scene, map);
        if (scene.name != SceneName) return;

        Material mat = FindSpriteMaterial();
        LayDirt(map);
        SpawnDebris(map, scene, mat);
        SpawnResources(map, scene);
        SetupExits(map, scene, mat);
        Camera cam = Camera.main;
        if (cam != null) CameraFollow.Attach(cam, map.Bounds);
    }

    // ---------------------------------------------------------------- 바닥 그림
    // 원래 BackGroundFarm 자리에 큰 농장 그림을 배경과 같은 픽셀 크기로, 맵 중앙에 깝니다.
    public static SpriteRenderer PlaceGround(SpriteRenderer original, Sprite sprite)
    {
        SpriteRenderer view = PixelArt.Overlay(original, sprite, native: true);
        if (view == null) return null;
        Vector2 c = Map.Center;
        view.transform.position = new Vector3(c.x, c.y, original.transform.position.z);
        view.sortingOrder = -50;   // 밭 타일(0) 아래
        return view;
    }

    // ---------------------------------------------------------------- 흙 타일
    static Tilemap FindTilemap()
    {
        if (PlayerAction.instance != null && PlayerAction.instance.groundTilemap != null) return PlayerAction.instance.groundTilemap;
        GameObject grid = GameObject.Find("Grid");
        return grid != null ? grid.GetComponentInChildren<Tilemap>() : null;
    }

    static void LayDirt(MapData map)
    {
        foreach (var cell in map.Cells())
        {
            char ch = cell.Value;
            bool open = ch == 'x' || (MapData.IsDebris(ch) && Cleared(cell.Key));
            if (open) MakeSoil(new Vector3Int(cell.Key.x, cell.Key.y, 0));
        }
    }

    static bool Cleared(Vector2Int cell)
    {
        DataManager data = DataManager.instance;
        return data != null && data.farm.IsDebrisCleared(cell.x, cell.y);
    }

    // 이 칸을 괭이로 갈 수 있는 흙으로 (이미 갈아 둔 칸은 그대로)
    public static void MakeSoil(Vector3Int cell)
    {
        Tilemap tilemap = FindTilemap();
        PlayerAction player = PlayerAction.instance;
        DataManager data = DataManager.instance;
        if (tilemap == null || player == null || player.dirtTile == null) return;
        if (data != null && data.farm.IsTilled(cell.x, cell.y)) return;
        tilemap.SetTile(cell, player.dirtTile);
    }

    // 묵은 밭 개간 진행도 (25% 단위로 알려 줌)
    public static void ReportCleared()
    {
        DataManager data = DataManager.instance;
        if (data == null || Map == null) return;
        int total = Map.CountDebris();
        int done = data.farm.clearedDebris.Count;
        if (total <= 0) return;
        int before = (done - 1) * 4 / total, after = done * 4 / total;
        if (after == before) return;
        if (done >= total)
        {
            HUD.Toast("<color=#ffe28a>The old field is completely cleared. It's all yours now!</color>", 4f);
            data.ChangeHeart(5f, "You brought the old field back to life.");
        }
        else HUD.Toast($"The old field is {after * 25}% cleared. ({done}/{total})", 3f);
    }

    // ---------------------------------------------------------------- 잡초 / 그루터기 / 바위
    static void SpawnDebris(MapData map, Scene scene, Material mat)
    {
        foreach (var cell in map.Cells())
        {
            if (!MapData.IsDebris(cell.Value) || Cleared(cell.Key)) continue;
            var c = new Vector3Int(cell.Key.x, cell.Key.y, 0);
            Debris d = Debris.Create(cell.Value, c, new Vector3(c.x + 0.5f, c.y + 0.5f, 0f), mat);
            SceneManager.MoveGameObjectToScene(d.gameObject, scene);
        }
    }

    // ---------------------------------------------------------------- 나무 / 돌 / 사과
    // 씬에 이미 있는 자원(프리팹)을 복제해서 같은 규칙(채집, 하루 뒤 다시 자람)을 그대로 씁니다.
    static void SpawnResources(MapData map, Scene scene)
    {
        GameObject wood = FindTemplate(scene, "Wood"), stone = FindTemplate(scene, "Stone"), apple = FindTemplate(scene, "Food");
        DataManager data = DataManager.instance;
        foreach (var cell in map.Cells())
        {
            GameObject template = cell.Value == 'T' ? wood : cell.Value == 'r' ? stone : cell.Value == 'a' ? apple : null;
            if (template == null) continue;
            Vector3 pos = new Vector3(cell.Key.x + 0.5f, cell.Key.y + 0.5f, template.transform.position.z);
            GameObject go = Object.Instantiate(template, pos, template.transform.rotation);
            go.name = template.name.Split(' ')[0] + " (map)";
            SceneManager.MoveGameObjectToScene(go, scene);
            if (data != null && data.IsResourceCleared(scene.name, go))
            {
                Object.Destroy(go);
                continue;
            }
            PixelArtWorld.ApplyTo(go);
        }
    }

    static GameObject FindTemplate(Scene scene, string tag)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.CompareTag(tag)) return root;
        return null;
    }

    // ---------------------------------------------------------------- 출구
    static void SetupExits(MapData map, Scene scene, Material mat)
    {
        if (map.exits == null) return;
        foreach (MapExit exit in map.exits)
        {
            if (exit.IsLocked)
            {
                LockedExit locked = LockedExit.Create(exit, map.Bounds, mat);
                SceneManager.MoveGameObjectToScene(locked.gameObject, scene);
                continue;
            }
            // 열린 출구: 같은 씬으로 가는 기존 포털을 길 끝으로 옮김
            foreach (Portal portal in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None))
            {
                if (portal.gameObject.scene != scene || portal.destinationSceneName != exit.to) continue;
                Rect area = exit.Area;
                portal.transform.position = new Vector3(area.center.x, area.center.y, portal.transform.position.z);
                BoxCollider2D box = portal.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    Vector3 s = portal.transform.lossyScale;
                    box.offset = Vector2.zero;
                    box.size = new Vector2(area.width / Mathf.Max(0.01f, Mathf.Abs(s.x)), area.height / Mathf.Max(0.01f, Mathf.Abs(s.y)));
                }
                SpriteRenderer sr = portal.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;   // 길 끝이 곧 출구 (표지판은 배경 그림에)
            }
        }
    }

    // 다른 씬에서 농장으로 오는 포털의 도착 위치
    static void PatchArrivals(Scene scene, MapData map)
    {
        if (!map.TryArrival(scene.name, out Vector2 arrival)) return;
        foreach (Portal portal in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None))
            if (portal.gameObject.scene == scene && portal.destinationSceneName == SceneName)
                portal.destinationPosition = arrival;
    }

    // 흥정 장면처럼 포털 없이 농장으로 돌아올 때
    public static void ReturnFrom(string fromScene)
    {
        MapData map = Map;
        if (map == null || !map.TryArrival(fromScene, out Vector2 arrival)) return;
        SceneData.nextPlayerPosition = arrival;
        SceneData.hasNextPlayerPosition = true;
    }

    static Material FindSpriteMaterial()
    {
        if (PlayerAction.instance != null)
        {
            SpriteRenderer sr = PlayerAction.instance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sharedMaterial != null) return sr.sharedMaterial;
        }
        SpriteRenderer any = Object.FindFirstObjectByType<SpriteRenderer>();
        return any != null ? any.sharedMaterial : null;
    }
}
