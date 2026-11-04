using System.Collections.Generic;

// 주민 한 명의 진행 상황
[System.Serializable]
public class VillagerProgress
{
    public string id;
    public int wishIndex;   // 이뤄준 소원 수 (= 색이 돌아온 단계, 0 = 회색)
    public bool met;        // 처음 인사를 나눴는지
}

// 색을 먹는 정령의 상태
[System.Serializable]
public class SpiritState
{
    public bool found;
    public int xp;
    public int sun, earth, water, bloom;   // 먹인 색 기운
    public List<string> formsSeen = new List<string>();
}

// 이야기 진행 상태 (저장됨): 주민, 해금, 잡은 물고기, 이벤트 플래그
[System.Serializable]
public class StoryState
{
    public List<VillagerProgress> villagers = new List<VillagerProgress>();
    public List<string> unlocks = new List<string>();   // 레시피/상점/이벤트 플래그
    public List<ItemStack> fishCaught = new List<ItemStack>();

    // 색을 먹는 정령
    public SpiritState spirit = new SpiritState();

    // 나눔 → 별의 언덕
    public int kindness;
    public int donationDay = -1;     // 마지막으로 나눔을 한 날 (하루 첫 나눔 보너스용)
    public int starsDay = -1;        // 별을 주운 날
    public int starsTonight;         // 그날 밤 주운 별 수
    public int starsTotal;
    public int benchDay = -1;        // 벤치에서 별을 본 날

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
