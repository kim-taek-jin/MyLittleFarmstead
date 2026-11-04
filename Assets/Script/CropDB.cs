using System.Collections.Generic;
using UnityEngine;

// 작물 하나의 정의
public class CropDef
{
    public string id;
    public string seedItem;
    public string harvestItem;
    public int daysToMature;      // 물을 준 날 수 기준으로 다 자라는 데 필요한 일수
    public int harvestAmount = 1;
    public int regrowDays;        // 0이면 수확 후 사라짐, 1 이상이면 그 일수 뒤 다시 열매
    public Color tint = Color.white; // 다 자랐을 때 스프라이트 색 (작물 구분용)
    public Season minSeason = Season.Winter; // 이 계절(마음) 이상에서만 심고 자랄 수 있음

    public bool GrowsIn(Season s) => s >= minSeason;
}

// 작물 데이터베이스. 새 작물은 여기와 ItemDB(씨앗/수확물)에 추가하면 됩니다.
public static class CropDB
{
    private static readonly Dictionary<string, CropDef> crops = new Dictionary<string, CropDef>();

    static CropDB()
    {
        Register(new CropDef { id = "carrot", seedItem = "Carrot Seed", harvestItem = "Carrot", daysToMature = 3, tint = Color.white });
        Register(new CropDef { id = "potato", seedItem = "Potato Seed", harvestItem = "Potato", daysToMature = 4, harvestAmount = 1, tint = new Color(0.85f, 0.75f, 0.55f) });
        Register(new CropDef { id = "corn", seedItem = "Corn Seed", harvestItem = "Corn", daysToMature = 5, regrowDays = 3, tint = new Color(1f, 0.95f, 0.45f), minSeason = Season.Summer });
        Register(new CropDef { id = "sunflower", seedItem = "Sunflower Seed", harvestItem = "Sunflower", daysToMature = 5, tint = new Color(1f, 0.95f, 0.4f), minSeason = Season.Summer });
        Register(new CropDef { id = "strawberry", seedItem = "Strawberry Seed", harvestItem = "Strawberry", daysToMature = 6, regrowDays = 3, tint = new Color(1f, 0.55f, 0.6f), minSeason = Season.Spring });
        Register(new CropDef { id = "pumpkin", seedItem = "Pumpkin Seed", harvestItem = "Pumpkin", daysToMature = 6, tint = new Color(1f, 0.7f, 0.4f), minSeason = Season.Autumn });
    }

    private static void Register(CropDef def) => crops[def.id] = def;

    public static CropDef Get(string id)
    {
        if (string.IsNullOrEmpty(id)) id = "carrot";
        return crops.TryGetValue(id, out CropDef def) ? def : crops["carrot"];
    }

    public static CropDef FromSeed(string seedItem)
    {
        ItemDef item = ItemDB.Get(seedItem);
        if (item == null || string.IsNullOrEmpty(item.cropId)) return null;
        return crops.TryGetValue(item.cropId, out CropDef def) ? def : null;
    }

    public static IEnumerable<CropDef> All => crops.Values;
}
