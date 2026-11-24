using System.Collections.Generic;

// 마을 지도 (M키). 유니티에 의존하지 않는 규칙만 담았습니다 (화면은 PixelHUD.ShowMap).
//  - 아직 열리지 않은 곳(뒷산, 저수지, 노을 언덕, 별의 언덕)은 회색 안개에 덮여 있고, 마음의 계절이 오면 걷힙니다.
//  - 지역마다 한 줄로 "어디가 얼마나 회복됐나"를 보여 줍니다 (빈터, 맑은 구역, 과일나무, 텃밭 뙈기, 장날).
//  - 순간이동은 없습니다. 걸어 다니는 생활감을 지키려고요.
// 그림: Tools/pixelart/villagemap.py (World/vmap, World/vmap_fog_<id>, World/vmap_pin)
// 글자와 핀 자리: Assets/Resources/Maps/villagemap.json (같은 스크립트가 만듦)
public class VillageMapEntry
{
    public string id;
    public string name;
    public bool open;
    public string status;     // 한 줄: 회복 정도, 또는 언제 열리는지
    public string who;        // 그곳에 있는 주민 (없으면 빈 문자열)
}

public static class VillageMap
{
    public static readonly string[] Ids = { "farm", "market", "forest", "hill", "lake", "starhill" };

    public static string Name(string id)
    {
        switch (id)
        {
            case "farm": return Loc.T("Home", "우리 집");
            case "market": return Loc.T("Market", "장터");
            case "forest": return Loc.T("Back Hill", "뒷산");
            case "hill": return Loc.T("Sunset Hill", "노을 언덕");
            case "lake": return Loc.T("Reservoir", "저수지");
            case "starhill": return Loc.T("Star Hill", "별의 언덕");
            default: return id;
        }
    }

    public static bool IsOpen(string id, StoryState s)
    {
        switch (id)
        {
            case "forest": return s.forest != null && s.forest.open;
            case "lake": return s.lake != null && s.lake.open;
            case "hill": return s.hill != null && s.hill.open;
            case "starhill": return s.IsUnlocked("starhill");
            default: return true;
        }
    }

    // 안개를 그릴 지역 (지도 그림 World/vmap_fog_<id>)
    public static List<string> Fogged(StoryState s)
    {
        var list = new List<string>();
        foreach (string id in Ids) if (!IsOpen(id, s)) list.Add(id);
        return list;
    }

    public static string Status(string id, StoryState s, int day, int plotsOpen, int plotsTotal)
    {
        bool open = IsOpen(id, s);
        switch (id)
        {
            case "farm":
                return Loc.T($"Plots {plotsOpen}/{plotsTotal} open", $"살린 텃밭 {plotsOpen}/{plotsTotal}뙈기");
            case "market":
                if (MarketState.IsMarketDay(day)) return Loc.T("Market day today!", "오늘은 장날!");
                int n = MarketState.DaysUntilMarket(day);
                return n == 1 ? Loc.T("Market day tomorrow", "내일은 장날") : Loc.T($"Market day in {n} days", $"장날까지 {n}일");
            case "forest":
                if (!open) return Loc.T("When spring comes to your heart", "마음에 봄이 오면");
                return Loc.T($"Clearings {s.forest.Restored}/{ForestState.Clearings}", $"되살린 빈터 {s.forest.Restored}/{ForestState.Clearings}")
                       + (s.forest.treeAwake ? Loc.T(" · the zelkova is awake", " · 느티나무가 깨어남") : "");
            case "lake":
                if (!open) return Loc.T("When summer comes to your heart", "마음에 여름이 오면");
                return s.lake.IsClear ? Loc.T("Clear all the way down", "바닥까지 맑아요")
                     : Loc.T($"Clear parts {s.lake.ZonesClear}/{LakeState.Thresholds.Length}", $"맑은 구역 {s.lake.ZonesClear}/{LakeState.Thresholds.Length}");
            case "hill":
                if (!open) return Loc.T("When autumn comes to your heart", "마음에 가을이 오면");
                return Loc.T($"Fruit trees {s.hill.Revived}/{HillState.Trees}", $"살린 과일나무 {s.hill.Revived}/{HillState.Trees}");
            case "starhill":
                if (!open) return Loc.T($"Keep sharing (kindness {s.kindness}/{KindnessToOpen})", $"나눔을 이어 가면 (다정함 {s.kindness}/{KindnessToOpen})");
                return Loc.T("Clear nights after 20:00", "맑은 밤 20시 이후");
            default:
                return "";
        }
    }

    // StarHill.KindnessToUnlock과 같은 값 (StarHill은 유니티 쪽이라 여기서는 숫자만)
    public const int KindnessToOpen = 60;

    // 그곳에 사는 주민 이름 (지도 글자 아래 한 줄)
    public static string Who(string id)
    {
        string scene = id == "market" ? "Market" : id == "farm" ? "Farm" : null;
        if (scene == null) return "";
        var names = new List<string>();
        foreach (VillagerDef v in VillagerDB.All) if (v.scene == scene) names.Add(v.name);
        return string.Join(" · ", names);
    }

    public static List<VillageMapEntry> Entries(StoryState s, int day, int plotsOpen, int plotsTotal)
    {
        var list = new List<VillageMapEntry>();
        foreach (string id in Ids)
        {
            bool open = IsOpen(id, s);
            list.Add(new VillageMapEntry
            {
                id = id, name = Name(id), open = open,
                status = Status(id, s, day, plotsOpen, plotsTotal),
                who = open ? Who(id) : "",
            });
        }
        return list;
    }

    // 지금 서 있는 곳 (씬 이름 + 월드 좌표). 숲, 저수지, 언덕, 별의 언덕은 농장 씬 안 멀리 있습니다.
    public static string RegionAt(string scene, float x, float y)
    {
        if (scene == "Market" || scene == "MiniGame") return "market";
        if (scene != "Farm") return "farm";
        if (y >= 80f) return "starhill";
        if (x >= 300f) return "hill";
        if (x >= 240f) return "lake";
        if (x >= 180f) return "forest";
        return "farm";
    }

    // 지도 화면 아래쪽에 쓰는 한 줄 (오늘의 마음의 계절과 한 마디)
    public static string Footer(StoryState s)
    {
        int fog = Fogged(s).Count;
        if (fog == 0) return Loc.T("The fog is gone. The whole valley is yours to walk.", "안개가 모두 걷혔어요. 골짜기 어디든 걸어갈 수 있어요.");
        return Loc.T($"Fog still hangs over {fog} place(s). Take care of yourself and it will lift.",
                     $"아직 {fog}곳에 안개가 끼어 있어요. 나를 잘 돌보면 걷혀요.");
    }
}
