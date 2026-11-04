using System.Collections.Generic;

// 숲의 상태 (저장됨). 유니티에 의존하지 않는 규칙만 담아서 테스트할 수 있게 했습니다.
//  - 빈터 3곳: 도토리를 심고(0) 물 준 날마다 한 단계씩 자라서 3이 되면 그 빈터에 색이 돌아옵니다.
//    비 오는 날은 저절로 물을 준 것으로 칩니다.
//  - 3곳이 모두 되살아나면 큰 나무가 깨어나고 숲 전체에 색이 돌아옵니다.
//  - 채집물은 아침마다 정해진 자리 중 몇 곳에 새로 생깁니다. 마음이 높을수록 많이 보입니다.
[System.Serializable]
public class ForestState
{
    public const int Clearings = 3;
    public const int RestoredStage = 3;
    public static readonly string[] ClearingNames = { "north", "west", "south" };

    public bool open;                                  // 숲길이 열렸는지 (봄이 오면, 한 번 열리면 계속)
    public int[] sapling = { -1, -1, -1 };             // -1 빈 자리, 0 도토리, 1 새싹, 2 묘목, 3 되살아남
    public bool[] watered = new bool[Clearings];       // 오늘 물을 받았는지
    public bool treeAwake;                             // 큰 나무가 깨어났는지
    public int forageDay = -1;                         // 채집물을 정한 날
    public List<string> forage = new List<string>();   // "x,y,아이템"
    public int restDay = -1;                           // 샘가에서 쉰 날

    // 예전 저장 파일처럼 배열이 비어 있을 때 바로잡기
    public void Ensure()
    {
        if (sapling == null || sapling.Length != Clearings) sapling = new[] { -1, -1, -1 };
        if (watered == null || watered.Length != Clearings) watered = new bool[Clearings];
        if (forage == null) forage = new List<string>();
    }

    public int Restored
    {
        get
        {
            Ensure();
            int n = 0;
            foreach (int s in sapling) if (s >= RestoredStage) n++;
            return n;
        }
    }

    public bool IsRestored(int i) { Ensure(); return sapling[i] >= RestoredStage; }
    public bool AnyEmptySpot { get { Ensure(); foreach (int s in sapling) if (s < 0) return true; return false; } }

    public bool Plant(int i)
    {
        Ensure();
        if (i < 0 || i >= Clearings || sapling[i] >= 0) return false;
        sapling[i] = 0;
        watered[i] = false;
        return true;
    }

    public bool NeedsWater(int i) { Ensure(); return sapling[i] >= 0 && sapling[i] < RestoredStage && !watered[i]; }

    public bool Water(int i)
    {
        if (!NeedsWater(i)) return false;
        watered[i] = true;
        return true;
    }

    // 아침: 물 받은 묘목이 한 단계씩 자람. 새로 되살아난 빈터 번호를 돌려주고, 큰 나무가 깨어났는지 알려줌
    public List<int> AdvanceDay(bool rain, out bool treeWoke)
    {
        Ensure();
        var restored = new List<int>();
        for (int i = 0; i < Clearings; i++)
        {
            if (sapling[i] >= 0 && sapling[i] < RestoredStage && (watered[i] || rain))
            {
                sapling[i]++;
                if (sapling[i] == RestoredStage) restored.Add(i);
            }
            watered[i] = false;
        }
        treeWoke = false;
        if (!treeAwake && Restored == Clearings)
        {
            treeAwake = true;
            treeWoke = true;
        }
        return restored;
    }

    // ---- 채집 ----
    public static readonly string[] ForageItems = { "Wild Garlic", "Fern Sprout", "Morel" };
    static readonly int[] ForageWeights = { 45, 35, 20 };

    public static int ForageCount(Season season, float heart)
    {
        if (season == Season.Winter) return 2;   // 꽃샘추위: 숲도 조용해짐
        return System.Math.Min(7, 3 + (int)(heart / 25f));
    }

    // 오늘의 채집물 정하기. wantAcorn이면 한 자리는 도토리 (빈 묘목 자리가 있고 도토리가 없을 때)
    public void RollForage(int day, Season season, float heart, IList<(int x, int y)> spots, System.Random rng, bool wantAcorn)
    {
        Ensure();
        forageDay = day;
        Forage.Roll(forage, spots, ForageCount(season, heart), rng, ForageItems, ForageWeights, wantAcorn ? "Acorn" : null);
    }

    // 그 칸의 채집물을 주움 (없으면 null)
    public string TakeForage(int x, int y) { Ensure(); return Forage.Take(forage, x, y); }

    public bool HasForage(string item) { Ensure(); return Forage.Has(forage, item); }

    // 쉼터: 하루 한 번
    public bool Rest(int day)
    {
        if (restDay == day) return false;
        restDay = day;
        return true;
    }
}
