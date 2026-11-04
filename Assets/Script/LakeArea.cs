using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 호수 (맵 4-1단계, Assets/Resources/Maps/lake.json). 마음이 여름이 되면 농장 남동쪽 갈대숲 길이 열립니다.
// 숲처럼 농장 씬 안 멀리(x 250~280)에 실행 중에 만듭니다.
//  - 호수는 회색으로 흐려져 있습니다. 서쪽 돌계단에서 E: 잡은 물고기를 놓아주면 맑기가 오르고
//    구역마다 색이 돌아옵니다 (회색 바닥 위 컬러 바닥을 둥근 마스크로). 다 맑아지면 밤에 반딧불이.
//  - 부두 끝과 동쪽 물가에서 낚시 (호수 물고기: 블루길, 밤의 문핀, 맑은 물의 유리고기)
//  - 버드나무 아래 벤치에서 E: 하루 한 번 마음 +2
//  - 아침마다 물가에 산딸기, 물냉이가 생깁니다.
public class LakeArea : MonoBehaviour
{
    public static LakeArea Instance { get; private set; }
    public static MapData Map => MapData.Load("lake");

    public const float RestHeart = 2f;
    public const float ZoneHeart = 3f;
    public const float ClearHeart = 5f;

    [System.Serializable]
    public class Zone { public float x, y; public string size; }
    [System.Serializable]
    class Zones { public Zone[] zones = new Zone[0]; }

    private MapData map;
    private LakeState state;
    private Material material;
    private SpriteRenderer colorGround;
    private readonly List<GameObject> masks = new List<GameObject>();
    private readonly List<Transform> fireflies = new List<Transform>();
    private Vector2 steps, bench;

    static DataManager Data => DataManager.instance;

    // ---------------------------------------------------------------- 규칙 (DataManager가 부름)
    public static bool IsOpen => Data != null && Data.story.lake != null && Data.story.lake.open;

    public static bool CheckOpen(DataManager data)
    {
        if (data.story.lake == null) data.story.lake = new LakeState();
        LakeState l = data.story.lake;
        l.Ensure();
        if (l.open || data.CurrentSeason < Season.Summer) return false;
        l.open = true;
        data.story.Unlock("lake_open");
        return true;
    }

    public static List<(int x, int y)> ForageSpots(MapData lakeMap)
    {
        var list = new List<(int x, int y)>();
        if (lakeMap == null) return list;
        foreach (var cell in lakeMap.Cells()) if (cell.Value == 'f') list.Add((cell.Key.x, cell.Key.y));
        return list;
    }

    public static void EnsureForage(DataManager data, int day, bool force)
    {
        LakeState l = data.story.lake;
        if (l == null || !l.open || Map == null) return;
        if (!force && l.forageDay == day) return;
        l.RollForage(day, data.CurrentSeason, data.heart, ForageSpots(Map), new System.Random(day * 6151 + 3));
    }

    // 하루가 끝날 때: 길이 열리는지, 새 채집물
    public static void Morning(DataManager data, int day, out string headline)
    {
        headline = null;
        if (CheckOpen(data))
            headline = "<color=#8fd0ff>The gray reeds south-east of the farm turned green and parted.</color>\nA path leads down to the lake.";
        if (data.story.lake.open) EnsureForage(data, day, true);
    }

    // ---------------------------------------------------------------- 만들기 (FarmMap이 부름)
    public static LakeArea Build(Scene scene, Material material)
    {
        MapData lakeMap = Map;
        if (lakeMap == null || Data == null || !IsOpen) return null;
        var go = new GameObject("Lake");
        SceneManager.MoveGameObjectToScene(go, scene);
        LakeArea area = go.AddComponent<LakeArea>();
        area.Setup(lakeMap, material);
        return area;
    }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    static Zone[] ReadZones()
    {
        TextAsset text = Resources.Load<TextAsset>("Maps/lake");
        Zones z = text != null ? JsonUtility.FromJson<Zones>(text.text) : null;
        return z != null && z.zones != null ? z.zones : new Zone[0];
    }

