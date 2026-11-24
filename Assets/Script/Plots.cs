using System.Collections.Generic;

// 텃밭 뙈기 (Assets/Resources/Maps/farm.json의 "plots"). 유니티에 의존하지 않는 규칙만 담았습니다.
//  - 밭은 정해진 뙈기 안의 이랑에서만 짓습니다 (넓은 흙 아무 데나 괭이질하던 방식 대신).
//  - 처음에는 한 뙈기만 열려 있고, 나머지는 회색으로 굳은 묵정밭입니다. 퇴비를 뿌리면 되살아납니다.
//  - 땅도 쉬어야 한다 (휴경): 작물 없이 하룻밤을 비워 둔 뙈기는 땅심이 올라서,
//    다음에 심는 작물이 한 개 더 열립니다 (첫 수확에만).
//  - 퇴비 더미: 작물, 열매, 나물, 음식 같은 걸 넣으면 두 개마다 다음 날 아침 퇴비 한 포대가 됩니다.
[System.Serializable]
public class MapPlot
{
    public string id;
    public int x0, y0, x1, y1;     // 칸 좌표 (양 끝 포함)
    public bool startOpen;         // 처음부터 열린 뙈기

    public bool Contains(int x, int y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
    public int Width => x1 - x0 + 1;
    public int Height => y1 - y0 + 1;
    public int CellCount => Width * Height;
    // 되살리는 데 드는 퇴비: 세 칸에 한 포대 (최소 2)
    public int RestoreCost => System.Math.Max(2, (CellCount + 2) / 3);
}

public static class Plots
{
    public const int MaxCompostReady = 20;

    public static MapPlot At(IEnumerable<MapPlot> plots, int x, int y)
    {
        if (plots == null) return null;
        foreach (MapPlot p in plots) if (p != null && p.Contains(x, y)) return p;
        return null;
    }

    public static bool IsOpen(FarmData farm, MapPlot plot) =>
        plot != null && (plot.startOpen || (farm.openPlots != null && farm.openPlots.Contains(plot.id)));

    // 이 칸에 농사를 지을 수 있는지 (열린 뙈기 안)
    public static bool CanFarm(FarmData farm, IEnumerable<MapPlot> plots, int x, int y) => IsOpen(farm, At(plots, x, y));

    public static bool HasCrops(FarmData farm, MapPlot plot)
    {
        foreach (CropData c in farm.crops) if (plot.Contains(c.cellX, c.cellY)) return true;
        return false;
    }

    public static bool IsRested(FarmData farm, MapPlot plot) =>
        plot != null && farm.restedPlots != null && farm.restedPlots.Contains(plot.id);

    // 퇴비로 묵정밭 되살리기. 성공하면 쓴 퇴비 수, 실패하면 0
    public static int Restore(FarmData farm, MapPlot plot, int compostHave)
    {
        if (plot == null || IsOpen(farm, plot) || compostHave < plot.RestoreCost) return 0;
        if (farm.openPlots == null) farm.openPlots = new List<string>();
        farm.openPlots.Add(plot.id);
        // 갓 되살린 땅은 푹 쉰 땅이라 첫 작물부터 땅심 보너스
        if (farm.restedPlots == null) farm.restedPlots = new List<string>();
        if (!farm.restedPlots.Contains(plot.id)) farm.restedPlots.Add(plot.id);
        return plot.RestoreCost;
    }

    // 아침마다: 작물 없이 밤을 보낸 열린 뙈기는 쉰 땅이 되고, 작물이 있으면 쉰 표시가 사라집니다.
    // 반환값: 오늘 새로 쉰 땅이 된 뙈기 수
    public static int AdvanceNight(FarmData farm, IEnumerable<MapPlot> plots)
    {
        if (plots == null) return 0;
        if (farm.restedPlots == null) farm.restedPlots = new List<string>();
        int newly = 0;
        foreach (MapPlot p in plots)
        {
            if (!IsOpen(farm, p)) continue;
            bool rested = farm.restedPlots.Contains(p.id);
            if (HasCrops(farm, p))
            {
                if (rested) farm.restedPlots.Remove(p.id);
            }
            else if (!rested)
            {
                farm.restedPlots.Add(p.id);
                newly++;
            }
        }
        // 퇴비 더미: 두 개마다 퇴비 한 포대
        int made = farm.compostPoints / 2;
        farm.compostPoints -= made * 2;
        farm.compostReady = System.Math.Min(MaxCompostReady, farm.compostReady + made);
        return newly;
    }

    // 예전 저장 파일: 열린 뙈기 밖의 갈린 흙과 작물을 지웁니다. 지운 작물은 씨앗으로 돌려줍니다 (refunds에 씨앗 이름).
    public static int Migrate(FarmData farm, IEnumerable<MapPlot> plots, List<string> refunds)
    {
        if (plots == null) return 0;
        int removed = 0;
        for (int i = farm.crops.Count - 1; i >= 0; i--)
        {
            CropData c = farm.crops[i];
            if (CanFarm(farm, plots, c.cellX, c.cellY)) continue;
            CropDef def = c.Def;
            if (refunds != null && def != null && !string.IsNullOrEmpty(def.seedItem)) refunds.Add(def.seedItem);
            farm.crops.RemoveAt(i);
            removed++;
        }
        farm.tilledTiles.RemoveAll(t => !CanFarm(farm, plots, t.cellX, t.cellY));
        return removed;
    }

    // ---------------------------------------------------------------- 퇴비 더미
    // 퇴비가 되는 것: 작물, 열매, 나물, 버섯, 도토리, 음식 (물고기, 도구, 씨앗, 정수, 돌, 나무는 안 됨)
    static readonly HashSet<string> Organic = new HashSet<string>
    {
        "Carrot", "Potato", "Corn", "Strawberry", "Sunflower", "Pumpkin", "Apple", "Pear", "Persimmon",
        "Wild Garlic", "Fern Sprout", "Morel", "Wild Raspberry", "Watercress", "Acorn",
        "Bread", "Roasted Carrot", "Baked Potato", "Corn Soup", "Spring Greens",
    };

    public static bool IsOrganic(string item) => item != null && Organic.Contains(item);

    // 퇴비 더미에 넣기. 넣은 개수를 돌려줍니다 (오늘 밤에 퇴비가 됨)
    public static int AddToHeap(FarmData farm, string item, int count)
    {
        if (!IsOrganic(item) || count <= 0) return 0;
        farm.compostPoints += count;
        return count;
    }

    // 다 된 퇴비 꺼내기
    public static int TakeCompost(FarmData farm)
    {
        int n = farm.compostReady;
        farm.compostReady = 0;
        return n;
    }
}
