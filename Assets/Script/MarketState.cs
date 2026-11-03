using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public string itemName;
    public int count;

    public ItemStack() { }
    public ItemStack(string name, int amount) { itemName = name; count = amount; }
}

// 살아있는 시장: 오늘의 수요 작물, 많이 팔수록 떨어지는 가격, 흥정 보너스
[System.Serializable]
public class MarketState
{
    public string demandItem = "Carrot";      // 오늘 시장에서 찾는 작물 (+50%)
    public float haggleBonus = 0f;             // 흥정 성공 시 오늘 판매가 보너스 (0.25 = +25%)
    public bool haggledToday = false;
    public List<ItemStack> soldToday = new List<ItemStack>();

    public const float DemandBonus = 0.5f;
    public const float SaturationPerSale = 0.04f; // 같은 아이템을 하나 팔 때마다 -4%
    public const float MinSaturation = 0.5f;

    public int GetSoldToday(string itemName)
    {
        foreach (var s in soldToday) if (s.itemName == itemName) return s.count;
        return 0;
    }

    public void RecordSale(string itemName)
    {
        foreach (var s in soldToday)
        {
            if (s.itemName == itemName) { s.count++; return; }
        }
        soldToday.Add(new ItemStack(itemName, 1));
    }

    public int GetSellPrice(string itemName)
    {
        ItemDef def = ItemDB.Get(itemName);
        if (def == null || !ItemDB.CanSell(itemName)) return 0;

        float price = def.sellPrice;
        if (itemName == demandItem) price *= 1f + DemandBonus;
        price *= 1f + haggleBonus;
        price *= Mathf.Max(MinSaturation, 1f - SaturationPerSale * GetSoldToday(itemName));
        return Mathf.Max(1, Mathf.RoundToInt(price));
    }

    public void StartNewDay(System.Random rng)
    {
        List<string> candidates = new List<string>();
        foreach (var crop in CropDB.All) candidates.Add(crop.harvestItem);
        demandItem = candidates[rng.Next(candidates.Count)];
        haggleBonus = 0f;
        haggledToday = false;
        soldToday.Clear();
    }
}

// 하루 동안의 생활 기록 (잠들 때 '균형 잡힌 하루'인지 평가하는 데 사용)
[System.Serializable]
public class DayLog
{
    public int harvested;
    public int mealsEaten;
    public bool exhausted;      // 에너지를 바닥까지 썼는지
    public bool starved;        // 배고픔/갈증이 0이 된 적 있는지
    public bool drankWater;
}
