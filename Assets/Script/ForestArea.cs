using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 숲 (맵 3단계, Assets/Resources/Maps/forest.json). 마음이 봄이 되면 농장 서쪽 숲길이 열립니다.
// 별의 언덕처럼 농장 씬 안 멀리(동쪽 x 190~220)에 실행 중에 만듭니다. 씬 파일은 고치지 않습니다.
//  - 숲은 짙은 회색으로 시작. 빈터에 도토리를 심고 물을 주어 묘목이 다 자라면 그 빈터에 색이 돌아옵니다.
//    (회색 그림 위에 컬러 그림을 둥근 마스크로 보여 줌) 3곳 모두 되살리면 큰 나무가 깨어나 숲 전체가 컬러.
//  - 아침마다 채집물(달래, 고사리, 곰보버섯, 필요하면 도토리)이 몇 곳에 생깁니다.
//  - 샘가 그루터기에서 E: 하루 한 번 마음 +2, 물 마시기, 물뿌리개 채우기
//  - 쓰러진 통나무(Debris 'l')는 곡괭이로 치웁니다. 남쪽 빈터로 가는 길을 막고 있습니다.
public class ForestArea : MonoBehaviour
{
    public static ForestArea Instance { get; private set; }
    public static MapData Map => MapData.Load("forest");

    public const float RestHeart = 2f;
    public const float RestoreHeart = 3f;
    public const float AwakeHeart = 5f;

    private MapData map;
    private ForestState state;
    private SpriteRenderer colorGround, oldTree;
    private readonly List<Vector3Int> spots = new List<Vector3Int>();
    private readonly List<SpriteRenderer> saplings = new List<SpriteRenderer>();
    private readonly List<GameObject> masks = new List<GameObject>();
    private Vector2 seat, treeBase;
    private Material material;

    static DataManager Data => DataManager.instance;
    static string ClearingLabel(int i) => Loc.Ko ? (i == 0 ? "북쪽" : i == 1 ? "서쪽" : "남쪽") : ForestState.ClearingNames[i];

    // ---------------------------------------------------------------- 규칙 (DataManager가 부름)
    public static bool IsOpen => Data != null && Data.story.forest != null && Data.story.forest.open;

    // 마음의 계절이 봄 이상이면 숲길을 엽니다 (한 번 열리면 꽃샘추위에도 계속 열림). 새로 열렸으면 true
    public static bool CheckOpen(DataManager data)
    {
        if (data.story.forest == null) data.story.forest = new ForestState();
        ForestState f = data.story.forest;
        f.Ensure();
        if (f.open || data.CurrentSeason < Season.Spring) return false;
        f.open = true;
        data.story.Unlock("forest_open");
        return true;
    }

    public static List<(int x, int y)> ForageSpots(MapData forestMap)
    {
        var list = new List<(int x, int y)>();
        if (forestMap == null) return list;
        foreach (var cell in forestMap.Cells()) if (cell.Value == 'f') list.Add((cell.Key.x, cell.Key.y));
        return list;
    }

    public static void EnsureForage(DataManager data, int day, bool force)
    {
        ForestState f = data.story.forest;
        if (f == null || !f.open || Map == null) return;
        if (!force && f.forageDay == day) return;
        bool wantAcorn = f.AnyEmptySpot && !data.HasItem("Acorn");
        f.RollForage(day, data.CurrentSeason, data.heart, ForageSpots(Map), new System.Random(day * 7919 + 17), wantAcorn);
    }

    // 하루가 끝날 때: 숲길 열림, 묘목 성장, 큰 나무, 새 채집물. 아침 요약 줄을 더하고 마음 변화량을 돌려줌
    public static float Morning(DataManager data, bool rain, int day, List<string> lines, out string headline)
    {
        headline = null;
        float delta = 0f;
        if (CheckOpen(data))
            headline = Loc.T("<color=#ffb6d0>The gray thorns on the forest path have bloomed into an arch.</color>\nThe forest west of the farm is open.",
                "<color=#ffb6d0>숲길을 막던 잿빛 가시덤불이 꽃 아치로 피어났어요.</color>\n농장 서쪽 뒷산 길이 열렸어요.");
        ForestState f = data.story.forest;
        if (!f.open) return 0f;

        List<int> restored = f.AdvanceDay(rain, out bool woke);
        foreach (int i in restored)
        {
            delta += RestoreHeart;
            lines.Add(Loc.T($"Color came back to the {ForestState.ClearingNames[i]} clearing", $"{ClearingLabel(i)} 빈터에 빛깔이 돌아왔어요") + $"  <color=#9dff9d>+{RestoreHeart:0}</color>");
        }
        if (woke)
        {
            delta += AwakeHeart;
            lines.Add(Loc.T("The old zelkova woke up", "늙은 느티나무가 깨어났어요") + $"  <color=#9dff9d>+{AwakeHeart:0}</color>");
            headline = Loc.T("<color=#ffb6d0>Overnight, the old zelkova on the back hill woke up.</color>\nColor is flowing back into the whole forest.",
                "<color=#ffb6d0>밤사이 뒷산의 늙은 느티나무가 깨어났어요.</color>\n뒷산 전체에 빛깔이 다시 흘러들어요.");
        }
        EnsureForage(data, day, true);
        return delta;
    }

