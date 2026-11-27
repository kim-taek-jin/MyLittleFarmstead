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
    public bool snowing;
    public int waterCharges;
}

// JSON 파일 저장/불러오기 (Application.persistentDataPath/save.json)
public static class SaveSystem
{
    // 체험판은 따로 저장 (정식판 저장과 섞이지 않게, K-5)
    public static string SavePath => Path.Combine(Application.persistentDataPath, DemoConfig.IsDemo ? "save_demo.json" : "save.json");

    public static string BackupPath => SavePath + ".bak";
    public static bool HasSave() => File.Exists(SavePath) || File.Exists(BackupPath);

    public static void Save(SaveData data)
    {
        try
        {
            // F-0: 먼저 임시 파일에 쓰고, 예전 저장은 .bak으로 남긴 뒤 바꿔 넣음 (쓰다가 꺼져도 저장이 깨지지 않게)
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);
            File.Copy(tmp, SavePath, true);
            File.Delete(tmp);
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
        SaveData d = Read(SavePath);
        if (d == null && File.Exists(BackupPath))
        {
            Debug.LogWarning("[SaveSystem] The save could not be read. Loading the backup from the day before.");
            d = Read(BackupPath);
        }
        return d;
    }

    static SaveData Read(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed (" + Path.GetFileName(path) + "): " + e.Message);
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
    }
}
