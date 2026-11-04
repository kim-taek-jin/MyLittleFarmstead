using System.Collections.Generic;

// 주민 한 명의 진행 상황
[System.Serializable]
public class VillagerProgress
{
    public string id;
    public int wishIndex;   // 이뤄준 소원 수 (= 색이 돌아온 단계, 0 = 회색)
    public bool met;        // 처음 인사를 나눴는지
}

// 이야기 진행 상태 (저장됨): 주민, 해금, 잡은 물고기, 이벤트 플래그
[System.Serializable]
public class StoryState
{
    public List<VillagerProgress> villagers = new List<VillagerProgress>();
    public List<string> unlocks = new List<string>();   // 레시피/상점/이벤트 플래그
    public List<ItemStack> fishCaught = new List<ItemStack>();

    public VillagerProgress Get(string id)
    {
        foreach (var v in villagers) if (v.id == id) return v;
        var progress = new VillagerProgress { id = id };
        villagers.Add(progress);
        return progress;
    }

    public bool IsUnlocked(string key) => !string.IsNullOrEmpty(key) && unlocks.Contains(key);

    public bool Unlock(string key)
    {
        if (string.IsNullOrEmpty(key) || unlocks.Contains(key)) return false;
        unlocks.Add(key);
        return true;
    }

    public void RecordFish(string fish)
    {
        foreach (var s in fishCaught)
        {
            if (s.itemName == fish) { s.count++; return; }
        }
        fishCaught.Add(new ItemStack(fish, 1));
    }

    public int FishCount(string fish)
    {
        foreach (var s in fishCaught) if (s.itemName == fish) return s.count;
        return 0;
    }

    public int RestoredVillagers
    {
        get
        {
            int n = 0;
            foreach (VillagerDef def in VillagerDB.All)
            {
                if (Get(def.id).wishIndex >= def.wishes.Length) n++;
            }
            return n;
        }
    }

    public bool AllVillagersRestored => RestoredVillagers >= VillagerDB.All.Count;
}
