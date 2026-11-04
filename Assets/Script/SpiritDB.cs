using System.Collections.Generic;

// 정령에게 먹일 수 있는 것과 색 기운, 성장 단계, 모습, 능력 계산 (순수 로직)
public static class SpiritDB
{
    public const int YoungXp = 8;
    public const int GrownXp = 25;
    public static readonly string[] Colors = { "sun", "earth", "water", "bloom" };

    // 아이템 → (햇빛, 대지, 물, 꽃)
    static readonly Dictionary<string, int[]> food = new Dictionary<string, int[]>
    {
        { "Sunshine Shard", new[] { 3, 0, 0, 0 } },
        { "Sunflower", new[] { 2, 0, 0, 0 } },
        { "Corn", new[] { 1, 0, 0, 0 } },
        { "Earth's Breath", new[] { 0, 3, 0, 0 } },
        { "Carrot", new[] { 0, 1, 0, 0 } },
        { "Potato", new[] { 0, 1, 0, 0 } },
        { "Minnow", new[] { 0, 0, 1, 0 } },
        { "Carp", new[] { 0, 0, 2, 0 } },
        { "Golden Koi", new[] { 0, 0, 5, 0 } },
        { "Grilled Fish", new[] { 0, 0, 2, 0 } },
        { "Apple", new[] { 0, 0, 0, 1 } },
        { "Strawberry", new[] { 0, 0, 0, 1 } },
        { "Strawberry Jam", new[] { 0, 0, 0, 3 } },
        { "Carrot Cake", new[] { 0, 1, 0, 2 } },
        { "Star Fragment", new[] { 1, 1, 1, 1 } },
    };

    public static bool CanEat(string item) => !string.IsNullOrEmpty(item) && food.ContainsKey(item);

    public static int[] PointsFor(string item) => food.TryGetValue(item, out int[] p) ? p : new int[4];

    public static int Get(SpiritState s, int i)
    {
        switch (i) { case 0: return s.sun; case 1: return s.earth; case 2: return s.water; default: return s.bloom; }
    }

    // 먹이기: 색 기운과 경험치를 더합니다.
    public static void Feed(SpiritState s, string item)
    {
        int[] p = PointsFor(item);
        s.sun += p[0]; s.earth += p[1]; s.water += p[2]; s.bloom += p[3];
        s.xp += p[0] + p[1] + p[2] + p[3];
    }

    public static int Stage(SpiritState s) => s.xp >= GrownXp ? 2 : (s.xp >= YoungXp ? 1 : 0);

    // 가장 많이 먹은 색 (동점이면 먼저 정의된 색)
    public static string Dominant(SpiritState s)
    {
        int best = -1, bestValue = 0;
        for (int i = 0; i < 4; i++)
        {
            if (Get(s, i) > bestValue) { bestValue = Get(s, i); best = i; }
        }
        return best < 0 ? null : Colors[best];
    }

    // 네 색을 고르게 먹인 다 큰 정령 = 프리즘
    public static bool IsPrism(SpiritState s)
    {
        if (Stage(s) < 2) return false;
        int min = int.MaxValue, max = 0;
        for (int i = 0; i < 4; i++)
        {
            int v = Get(s, i);
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return max > 0 && min * 2 >= max;
    }

    public static string FormKey(SpiritState s)
    {
        int stage = Stage(s);
        string dom = Dominant(s);
        if (stage == 0 || dom == null) return "spirit_gray";
        if (IsPrism(s)) return "spirit_prism";
        return $"spirit_{dom}_{(stage == 1 ? "young" : "grown")}";
    }

    public static string FormName(string key)
    {
        switch (key)
        {
            case "spirit_gray": return "Gray Wisp";
            case "spirit_prism": return "Prism Spirit";
            case "spirit_sun_young": return "Sunny Sprite";
            case "spirit_sun_grown": return "Sun Spirit";
            case "spirit_earth_young": return "Sprout Sprite";
            case "spirit_earth_grown": return "Grove Spirit";
            case "spirit_water_young": return "Droplet Sprite";
            case "spirit_water_grown": return "River Spirit";
            case "spirit_bloom_young": return "Petal Sprite";
            case "spirit_bloom_grown": return "Blossom Spirit";
            default: return key;
        }
    }

    public static readonly string[] AllForms =
    {
        "spirit_gray", "spirit_sun_young", "spirit_sun_grown", "spirit_earth_young", "spirit_earth_grown",
        "spirit_water_young", "spirit_water_grown", "spirit_bloom_young", "spirit_bloom_grown", "spirit_prism",
    };

    // 능력 단계: 0 없음, 1 어린 정령(또는 프리즘), 2 다 큰 정령
    public static int PerkLevel(SpiritState s, string color)
    {
        if (s == null || !s.found) return 0;
        if (IsPrism(s)) return 1;
        return Dominant(s) == color ? Stage(s) : 0;
    }

    public static string PerkText(SpiritState s)
    {
        if (IsPrism(s)) return "A little of every color's blessing";
        switch (Dominant(s))
        {
            case "sun": return Stage(s) > 0 ? $"Max energy +{10 * Stage(s)}%" : "";
            case "earth": return Stage(s) > 0 ? $"Watered crops sometimes grow an extra day ({10 * Stage(s)}%)" : "";
            case "water": return Stage(s) > 0 ? "Better fishing (more Golden Koi, longer bites)" : "";
            case "bloom": return Stage(s) > 0 ? $"+{Stage(s)} Heart every morning" : "";
            default: return "";
        }
    }
}
