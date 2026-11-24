using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 노을 언덕 (맵 4-2단계, Assets/Resources/Maps/hill.json). 마음이 가을이 되면 농장 남쪽 언덕길이 열립니다.
// 숲, 호수처럼 농장 씬 안 멀리(x 310~340)에 실행 중에 만듭니다.
//  - 늙은 과일나무 6그루(사과, 배, 감): 낫으로 가지치기 → 대지의 숨결 → 되살아나 그 둘레에 색이 돌아옴.
//    되살아난 나무는 며칠마다 열매. 6그루 모두 되살리면 과수원 전체가 컬러.
//  - 곶감 걸이(E): 감을 걸어 두면 며칠 뒤 곶감.
//  - 꼭대기 전망대 벤치(E): 마을 전체가 내려다보이는 풍경. 지금까지 되살린 곳만 컬러로 보입니다. 하루 한 번 마음 +2.
public class HillArea : MonoBehaviour
{
    public static HillArea Instance { get; private set; }
    public static MapData Map => MapData.Load("hill");

    public const float ReviveHeart = 2f;
    public const float OrchardHeart = 5f;
    public const float RestHeart = 2f;
    public const float PruneEnergy = 4f;

    private MapData map;
    private HillState state;
    private Material material;
    private SpriteRenderer colorGround, rack;
    private readonly List<Vector3Int> trunks = new List<Vector3Int>();
    private readonly List<char> kinds = new List<char>();
    private readonly List<SpriteRenderer> trees = new List<SpriteRenderer>();
    private readonly List<GameObject> masks = new List<GameObject>();
    private Vector2 rackPos, bench;
    private float nextRefresh;

    static DataManager Data => DataManager.instance;
    static int Today => AreaKit.Today;

    // ---------------------------------------------------------------- 규칙
    public static bool IsOpen => Data != null && Data.story.hill != null && Data.story.hill.open;

    public static bool CheckOpen(DataManager data)
    {
        if (data.story.hill == null) data.story.hill = new HillState();
        HillState h = data.story.hill;
        h.Ensure();
        if (h.open || data.CurrentSeason < Season.Autumn) return false;
        h.open = true;
        data.story.Unlock("hill_open");
        return true;
    }

    public static void Morning(DataManager data, out string headline)
    {
        headline = CheckOpen(data)
            ? "<color=#ffb070>The thorns on the south lane turned into an arch of autumn leaves.</color>\nA path climbs up to Sunset Hill."
            : null;
    }

