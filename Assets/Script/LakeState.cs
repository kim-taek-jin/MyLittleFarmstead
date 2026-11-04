using System.Collections.Generic;

// 호수의 상태 (저장됨). 유니티에 의존하지 않는 규칙만 담았습니다.
//  - 마음이 여름이 된 아침, 농장 남동쪽의 회색 갈대숲이 살아나며 호수 길이 열립니다. (한 번 열리면 계속)
//  - 호수는 회색으로 흐려져 있습니다. 잡은 물고기를 돌계단에서 놓아주면 맑기(clarity)가 오르고,
//    10 / 25 / 45를 넘을 때마다 서쪽 만 → 호수 가운데 → 동쪽 만 순서로 색이 돌아옵니다.
//    다 맑아지면 밤마다 반딧불이가 떠오릅니다.
//  - 호수 낚시: 블루길, 밤의 문핀, 물이 맑아야 보이는 유리고기.
[System.Serializable]
public class LakeState
{
    public static readonly int[] Thresholds = { 10, 25, 45 };
    public static readonly string[] ZoneNames = { "west cove", "middle of the lake", "east cove" };

    public bool open;
    public int clarity;             // 놓아준 물고기 점수의 합
    public int released;            // 놓아준 물고기 수
    public int restDay = -1;        // 버드나무 벤치에서 쉰 날
    public int forageDay = -1;
    public List<string> forage = new List<string>();

    public void Ensure() { if (forage == null) forage = new List<string>(); }

    public int ZonesClear
    {
        get
        {
            int n = 0;
            foreach (int t in Thresholds) if (clarity >= t) n++;
            return n;
        }
    }

    public bool IsClear => clarity >= Thresholds[Thresholds.Length - 1];

    // 물고기마다 맑기 점수 (놓아줄 수 없는 것은 0)
    public static int Points(string fish)
    {
        switch (fish)
        {
            case "Minnow": return 1;
            case "Carp": return 2;
            case "Bluegill": return 2;
            case "Moonfin": return 3;
            case "Glassfish": return 4;
            case "Golden Koi": return 5;
            default: return 0;
        }
    }

    // 함께 놓아줄 물고기 (황금 잉어는 실수로 놓아주지 않게 빼 둠)
    public static readonly string[] Releasable = { "Minnow", "Carp", "Bluegill", "Moonfin", "Glassfish" };

    // 놓아주기: 새로 맑아진 구역 번호와, 이번에 호수 전체가 맑아졌는지
    public List<int> Release(int fishCount, int points, out bool becameClear)
    {
        int before = ZonesClear;
        bool wasClear = IsClear;
        released += fishCount;
        clarity += points;
        var zones = new List<int>();
        for (int i = before; i < ZonesClear; i++) zones.Add(i);
        becameClear = !wasClear && IsClear;
        return zones;
    }

    // 다음 구역까지 남은 점수 (다 맑으면 0)
    public int ToNextZone()
    {
        foreach (int t in Thresholds) if (clarity < t) return t - clarity;
        return 0;
    }

    // ---- 호수 낚시 ----
    // roll: 0~1 난수, zonesClear: 맑아진 구역 수, hour: 시각, heart01: 마음 0~1
    public static string LakeCatch(double roll, int zonesClear, int hour, float heart01)
    {
        double koi = 0.02 + 0.05 * heart01;
        if (roll < koi) return "Golden Koi";
        roll -= koi;
        if (roll < 0.06) return "Old Boot";
        roll -= 0.06;
        if (zonesClear >= 2)
        {
            if (roll < 0.10) return "Glassfish";
            roll -= 0.10;
        }
        bool night = hour >= 19 || hour < 5;
        if (night)
        {
            if (roll < 0.25) return "Moonfin";
            roll -= 0.25;
        }
        if (roll < 0.40) return "Bluegill";
        return "Carp";
    }

    // ---- 채집 (여름 물가) ----
    public static readonly string[] ForageItems = { "Wild Raspberry", "Watercress" };
    static readonly int[] ForageWeights = { 55, 45 };

    public static int ForageCount(Season season, float heart)
    {
        if (season == Season.Winter) return 2;
        return System.Math.Min(6, 2 + (int)(heart / 25f));
    }

    public void RollForage(int day, Season season, float heart, IList<(int x, int y)> spots, System.Random rng)
    {
        Ensure();
        forageDay = day;
        Forage.Roll(forage, spots, ForageCount(season, heart), rng, ForageItems, ForageWeights);
    }

    public string TakeForage(int x, int y) { Ensure(); return Forage.Take(forage, x, y); }

    public bool Rest(int day)
    {
        if (restDay == day) return false;
        restDay = day;
        return true;
    }
}