    // ---------------------------------------------------------------- 만들기 (FarmMap이 농장 씬에서 부름)
    public static ForestArea Build(Scene scene, Material material)
    {
        MapData forestMap = Map;
        DataManager data = Data;
        if (forestMap == null || data == null || !IsOpen) return null;

        var go = new GameObject("Forest");
        SceneManager.MoveGameObjectToScene(go, scene);
        ForestArea area = go.AddComponent<ForestArea>();
        area.Setup(forestMap, material, scene);
        return area;
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Setup(MapData forestMap, Material mat, Scene scene)
    {
        map = forestMap;
        material = mat;
        state = Data.story.forest;
        state.Ensure();
        int day = GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
        EnsureForage(Data, day, false);

        Vector2 center = map.Center;
        // 바닥: 회색 숲 + 그 위에 마스크로만 보이는 컬러 숲
        Ground("bg_forest_gray", center, -50, SpriteMaskInteraction.None);
        colorGround = Ground("bg_forest", center, -49, state.treeAwake ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask);

        AreaKit.Walls(transform, map, Blocks);
        var treeCells = new List<Vector2Int>();

        foreach (var cell in map.Cells())
        {
            var v = new Vector3Int(cell.Key.x, cell.Key.y, 0);
            Vector3 mid = new Vector3(v.x + 0.5f, v.y + 0.5f, 0f);
            switch (cell.Value)
            {
                case 'c': spots.Add(v); break;
                case 's': seat = mid; break;
                case 'O': treeCells.Add(cell.Key); break;
                case 'L':
                    if (Data.farm.IsDebrisCleared(v.x, v.y)) break;
                    Debris log = Debris.Create('l', v, mid, mat);
                    SceneManager.MoveGameObjectToScene(log.gameObject, scene);
                    break;
            }
        }

        // 큰 나무 (3x3 칸의 바닥 가운데에 밑동)
        if (treeCells.Count > 0)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue;
            foreach (var t in treeCells) { minX = Mathf.Min(minX, t.x); maxX = Mathf.Max(maxX, t.x + 1); minY = Mathf.Min(minY, t.y); }
            treeBase = new Vector2((minX + maxX) * 0.5f, minY);
            oldTree = Sprite("OldTree", state.treeAwake ? "old_tree_awake" : "old_tree_sleep", treeBase, 4);
        }

        // 빈터마다: 묘목과 색 마스크
        for (int i = 0; i < spots.Count; i++)
        {
            Vector2 at = new Vector2(spots[i].x + 0.5f, spots[i].y + 0.05f);
            saplings.Add(Sprite("Sapling", null, at, 4));
            GameObject m = AreaKit.Mask(transform, "forest_mask", new Vector2(spots[i].x + 0.5f, spots[i].y + 0.5f));
            masks.Add(m);
        }
        RefreshSaplings();

        foreach (string f in state.forage) AreaKit.SpawnForage(transform, f, material, "forest");

        // 숲 입구 → 농장
        if (map.exits != null)
            foreach (MapExit exit in map.exits)
            {
                AreaPortal back = AreaPortal.Create("ForestExit", exit.Area, new Vector2(exit.spawnX, exit.spawnY));
                back.transform.SetParent(transform, true);
            }
    }

    static bool Blocks(char ch) => ch == '#' || ch == '~' || ch == 'w' || ch == 'O' || ch == 'B';

    SpriteRenderer Ground(string key, Vector2 center, int order, SpriteMaskInteraction maskMode) =>
        AreaKit.Ground(transform, key, center, order, maskMode, material);

    SpriteRenderer Sprite(string name, string key, Vector2 bottom, int order) =>
        AreaKit.SpriteAt(transform, name, key, bottom, order, material);

    static void SetSprite(SpriteRenderer sr, string key, Vector2 bottom) => AreaKit.SetSprite(sr, key, bottom);

    void RefreshSaplings()
    {
        for (int i = 0; i < spots.Count && i < ForestState.Clearings; i++)
        {
            int stage = state.sapling[i];
            Vector2 bottom = new Vector2(spots[i].x + 0.5f, spots[i].y + 0.05f);
            SetSprite(saplings[i], stage < 0 ? "sapling_spot" : "sapling_" + Mathf.Min(stage, ForestState.RestoredStage), bottom);
            // 오늘 물을 아직 안 받은 묘목은 살짝 어둡게
            saplings[i].color = state.NeedsWater(i) ? new Color(0.82f, 0.82f, 0.82f) : Color.white;
            if (i < masks.Count) masks[i].SetActive(state.IsRestored(i));
        }
        if (colorGround != null) colorGround.maskInteraction = state.treeAwake ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
    }

    // ---------------------------------------------------------------- 빈터 (PlayerAction이 부름)
    int ClearingAt(Vector3Int cell)
    {
        for (int i = 0; i < spots.Count; i++) if (spots[i] == cell) return i;
        return -1;
    }

