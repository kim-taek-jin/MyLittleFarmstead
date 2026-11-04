using System.Collections.Generic;

// 밭 한 칸의 상태 (갈아둔 땅 / 젖은 땅)
[System.Serializable]
public class FarmTileData
{
    public int cellX;
    public int cellY;
    public bool wet;
}

// 농장 전체 상태. DataManager가 들고 있어서 씬을 이동해도 사라지지 않습니다.
[System.Serializable]
public class FarmData
{
    public List<FarmTileData> tilledTiles = new List<FarmTileData>();
    public List<CropData> crops = new List<CropData>();

    // --- 타일 ---
    public FarmTileData GetTile(int x, int y)
    {
        foreach (var t in tilledTiles) if (t.cellX == x && t.cellY == y) return t;
        return null;
    }

    public bool IsTilled(int x, int y) => GetTile(x, y) != null;

    public FarmTileData Till(int x, int y)
    {
        FarmTileData tile = GetTile(x, y);
        if (tile != null) return tile;
        tile = new FarmTileData { cellX = x, cellY = y, wet = false };
        tilledTiles.Add(tile);
        return tile;
    }

    // --- 작물 ---
    public CropData GetCrop(int x, int y)
    {
        foreach (var c in crops) if (c.cellX == x && c.cellY == y) return c;
        return null;
    }

    public CropData Plant(int x, int y, string cropId)
    {
        if (!IsTilled(x, y) || GetCrop(x, y) != null) return null;
        CropData crop = new CropData(x, y, cropId);
        crop.isWatered = GetTile(x, y).wet;
        crops.Add(crop);
        return crop;
    }

    // 물 주기: 갈아둔 땅이면 젖게 하고, 작물이 있으면 '오늘 물 받음' 처리
    public int FrozenCrops(Season season)
    {
        int n = 0;
        foreach (var c in crops) if (!c.Def.GrowsIn(season)) n++;
        return n;
    }

    // 내일 아침 다 자랄 작물 수 (오늘 물을 주면)
    public int RipeningTomorrow(Season season)
    {
        int n = 0;
        foreach (var c in crops)
        {
            if (!c.IsMature && c.Def.GrowsIn(season) && c.daysGrown + 1 >= c.Def.daysToMature) n++;
        }
        return n;
    }

    public int RipeNow()
    {
        int n = 0;
        foreach (var c in crops) if (c.IsMature) n++;
        return n;
    }

    public bool Water(int x, int y)
    {
        FarmTileData tile = GetTile(x, y);
        if (tile == null) return false;
        bool changed = !tile.wet;
        tile.wet = true;
        CropData crop = GetCrop(x, y);
        if (crop != null && !crop.isWatered)
        {
            crop.isWatered = true;
            changed = true;
        }
        return changed;
    }

    // 수확: 수확량을 돌려주고, 다시 자라는 작물이 아니면 밭에서 제거
    public int Harvest(CropData crop)
    {
        if (crop == null || !crop.IsMature) return 0;
        CropDef def = crop.Def;
        int amount = def.harvestAmount + (crop.blessed ? 1 : 0);

        if (def.regrowDays > 0)
        {
            crop.daysGrown = def.daysToMature - def.regrowDays;
            crop.blessed = false;
        }
        else
        {
            crops.Remove(crop);
        }
        return amount;
    }

    // 하루가 지날 때 호출. 어제 물을 받은 작물만 자라고, 비가 오면 오늘 밭 전체가 젖습니다.
    // 반환값: 오늘 새로 다 자란 작물 수
    // extraGrowthChance: 물 받은 작물이 하루 더 자랄 확률 (대지의 정령 능력)
    // season: 지금 마음의 계절. 그보다 높은 계절의 작물은 '얼어서' 자라지 않습니다 (시들지는 않음).
    public int AdvanceDay(bool rainToday, float extraGrowthChance = 0f, System.Random rng = null, Season season = Season.Autumn)
    {
        int newlyMature = 0;
        foreach (var crop in crops)
        {
            if (crop.isWatered && !crop.IsMature && crop.Def.GrowsIn(season))
            {
                crop.daysGrown++;
                if (!crop.IsMature && rng != null && rng.NextDouble() < extraGrowthChance) crop.daysGrown++;
                if (crop.IsMature) newlyMature++;
            }
            crop.isWatered = rainToday;
        }

        foreach (var tile in tilledTiles)
        {
            tile.wet = rainToday;
        }
        return newlyMature;
    }
}
