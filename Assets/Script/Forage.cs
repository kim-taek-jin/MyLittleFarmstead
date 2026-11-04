using System.Collections.Generic;

// 숲과 호수가 함께 쓰는 채집 규칙. 채집물은 "x,y,아이템" 문자열 목록으로 저장됩니다.
public static class Forage
{
    // 오늘의 채집물 정하기: 자리를 겹치지 않게 count곳 고르고, 가중치대로 아이템을 정함. firstItem이 있으면 첫 자리는 그것
    public static void Roll(List<string> list, IList<(int x, int y)> spots, int count, System.Random rng,
                            string[] items, int[] weights, string firstItem = null)
    {
        list.Clear();
        if (spots == null || spots.Count == 0) return;
        var pool = new List<(int x, int y)>(spots);
        count = System.Math.Min(pool.Count, count);
        for (int k = 0; k < count; k++)
        {
            int pick = rng.Next(pool.Count);
            var spot = pool[pick];
            pool.RemoveAt(pick);
            string item = firstItem != null && k == 0 ? firstItem : Pick(rng, items, weights);
            list.Add($"{spot.x},{spot.y},{item}");
        }
    }

    static string Pick(System.Random rng, string[] items, int[] weights)
    {
        int total = 0;
        foreach (int w in weights) total += w;
        int roll = rng.Next(total);
        for (int i = 0; i < items.Length; i++)
        {
            if (roll < weights[i]) return items[i];
            roll -= weights[i];
        }
        return items[0];
    }

    // 그 칸의 채집물을 주움 (없으면 null)
    public static string Take(List<string> list, int x, int y)
    {
        string prefix = $"{x},{y},";
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].StartsWith(prefix)) continue;
            string item = list[i].Substring(prefix.Length);
            list.RemoveAt(i);
            return item;
        }
        return null;
    }

    public static bool Has(List<string> list, string item)
    {
        foreach (string f in list) if (f.EndsWith("," + item)) return true;
        return false;
    }
}