    // 클릭한 칸이 묘목 자리면 처리하고 true. 도토리를 엉뚱한 곳에 쓰면 안내하고 true.
    public static bool TryUse(Vector3Int cell, ItemDef def)
    {
        bool acorn = def != null && def.name == "Acorn";
        ForestArea area = Instance;
        int i = area != null ? area.ClearingAt(cell) : -1;
        if (i < 0)
        {
            if (acorn) HUD.Toast(Loc.T("Acorns want to grow in a forest clearing. Look for the ring of pebbles.", "도토리는 숲 빈터에서 자라고 싶어 해요. 조약돌로 두른 자리를 찾아보세요."), 3f);
            return acorn;
        }
        area.UseSpot(i, def);
        return true;
    }

    void UseSpot(int i, ItemDef def)
    {
        DataManager data = Data;
        int stage = state.sapling[i];
        string clearing = ClearingLabel(i);
        if (stage < 0)
        {
            if (def == null || def.name != "Acorn")
            {
                HUD.Toast(Loc.T($"A ring of pebbles in the {clearing} clearing. An acorn could grow here.", $"{clearing} 빈터에 조약돌을 둥글게 둘러놨어요. 여기라면 도토리가 자랄 것 같아요."), 3f);
                return;
            }
            if (!data.UseEnergy(1f)) return;
            data.RemoveItem("Acorn", 1);
            state.Plant(i);
            HUD.Toast(Loc.T("You planted the acorn. Water it every day and it will grow.", "도토리를 심었어요. 날마다 물을 주면 자랄 거예요."), 3f);
        }
        else if (stage >= ForestState.RestoredStage)
        {
            HUD.Toast(Loc.T($"The young oak is doing well. The {clearing} clearing is full of color again.", $"어린 참나무가 잘 크고 있어요. {clearing} 빈터에 다시 빛깔이 가득해요."), 3f);
            return;
        }
        else if (def != null && def.toolType == ToolType.WateringCan)
        {
            if (!state.NeedsWater(i)) { HUD.Toast(Loc.T("It's already had water today.", "오늘은 벌써 물을 줬어요."), 2f); return; }
            if (data.waterCharges <= 0) { HUD.Toast(Loc.T("Your watering can is empty. The spring or the well can fill it.", "물뿌리개가 비었어요. 옹달샘이나 우물에서 채울 수 있어요."), 3f); return; }
            if (!data.UseEnergy(2f)) return;
            data.waterCharges--;
            state.Water(i);
            data.UpdateAllUI();
            HUD.Toast(stage == 0 ? Loc.T("The soil soaks up the water.", "흙이 물을 머금어요.") : Loc.T("The sapling drinks the water.", "묘목이 물을 마셔요."), 2f);
        }
        else
        {
            string[] looks = Loc.Ko
                ? new[] { "흙 속에서 도토리가 자고 있어요.", "조그만 싹이 고개를 내밀었어요.", "묘목이 쑥쑥 자라고 있어요." }
                : new[] { "An acorn is sleeping in the soil.", "A tiny sprout is peeking out.", "The sapling is getting taller." };
            HUD.Toast($"{looks[Mathf.Clamp(stage, 0, 2)]} ({stage}/3) " + Loc.T("Water it every day.", "날마다 물을 주세요."), 3f);
        }
        RefreshSaplings();
    }

    // ---------------------------------------------------------------- 샘가 쉼터, 큰 나무
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E) || HUD.DialogueOpen || Time.frameCount - HUD.DialogueClosedFrame <= 1) return;
        DataManager data = Data;
        PlayerAction player = PlayerAction.instance;
        if (data == null || player == null || data.IsGameOver) return;
        Vector2 p = player.transform.position;

        if (Vector2.Distance(p, seat) < 1.3f)
        {
            data.RefillThirst();
            if (data.WaterCapacity > 0) { data.RefillWater(); data.UpdateAllUI(); }
            int day = GameTimeManager.instance != null ? GameTimeManager.instance.day : 1;
            if (state.Rest(day))
            {
                data.NoteRest();
                data.ChangeHeart(RestHeart, Loc.T("You sat by the spring and listened to the forest.", "옹달샘 곁에 앉아 숲 소리를 들었어요."));
            }
            else HUD.Toast(Loc.T("You drink from the spring. It's cool and clear.", "옹달샘 물을 마셨어요. 시원하고 맑아요."), 2.5f);
            return;
        }
        if (Vector2.Distance(p, treeBase + new Vector2(0f, 0.6f)) < 2.2f)
        {
            if (state.treeAwake)
                HUD.Toast(Loc.T("The old zelkova rustles softly. Its shade is cool and green.", "늙은 느티나무가 나직이 바스락거려요. 그늘이 서늘하고 푸르러요."), 3f);
            else
                HUD.Toast(Loc.T($"The old zelkova on its stone platform is asleep, gray to its roots. Maybe if the clearings around it came back to life... ({state.Restored}/3)",
                    $"돌 축대 위 늙은 느티나무가 뿌리까지 잿빛으로 잠들어 있어요. 주위 빈터들이 살아나면 혹시... ({state.Restored}/3)"), 4f);
        }
    }
}
