using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 농장 (화면 4장, Assets/Resources/Maps/farm.json): 한국 시골 집과 텃밭. 씬 파일은 건드리지 않고 실행 중에 만듭니다.
//  - 큰 농장 그림(bg_farmmap_<계절>)을 원래 배경 자리에 깔기   (PixelArtWorld가 PlaceGround를 부름)
//  - 집 문, 우물, 보관 상자를 맵의 자리(anchors)로 옮기고, 예전 농장의 나무/돌/사과와 흙 타일은 치움
//  - 지나갈 수 없는 칸(뒷산, 개울, 돌담, 집, 장독대, 논...)에 벽
//  - 텃밭 뙈기(plots): 열린 뙈기에서만 농사. 굳은 묵정밭은 퇴비로 되살림. 쉰 땅에는 짚 (FarmPlots)
//  - 퇴비 더미 (CompostHeap)
//  - 출구: 시장 포털을 길 끝으로 옮기고, 잠긴 길(숲, 언덕)에는 회색 가시덤불, 호수 길에는 회색 갈대
//  - 카메라가 플레이어를 따라가고, 플레이어는 맵 밖으로 나가지 못함
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
        if (DataManager.instance != null)
        {
            // 예전 저장 파일: 이미 그 계절이면 열어 둠
            ForestArea.CheckOpen(DataManager.instance);
            LakeArea.CheckOpen(DataManager.instance);
            HillArea.CheckOpen(DataManager.instance);
        }
        MigrateOldFarm(map);
        ClearSceneTiles(map);
        MoveSceneObjects(map, scene);
        SpawnDebris(map, scene, mat);
        SpawnResources(map, scene);
        BuildWalls(map, scene);
        FarmPlots.Create(scene, mat);
        CompostHeap.Create(scene, Anchor("compost", new Vector2(9.5f, 1.5f)), mat);
        SetupExits(map, scene, mat);
        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? CameraFollow.Attach(cam, map.Bounds) : null;

        // 숲(봄), 호수(여름): 같은 씬 안 멀리에 만들고, 카메라가 다닐 수 있는 지역으로 추가
        ForestArea forest = ForestArea.Build(scene, mat);
        if (forest != null && follow != null) follow.AddRegion(ForestArea.Map.Bounds);
        LakeArea lake = LakeArea.Build(scene, mat);
        if (lake != null && follow != null) follow.AddRegion(LakeArea.Map.Bounds);
        HillArea hill = HillArea.Build(scene, mat);
        if (hill != null && follow != null) follow.AddRegion(HillArea.Map.Bounds);

        // 엔딩 뒤: 회색이던 오두막에 색 (배경의 회색 오두막 위에 컬러 오두막을 겹침)
        if (StoryEvents.EndingSeen) PlaceColorCottage(scene, mat);
        KeepPlayerSafe(map, follow);
    }

    // ---------------------------------------------------------------- 맵의 자리
    public static Vector2 Anchor(string name, Vector2 fallback) => Map != null ? Map.Anchor(name, fallback) : fallback;

    public static MapPlot[] PlotList => Map != null ? Map.plots : null;

    // 지나갈 수 없는 칸
    public static bool Blocks(char ch) => "PwWHJKBCRb|".IndexOf(ch) >= 0;

    static void BuildWalls(MapData map, Scene scene)
    {
        var root = new GameObject("FarmWalls");
        SceneManager.MoveGameObjectToScene(root, scene);
        AreaKit.Walls(root.transform, map, Blocks);
    }

    // 예전 저장 파일: 뙈기 밖의 밭과 작물을 정리하고, 작물은 씨앗으로 돌려줌
    static void MigrateOldFarm(MapData map)
    {
        DataManager data = DataManager.instance;
        if (data == null || map.plots == null) return;
        var refunds = new List<string>();
        int removed = Plots.Migrate(data.farm, map.plots, refunds);
        foreach (string seed in refunds) data.AddItem(seed, 1);
        if (removed > 0)
            HUD.Toast(Loc.T($"The farm has changed. {removed} crop(s) outside the new plots were returned to your bag as seeds.",
                            $"농장이 바뀌었어요. 새 텃밭 밖에 있던 작물 {removed}개를 씨앗으로 돌려받았어요."), 5f);
    }

    // 씬에 칠해져 있던 예전 흙 타일을 지움 (갈아 둔 밭은 CropManager가 데이터대로 다시 그림)
    static void ClearSceneTiles(MapData map)
    {
        Tilemap tilemap = FindTilemap();
        if (tilemap == null) return;
        DataManager data = DataManager.instance;
        BoundsInt b = tilemap.cellBounds;
        foreach (Vector3Int cell in b.allPositionsWithin)
        {
            if (tilemap.GetTile(cell) == null) continue;
            if (data != null && data.farm.IsTilled(cell.x, cell.y)) continue;
            tilemap.SetTile(cell, null);
        }
    }

    // 씬에 원래 있던 집 문, 우물, 보관 상자를 새 자리로. 예전 농장의 장식 사각형은 숨김
    static void MoveSceneObjects(MapData map, Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            string n = root.name;
            if (n == "well_0") MoveTo(root, map.Anchor("well", root.transform.position));
            else if (n == "Storage") MoveTo(root, map.Anchor("storage", root.transform.position));
            else if (n.StartsWith("Square")) root.SetActive(false);
            else if (root.GetComponent<Portal>() != null && root.GetComponent<Portal>().destinationSceneName == "MiniGame")
                root.SetActive(false);   // 흥정은 이제 장날 장터에서 (MarketFair). 뒷산 길은 길만 남김
            else
            {
                Portal portal = root.GetComponent<Portal>();
                if (portal != null && portal.destinationSceneName == "Home")
                {
                    MoveTo(root, map.Anchor("door", root.transform.position));
                    if (root.GetComponent<HideArt>() == null) root.AddComponent<HideArt>();   // 문은 집 그림에 있으므로 표지판은 숨김
                }
            }
        }
    }

    static void MoveTo(GameObject go, Vector2 at)
    {
        go.transform.position = new Vector3(at.x, at.y, go.transform.position.z);
    }

    // 새 게임이나 예전 저장에서 벽 안이나 맵 밖에 서 있으면 마당으로
    static void KeepPlayerSafe(MapData map, CameraFollow follow)
    {
        PlayerAction player = PlayerAction.instance;
        if (player == null) return;
        Vector2 p = player.transform.position;
        bool inFarm = map.Bounds.Contains(p);
        bool inRegion = (ForestArea.Map != null && ForestArea.Map.Bounds.Contains(p))
                     || (LakeArea.Map != null && LakeArea.Map.Bounds.Contains(p))
                     || (HillArea.Map != null && HillArea.Map.Bounds.Contains(p))
                     || p.y > 50f;                                  // 별의 언덕
        bool blocked = inFarm && Blocks(map.At(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)));
        if ((inFarm && !blocked) || (!inFarm && inRegion)) return;
        Vector2 start = map.Anchor("start", map.Center);
        player.transform.position = new Vector3(start.x, start.y, player.transform.position.z);
    }

    // ---------------------------------------------------------------- 텃밭
    // 이 칸이 열린 텃밭 뙈기 안인지 (농사를 지을 수 있는지)
    public static bool CanFarmCell(Vector3Int cell)
    {
        DataManager data = DataManager.instance;
        return data != null && Map != null && Plots.CanFarm(data.farm, Map.plots, cell.x, cell.y);
    }

    public static MapPlot PlotAt(Vector3Int cell) => Map != null ? Plots.At(Map.plots, cell.x, cell.y) : null;

    public static bool IsRestedCell(Vector3Int cell)
    {
        DataManager data = DataManager.instance;
        MapPlot plot = PlotAt(cell);
        return data != null && plot != null && Plots.IsOpen(data.farm, plot) && Plots.IsRested(data.farm, plot);
    }

    // 굳은 묵정밭을 누르면: 퇴비를 들고 있으면 되살리고, 아니면 안내. 묵정밭이 아니면 false
    public static bool TryUseHardPlot(Vector3Int cell, ItemDef held)
    {
        DataManager data = DataManager.instance;
        MapPlot plot = PlotAt(cell);
        if (data == null || plot == null || Plots.IsOpen(data.farm, plot)) return false;
        int have = data.GetCount("Compost");
        if (held == null || held.name != "Compost")
        {
            HUD.Toast(Loc.T($"This plot has gone gray and hard. Spread {plot.RestoreCost} bags of compost on it to bring it back. (You have {have})",
                            $"회색으로 굳은 묵정밭이에요. 퇴비 {plot.RestoreCost}포대를 뿌리면 되살아나요. (가진 퇴비 {have})"), 3.5f);
            return true;
        }
        int used = Plots.Restore(data.farm, plot, have);
        if (used <= 0)
        {
            HUD.Toast(Loc.T($"Not enough compost. This plot needs {plot.RestoreCost} bags. (You have {have})",
                            $"퇴비가 모자라요. 이 뙈기에는 {plot.RestoreCost}포대가 필요해요. (가진 퇴비 {have})"), 3f);
            return true;
        }
        data.RemoveItem("Compost", used);
        Sfx.Play("heart", 0.9f);
        data.ChangeHeart(2f, null);
        HUD.Toast(Loc.T("The gray crust crumbles away. Dark, soft soil breathes again. (+2 Heart, rested soil)",
                        "굳은 흙이 부서지고, 검고 보드라운 흙이 다시 숨을 쉬어요. (마음 +2, 쉰 땅)"), 4f);
        FarmPlots.RefreshAll();
        return true;
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
        int done = 0;
        foreach (var cell in Map.Cells())
            if (MapData.IsDebris(cell.Value) && data.farm.IsDebrisCleared(cell.Key.x, cell.Key.y)) done++;
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
        // 예전 한 화면 농장에 놓여 있던 나무/돌/사과는 새 집과 텃밭 자리와 겹치므로 치움 (맵의 자원만 남김)
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name.EndsWith("(map)")) continue;
            if (root.CompareTag("Wood") || root.CompareTag("Stone") || root.CompareTag("Food")) root.SetActive(false);
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
            MapData region = RegionMap(exit.to);
            if (region != null && RegionOpen(exit.to))
            {
                // 계절이 와서 열린 길 (가시덤불 → 꽃 아치, 회색 갈대 → 초록 갈대) → 걸어 들어가면 그 지역 입구로
                region.TryArrival("Farm", out Vector2 into);
                AreaPortal portal = AreaPortal.Create(exit.to + "Gate", exit.Area, into, ArriveLine(exit.to));
                LockedExit.PlaceArt(portal.transform, exit.OpenArt, exit.Area, map.Bounds, mat);
                SceneManager.MoveGameObjectToScene(portal.gameObject, scene);
                continue;
            }
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

    // 같은 씬 안에 만드는 지역들 (출구 이름 → 맵, 열렸는지)
    static MapData RegionMap(string to) => to == "Forest" ? ForestArea.Map : to == "Lake" ? LakeArea.Map : to == "Hill" ? HillArea.Map : null;
    static bool RegionOpen(string to) => to == "Forest" ? ForestArea.IsOpen : to == "Lake" ? LakeArea.IsOpen : to == "Hill" && HillArea.IsOpen;
    static string ArriveLine(string to) => to == "Lake" ? "The reeds rustle as you pass. The air smells of water."
        : to == "Hill" ? "Leaves crunch underfoot. The path climbs toward the sunset."
        : "The air turns cool and green. You step into the forest.";

    public static readonly Vector2 DefaultCottageBase = new Vector2(-7.75f, -3.85f);
    public static Vector2 CottageBase => Anchor("cottage", DefaultCottageBase);

    static void PlaceColorCottage(Scene scene, Material mat)
    {
        Sprite sprite = PixelArtWorld.SeasonalSprite("cottage_color");
        if (sprite == null) return;
        var go = new GameObject("CottageColor");
        SceneManager.MoveGameObjectToScene(go, scene);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingOrder = -49;
        float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.position = new Vector3(CottageBase.x, CottageBase.y + sprite.rect.height / sprite.pixelsPerUnit * s * 0.5f, 0f);
        go.AddComponent<SeasonalArt>().baseKey = "cottage_color";
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