    // ---------------------------------------------------------------- 만들기 (FarmMap이 부름)
    public static HillArea Build(Scene scene, Material material)
    {
        MapData hillMap = Map;
        if (hillMap == null || Data == null || !IsOpen) return null;
        var go = new GameObject("Hill");
        SceneManager.MoveGameObjectToScene(go, scene);
        HillArea area = go.AddComponent<HillArea>();
        area.Setup(hillMap, material);
        return area;
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Setup(MapData hillMap, Material mat)
    {
        map = hillMap;
        material = mat;
        state = Data.story.hill;
        state.Ensure();

        Vector2 center = map.Center;
        AreaKit.Ground(transform, "bg_hill_gray", center, -50, SpriteMaskInteraction.None, material);
        colorGround = AreaKit.Ground(transform, "bg_hill", center, -49, SpriteMaskInteraction.VisibleInsideMask, material);
        AreaKit.Walls(transform, map, ch => "#CAPKDRB".IndexOf(ch) >= 0);

        foreach (var cell in map.Cells())
        {
            Vector2 mid = new Vector2(cell.Key.x + 0.5f, cell.Key.y + 0.5f);
            switch (cell.Value)
            {
                case 'A': case 'P': case 'K':
                    if (trunks.Count >= HillState.Trees) break;
                    var v = new Vector3Int(cell.Key.x, cell.Key.y, 0);
                    trunks.Add(v);
                    kinds.Add(cell.Value);
                    trees.Add(AreaKit.SpriteAt(transform, "OrchardTree", null, new Vector2(v.x + 0.5f, v.y + 0.05f), 4, material));
                    masks.Add(AreaKit.Mask(transform, "forest_mask", new Vector2(v.x + 0.5f, v.y + 1.2f)));
                    break;
                case 'D':
                    rackPos = mid;
                    rack = AreaKit.SpriteAt(transform, "DryingRack", null, new Vector2(mid.x, cell.Key.y + 0.05f), 4, material);
                    break;
                case 'V':
                    bench = mid;
                    break;
            }
        }
        if (map.exits != null)
            foreach (MapExit exit in map.exits)
                AreaPortal.Create("HillExit", exit.Area, new Vector2(exit.spawnX, exit.spawnY)).transform.SetParent(transform, true);
        Refresh();
    }

    static void SetSeasonal(SpriteRenderer sr, string key, Vector2 bottom)
    {
        Sprite sprite = PixelArtWorld.SeasonalSprite(key);
        sr.sprite = sprite;
        if (sprite == null) return;
        float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
        sr.transform.localScale = new Vector3(s, s, 1f);
        sr.transform.position = new Vector3(bottom.x, bottom.y + sprite.rect.height / sprite.pixelsPerUnit * s * 0.5f, 0f);
    }

    void Refresh()
    {
        Season season = Data.CurrentSeason;
        for (int i = 0; i < trees.Count; i++)
        {
            int st = state.tree[i];
            string kind = HillState.KindName(kinds[i]);
            string key = st == 0 ? "orchard_gray" : st == 1 ? "orchard_pruned"
                : state.HasFruit(i, Today, season) ? $"orchard_{kind}_fruit" : $"orchard_{kind}_bare";
            SetSeasonal(trees[i], key, new Vector2(trunks[i].x + 0.5f, trunks[i].y + 0.05f));
            masks[i].SetActive(state.IsAlive(i));
        }
        if (rack != null)
        {
            string key = state.drying.Count == 0 ? "drying_rack_empty" : state.ReadyCount(Today) > 0 ? "drying_rack_dried" : "drying_rack_hung";
            SetSeasonal(rack, key, new Vector2(rackPos.x, rackPos.y - 0.45f));
        }
        if (colorGround != null) colorGround.maskInteraction = state.AllRevived ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
    }

    // ---------------------------------------------------------------- 과일나무 (PlayerAction이 부름)
    public static bool TryUse(Vector3Int cell, ItemDef def)
    {
        HillArea area = Instance;
        if (area == null) return false;
        int i = area.trunks.IndexOf(cell);
        if (i < 0) return false;
        area.UseTree(i, def);
        return true;
    }

    // 화면에 보이는 나무 이름 (그림 키는 HillState.KindName)
    static string KindLabel(char k) => Loc.Ko ? (k == 'A' ? "사과" : k == 'P' ? "배" : "감") : HillState.KindName(k);

    void UseTree(int i, ItemDef def)
    {
        DataManager data = Data;
        string kind = KindLabel(kinds[i]);
        int st = state.tree[i];
        if (st == 0)
        {
            if (def == null || def.toolType != ToolType.Scythe)
            {
                HUD.Toast(Loc.T($"An old {kind} tree, gray and tangled. Prune it with a scythe first.", $"잿빛으로 엉클어진 늙은 {kind}나무. 먼저 낫으로 가지를 쳐 주세요."), 3f);
                return;
            }
            if (!data.UseEnergy(PruneEnergy)) return;
            state.Prune(i);
            HUD.Toast(Loc.T($"You cut back the dead branches. The {kind} tree looks lighter. (It needs Earth's Breath now.)", $"죽은 가지를 쳐냈어요. {kind}나무가 한결 가벼워 보여요. (이제 대지의 숨결이 필요해요.)"), 3.5f);
        }
        else if (st == 1)
        {
            if (def == null || def.name != "Earth's Breath")
            {
                HUD.Toast(Loc.T($"The pruned {kind} tree is waiting. Give it Earth's Breath to wake it up.", $"가지를 친 {kind}나무가 기다려요. 대지의 숨결로 깨워 주세요."), 3f);
                return;
            }
            data.RemoveItem("Earth's Breath", 1);
            state.Revive(i, Today);
            data.ChangeHeart(ReviveHeart, Loc.T($"The old {kind} tree woke up. Color spreads around it.", $"늙은 {kind}나무가 깨어났어요. 주위로 빛깔이 번져요."));
            if (state.AllRevived)
            {
                data.ChangeHeart(OrchardHeart, Loc.T("The whole orchard is alive again.", "과수원 전체가 다시 살아났어요."));
                HUD.Announce("<size=42><color=#ffb070>The orchard is alive again</color></size>\n\nApples, pears and persimmons, all in color.\nThe hill glows in the evening light.", 6f);
            }
        }
        else
        {
            int n = state.Harvest(i, Today, data.CurrentSeason);
            string fruit = HillState.FruitOf(kinds[i]);
            if (n > 0)
            {
                data.AddItem(fruit, n);
                HUD.Toast(Loc.T($"You picked {n} {fruit}.", $"{ItemNames.Display(fruit)} {n}개를 땄어요.") + (fruit == "Persimmon" ? Loc.T(" (Hang them on the drying rack!)", " (곶감 걸이에 걸어 보세요!)") : ""), 2.5f);
            }
            else if (data.CurrentSeason == Season.Winter) HUD.Toast(Loc.T("It's resting through the cold. No fruit in winter.", "추위 속에 쉬고 있어요. 겨울엔 열매가 없어요."), 2.5f);
            else HUD.Toast(Loc.T($"No fruit yet. Come back in {Mathf.Max(1, state.fruitDay[i] - Today)} day(s).", $"아직 열매가 없어요. {Mathf.Max(1, state.fruitDay[i] - Today)}일 뒤에 와 보세요."), 2.5f);
        }
        Refresh();
    }

    // ---------------------------------------------------------------- 곶감 걸이, 전망대
    void Update()
    {
        if (Time.time >= nextRefresh) { nextRefresh = Time.time + 2f; Refresh(); }
        if (!AreaKit.PressedE() || PixelHUD.ViewOpen) return;
        DataManager data = Data;
        PlayerAction player = PlayerAction.instance;
        if (data == null || player == null || data.IsGameOver) return;
        Vector2 p = player.transform.position;

        if (Vector2.Distance(p, rackPos) < 1.5f) { UseRack(data); return; }
        if (Vector2.Distance(p, bench) < 1.6f)
        {
            PixelHUD.ShowView(PanoramaLayers(data), PanoramaCaption(data));
            if (state.Rest(Today)) data.ChangeHeart(RestHeart, Loc.T("You sat on the hill and watched the sun go down over the valley.", "언덕에 앉아 골짜기 너머로 해가 지는 걸 바라봤어요."));
        }
    }

    void UseRack(DataManager data)
    {
        int ready = state.Collect(Today);
        if (ready > 0) data.AddItem("Dried Persimmon", ready);
        int have = data.GetCount("Persimmon");
        int hung = state.Hang(have, Today);
        if (hung > 0) data.RemoveItem("Persimmon", hung);
        if (ready > 0 && hung > 0) HUD.Toast(Loc.T($"You took down {ready} Dried Persimmon and hung up {hung} more.", $"곶감 {ready}개를 걷고 감 {hung}개를 새로 걸었어요."), 3f);
        else if (ready > 0) HUD.Toast(Loc.T($"You took down {ready} Dried Persimmon. Chewy and sweet!", $"곶감 {ready}개를 걷었어요. 쫀득하고 달아요!"), 3f);
        else if (hung > 0) HUD.Toast(Loc.T($"You hung up {hung} persimmon(s). They'll be dried in {HillState.DryDays} days.", $"감 {hung}개를 걸었어요. {HillState.DryDays}일이면 곶감이 돼요."), 3f);
        else if (state.drying.Count > 0) HUD.Toast(Loc.T($"The persimmons are still drying. ({state.drying.Count} on the rack)", $"감이 아직 마르는 중이에요. (걸이에 {state.drying.Count}개)"), 2.5f);
        else HUD.Toast(Loc.T("A drying rack. Hang persimmons here and wait a few days.", "곶감 걸이. 감을 걸고 며칠 기다리면 돼요."), 2.5f);
        Refresh();
    }

    // ---------------------------------------------------------------- 전망대 풍경
    public static float FieldCleared(DataManager data)
    {
        MapData farm = FarmMap.Map;
        if (farm == null) return 0f;
        int total = 0, done = 0;
        // 새 농장: 굳은 묵정밭 뙈기를 퇴비로 되살린 비율
        if (farm.plots != null && farm.plots.Length > 0)
        {
            foreach (MapPlot p in farm.plots)
            {
                if (p.startOpen) continue;
                total++;
                if (Plots.IsOpen(data.farm, p)) done++;
            }
            return total > 0 ? done / (float)total : 1f;
        }
        foreach (var cell in farm.Cells())
        {
            if (!MapData.IsDebris(cell.Value)) continue;
            total++;
            if (data.farm.IsDebrisCleared(cell.Key.x, cell.Key.y)) done++;
        }
        return total > 0 ? done / (float)total : 1f;
    }

    // 지금 진행에 맞는 풍경 그림 층 (아래부터)
    public static List<string> PanoramaLayers(DataManager data)
    {
        StoryState s = data.story;
        bool farm = FieldCleared(data) >= 0.75f, forest = s.forest.treeAwake, lake = s.lake.IsClear,
             village = s.AllVillagersRestored, home = s.IsUnlocked("ending_seen"), orchard = s.hill.AllRevived;
        var layers = new List<string> { "pano_base" };
        if (farm && forest && lake && village && home && orchard) { layers.Add("pano_land"); layers.Add("pano_mountains"); }
        if (forest) layers.Add("pano_forest");
        if (lake) layers.Add("pano_lake");
        if (village) layers.Add("pano_village");
        if (farm) layers.Add("pano_farm");
        if (home) layers.Add("pano_home");
        if (orchard) layers.Add("pano_orchard");
        return layers;
    }

    public static string PanoramaCaption(DataManager data)
    {
        StoryState s = data.story;
        string Mark(bool done, string name) => done ? $"<color=#ffd84a>{name}</color>" : $"<color=#8a8a92>{name}</color>";
        return Mark(FieldCleared(data) >= 0.75f, Loc.T("The plots", "텃밭")) + "   " + Mark(s.forest.treeAwake, Loc.T("The forest", "숲")) + "   " +
               Mark(s.lake.IsClear, Loc.T("The lake", "저수지")) + "   " + Mark(s.AllVillagersRestored, Loc.T("The village", "마을")) + "   " +
               Mark(s.hill.AllRevived, Loc.T("The orchard", "과수원")) + "   " + Mark(s.IsUnlocked("ending_seen"), Loc.T("Home", "집"));
    }
}