    void Setup(MapData lakeMap, Material mat)
    {
        map = lakeMap;
        material = mat;
        state = Data.story.lake;
        state.Ensure();
        EnsureForage(Data, AreaKit.Today, false);

        Vector2 center = map.Center;
        AreaKit.Ground(transform, "bg_lake_gray", center, -50, SpriteMaskInteraction.None, material);
        colorGround = AreaKit.Ground(transform, "bg_lake", center, -49, SpriteMaskInteraction.VisibleInsideMask, material);
        AreaKit.Walls(transform, map, ch => ch == '#' || ch == '~' || ch == 'R' || ch == 'W' || ch == 'B');

        foreach (Zone z in ReadZones())
            masks.Add(AreaKit.Mask(transform, z.size == "l" ? "lake_mask_l" : "lake_mask_s", new Vector2(z.x, z.y)));

        foreach (var cell in map.Cells())
        {
            Vector2 mid = new Vector2(cell.Key.x + 0.5f, cell.Key.y + 0.5f);
            switch (cell.Value)
            {
                case 'X': steps = mid; break;
                case 'h': bench = mid; break;
                case 'F':
                    FishingSpot spot = FishingSpot.Create(mid, 1.3f);
                    spot.lake = true;
                    spot.transform.SetParent(transform, true);
                    break;
            }
        }
        foreach (string f in state.forage) AreaKit.SpawnForage(transform, f, material, "lake");

        if (map.exits != null)
            foreach (MapExit exit in map.exits)
                Portal(exit);

        Sprite fly = PixelArt.Load("World/firefly");
        // 반딧불이는 밤의 어둠(Global Light 2D)에 묻히지 않게 빛을 받지 않는 재질로 그립니다. (없으면 기본 재질)
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        Material glow = unlit != null ? new Material(unlit) : material;
        for (int i = 0; i < 16; i++)
        {
            var go = new GameObject("Firefly");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = fly;
            if (glow != null) sr.sharedMaterial = glow;
            sr.sortingOrder = 30;
            if (fly != null) go.transform.localScale = Vector3.one * (PixelArt.PixelsPerUnit / fly.pixelsPerUnit);
            fireflies.Add(go.transform);
        }
        Refresh();
    }

    void Portal(MapExit exit)
    {
        AreaPortal back = AreaPortal.Create("LakeExit", exit.Area, new Vector2(exit.spawnX, exit.spawnY));
        back.transform.SetParent(transform, true);
    }

    void Refresh()
    {
        int zones = state.ZonesClear;
        for (int i = 0; i < masks.Count; i++) masks[i].SetActive(i < zones);
        if (colorGround != null) colorGround.maskInteraction = state.IsClear ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
    }

    void Update()
    {
        UpdateFireflies();
        if (!AreaKit.PressedE()) return;
        DataManager data = Data;
        PlayerAction player = PlayerAction.instance;
        if (data == null || player == null || data.IsGameOver) return;
        Vector2 p = player.transform.position;

        if (Vector2.Distance(p, steps) < 1.3f) { ReleaseFish(data); return; }
        if (Vector2.Distance(p, bench) < 1.3f)
        {
            if (state.Rest(AreaKit.Today)) data.ChangeHeart(RestHeart, "You sat under the willow and watched the water.");
            else HUD.Toast("The willow sways. You've already rested here today.", 2.5f);
        }
    }

    // 돌계단: 가진 물고기를 (황금 잉어 빼고) 놓아줄지 묻습니다.
    void ReleaseFish(DataManager data)
    {
        var parts = new List<string>();
        int count = 0, points = 0;
        foreach (string fish in LakeState.Releasable)
        {
            int n = data.GetCount(fish);
            if (n <= 0) continue;
            parts.Add($"{fish} x{n}");
            count += n;
            points += n * LakeState.Points(fish);
        }
        if (count == 0)
        {
            HUD.Toast(state.IsClear ? "The lake is clear and calm. You can still let fish go here anytime." :
                $"Stone steps down to the gray water. Catch some fish and let them go here. ({state.ToNextZone()} to the next part)", 3.5f);
            return;
        }
        HUD.Ask("The lake", $"Let these fish go?\n{string.Join(", ", parts)}\n<size=22>(Golden Koi stays with you)</size>", null,
            () => DoRelease(data, count, points), null, "Let them go", "Keep them");
    }

    void DoRelease(DataManager data, int count, int points)
    {
        foreach (string fish in LakeState.Releasable)
        {
            int n = data.GetCount(fish);
            if (n > 0) data.RemoveItem(fish, n);
        }
        List<int> zones = state.Release(count, points, out bool clear);
        HUD.Toast($"The fish slip back into the water. The lake feels a little lighter. (+{points})", 3f);
        foreach (int z in zones)
            data.ChangeHeart(ZoneHeart, $"The {LakeState.ZoneNames[z]} runs clear and blue again.");
        if (clear)
        {
            data.ChangeHeart(ClearHeart, "The whole lake is clear.");
            HUD.Announce("<size=42><color=#8fd0ff>The lake is clear</color></size>\n\nYou can see the pebbles on the bottom.\nWhen night falls, fireflies come out over the water.", 6f);
        }
        Refresh();
    }

    void UpdateFireflies()
    {
        int hour = GameTimeManager.instance != null ? GameTimeManager.instance.hour : 12;
        DataManager data = Data;
        bool on = state != null && state.IsClear && (hour >= 20 || hour < 4) && data != null && data.CurrentSeason != Season.Winter;
        Vector2 c = map != null ? map.Center : Vector2.zero;
        for (int i = 0; i < fireflies.Count; i++)
        {
            Transform f = fireflies[i];
            if (f.gameObject.activeSelf != on) f.gameObject.SetActive(on);
            if (!on) continue;
            float t = Time.time * 0.35f + i * 1.7f;
            float x = c.x - 9f + (i * 37 % 18) + Mathf.Sin(t) * 1.2f;
            float y = c.y - 5f + (i * 23 % 10) + Mathf.Sin(t * 1.3f + i) * 0.8f;
            f.position = new Vector3(Mathf.Round(x * 32f) / 32f, Mathf.Round(y * 32f) / 32f, 0f);
        }
    }
}
