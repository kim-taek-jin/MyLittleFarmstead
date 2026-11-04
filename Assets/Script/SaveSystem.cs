using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 디스크에 저장되는 전체 게임 데이터
[System.Serializable]
public class SaveData
{
    public int version = 1;

    public int gold;
    public float health, hunger, thirst, energy, heart;
    public List<ItemStack> inventory = new List<ItemStack>();
    public List<ItemStack> storage = new List<ItemStack>();
    public FarmData farm = new FarmData();
    public MarketState market = new MarketState();
    public DayLog today = new DayLog();
    public StoryState story = new StoryState();
    public List<string> clearedResources = new List<string>();
    public List<int> heartMilestonesReached = new List<int>();

    public int day = 1;
    public int hour = 6;
    public float minute = 0f;
    public bool raining;
    public int waterCharges;
}

// JSON 파일 저장/불러오기 (Application.persistentDataPath/save.json)
public static class SaveSystem
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log("[SaveSystem] Saved: " + SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Save failed: " + e.Message);
        }
    }

    public static SaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed: " + e.Message);
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }
}
