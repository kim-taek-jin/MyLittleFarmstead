using System.Collections.Generic;

// 노을 언덕의 상태 (저장됨). 유니티에 의존하지 않는 규칙만 담았습니다.
//  - 마음이 가을이 된 아침, 농장 남쪽 언덕길이 열립니다. (한 번 열리면 계속)
//  - 늙은 과일나무 6그루 (맵 순서대로): 0 회색 → 낫으로 가지치기(1) → 대지의 숨결(2, 되살아남).
//    되살아난 나무는 FruitEvery일마다 열매를 맺습니다 (겨울 제외). 6그루 모두 되살리면 과수원 전체가 컬러.
//  - 곶감 걸이: 감을 걸어 두면 DryDays일 뒤 곶감이 됩니다 (한 번에 RackSize개까지).
[System.Serializable]
public class HillState
{
    public const int Trees = 6;
    public const int FruitEvery = 3;
    public const int FruitPerHarvest = 2;
    public const int DryDays = 3;
    public const int RackSize = 8;

    public bool open;
    public int[] tree = new int[Trees];                         // 0 회색, 1 가지치기함, 2 되살아남
    public int[] fruitDay = { -1, -1, -1, -1, -1, -1 };          // 열매가 익는 날
    public List<int> drying = new List<int>();                  // 걸어 둔 감마다 곶감이 되는 날
    public int restDay = -1;                                    // 전망대 벤치에서 쉰 날

    public void Ensure()
    {
        if (tree == null || tree.Length != Trees) tree = new int[Trees];
        if (fruitDay == null || fruitDay.Length != Trees) fruitDay = new[] { -1, -1, -1, -1, -1, -1 };
        if (drying == null) drying = new List<int>();
    }

    public int Revived
    {
        get
        {
            Ensure();
            int n = 0;
            foreach (int t in tree) if (t >= 2) n++;
            return n;
        }
    }

    public bool AllRevived => Revived == Trees;
    public bool IsAlive(int i) { Ensure(); return tree[i] >= 2; }

    public bool Prune(int i)
    {
        Ensure();
        if (tree[i] != 0) return false;
        tree[i] = 1;
        return true;
    }

    // 가지치기한 나무에 대지의 숨결 → 되살아남. 첫 열매는 FruitEvery일 뒤
    public bool Revive(int i, int day)
    {
        Ensure();
        if (tree[i] != 1) return false;
        tree[i] = 2;
        fruitDay[i] = day + FruitEvery;
        return true;
    }

    public bool HasFruit(int i, int day, Season season)
    {
        Ensure();
        return tree[i] >= 2 && season != Season.Winter && fruitDay[i] >= 0 && day >= fruitDay[i];
    }

    // 열매 따기: 딴 개수 (없으면 0). 다음 열매는 FruitEvery일 뒤
    public int Harvest(int i, int day, Season season)
    {
        if (!HasFruit(i, day, season)) return 0;
        fruitDay[i] = day + FruitEvery;
        return FruitPerHarvest;
    }

    public static string FruitOf(char kind) => kind == 'A' ? "Apple" : kind == 'P' ? "Pear" : kind == 'K' ? "Persimmon" : null;
    public static string KindName(char kind) => kind == 'A' ? "apple" : kind == 'P' ? "pear" : "persimmon";

    // ---- 곶감 ----
    public int Room { get { Ensure(); return RackSize - drying.Count; } }

    // 감을 건다: 실제로 건 개수
    public int Hang(int count, int day)
    {
        Ensure();
        int n = System.Math.Max(0, System.Math.Min(count, Room));
        for (int k = 0; k < n; k++) drying.Add(day + DryDays);
        return n;
    }

    public int ReadyCount(int day)
    {
        Ensure();
        int n = 0;
        foreach (int d in drying) if (day >= d) n++;
        return n;
    }

    // 다 마른 곶감을 거둠: 개수
    public int Collect(int day)
    {
        Ensure();
        int n = drying.RemoveAll(d => day >= d);
        return n;
    }

    public bool Rest(int day)
    {
        if (restDay == day) return false;
        restDay = day;
        return true;
    }
}
