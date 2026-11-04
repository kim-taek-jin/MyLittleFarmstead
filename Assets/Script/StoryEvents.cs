using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// 이야기 이벤트: 도시에서 온 편지(선택), 엔딩, 생활 일지
public static class StoryEvents
{
    static DataManager Data => DataManager.instance;

    // 아침마다 호출 (DataManager.EndDay 이후)
    public static void OnNewDay(int day)
    {
        if (Data == null) return;
        StoryState story = Data.story;

        if (day >= 4 && !story.IsUnlocked("letter1"))
        {
            story.Unlock("letter1");
            HUD.Ask("A letter from the city",
                "\"It's your old boss. We're short-staffed. Come back this weekend, just two days. I'll pay 400G.\"\n<color=#c8c0b0>(Accept: +400G, but the gray creeps back.)</color>",
                null,
                () =>
                {
                    Data.AddGold(400);
                    Data.energy *= 0.5f;
                    Data.ChangeHeart(-12f, "You worked all weekend. The gray creeps back in.");
                    story.Unlock("letter1_accepted");
                },
                () => Data.ChangeHeart(5f, "You wrote back: \"No, thank you.\" It felt good."),
                "Accept", "Decline");
            return;
        }

        if (day >= 10 && story.IsUnlocked("letter1") && !story.IsUnlocked("letter2"))
        {
            story.Unlock("letter2");
            HUD.Ask("A letter from the city",
                "\"Final offer. Full-time position, 1500G signing bonus. You'll be back on the 7:10 train by Monday.\"\n<color=#c8c0b0>(Accept: +1500G. The city will take its toll.)</color>",
                null,
                () =>
                {
                    Data.AddGold(1500);
                    Data.ChangeHeart(-25f, "Your phone buzzes all night. Everything looks gray again.");
                    story.Unlock("letter2_accepted");
                },
                () =>
                {
                    Data.ChangeHeart(8f, "You tore up the letter. This is home now.");
                    story.Unlock("chose_village");
                },
                "Accept", "Tear it up");
        }
    }

    // ---- 엔딩 A: "마지막 색은 나" ----
    // 주민 3명 모두 색을 되찾고 마음 90 이상이면, 집에서 아침을 맞을 때 엔딩이 시작됩니다.
    // 그때까지 내 집(오두막 밖, 집 안)만 회색으로 남아 있다가, 도시 편지에 답장을 쓰면 색이 돌아옵니다.
    public static bool EndingSeen => Data != null && Data.story.IsUnlocked("ending_seen");
    public static bool EndingReady => Data != null && !EndingSeen && Data.story.AllVillagersRestored && Data.heart >= 90f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitEnding()
    {
        SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == "Home") CheckEnding(); };
    }

    public static void CheckEnding()
    {
        if (!EndingReady) return;
        if (SceneManager.GetActiveScene().name == "Home")
        {
            if (Object.FindFirstObjectByType<EndingRunner>() == null) new GameObject("Ending").AddComponent<EndingRunner>();
        }
        else if (Data.story.Unlock("ending_hint"))
        {
            HUD.Toast("Something feels different at home... Maybe it's time to go back.", 5f);
        }
    }

    public static void StartHomeEnding()
    {
        if (!EndingReady) return;
        HUD.Say("", new[]
        {
            "Morning light slips through the window. On the table: one more letter from the city.",
            "\"Your old desk is still empty. Come back. We'll pay double.\"",
            "You look around the little house. Everything outside has its colors back. Only these walls are still gray.",
        }, null, () =>
        {
            HUD.Ask("A letter from the city", "Write back?", null, StayForGood,
                () => HUD.Toast("You fold the letter away. Maybe tomorrow.", 3f),
                "\"I'm staying. This is home now.\"", "Not yet");
        });
    }

    static void StayForGood()
    {
        StoryState story = Data.story;
        story.Unlock("ending_seen");
        story.Unlock("chose_village");
        PixelArtWorld.RefreshSeasonal();          // 집 안이 컬러로
        Data.ChangeHeart(5f, null);
        HUD.Announce(
            "<size=46><color=#ffb6d0>The last color was yours</color></size>\n\n" +
            "The walls warm up. The curtains blush. The kettle shines.\n" +
            "The gray was never the city's. It was yours, and now it's gone.\n\n" +
            "<size=24><color=#ff8fb1>Thank you for playing.</color>  Life in the village goes on.</size>",
            20f);
        Data.SaveGame();
    }

    // J키: 생활 일지
    public static string BuildJournal()
    {
        if (Data == null) return "";
        StoryState story = Data.story;
        var sb = new StringBuilder();
        int day = Data.TimeManager != null ? Data.TimeManager.day : 1;
        sb.Append($"<size=40>Journal</size>   <size=24>Day {day}</size>\n\n");
        Season season = Data.CurrentSeason;
        int toNext = SeasonSystem.HeartToNext(Data.heart, season);
        sb.Append($"<color=#ff8fb1>Heart</color> {Data.heart:0}/{Data.maxHeart:0}    Gold {Data.gold}    " +
                  $"<color={SeasonSystem.Color(season)}>{SeasonSystem.Name(season)}</color>" +
                  (toNext > 0 ? $" <size=22>({SeasonSystem.Name(season + 1)} at {toNext} more Heart)</size>" : "") +
                  $"    Album {story.photos.Count}/{AlbumDB.All.Count} [P]\n\n");

        sb.Append("<color=#ffd84a>Villagers</color>\n");
        foreach (VillagerDef def in VillagerDB.All)
        {
            VillagerProgress p = story.Get(def.id);
            string dots = new string('#', p.wishIndex) + new string('-', def.wishes.Length - p.wishIndex);
            string status;
            if (!p.met) status = "<color=#a0a0a0>not met yet</color>";
            else if (p.wishIndex >= def.wishes.Length) status = "<color=#9dff9d>colors restored</color>";
            else status = "wants " + VillagerDB.ItemsText(def.wishes[p.wishIndex].items);
            sb.Append($"{def.name}  [{dots}]  {status}\n");
        }

        SpiritState sp = story.spirit;
        if (sp.found)
        {
            string key = SpiritDB.FormKey(sp);
            sb.Append($"\n<color=#ff9fc4>Spirit</color>  {SpiritDB.FormName(key)}   " +
                      $"<color=#ffd84a>Sun {sp.sun}</color>  <color=#9dff9d>Earth {sp.earth}</color>  <color=#8fd0ff>Water {sp.water}</color>  <color=#ff9fc4>Bloom {sp.bloom}</color>" +
                      $"   forms {sp.formsSeen.Count}/{SpiritDB.AllForms.Length}\n");
            string perk = SpiritDB.PerkText(sp);
            if (!string.IsNullOrEmpty(perk)) sb.Append($"<size=22>{perk}</size>\n");
        }

        string hill = story.IsUnlocked("starhill")
            ? $"Star Hill open on clear nights. Stars caught: {story.starsTotal}"
            : $"{story.kindness}/{StarHill.KindnessToUnlock} to open Star Hill  (sharing box today: {StoryState.ShareCapPerDay - story.ShareRoom(day)}/{StoryState.ShareCapPerDay})";
        sb.Append($"\n<color=#ffb6d0>Kindness</color> {story.kindness}   <size=22>{hill}</size>\n");

        ForestState forest = story.forest;
        if (forest != null && forest.open)
        {
            string tree = forest.treeAwake ? "The old tree is awake." : "The old tree is asleep.";
            sb.Append($"<color=#9dff9d>Forest</color>  clearings restored {forest.Restored}/{ForestState.Clearings}   <size=22>{tree}</size>\n");
        }
        HillState sunset = story.hill;
        if (sunset != null && sunset.open)
            sb.Append($"<color=#ffb070>Sunset Hill</color>  orchard trees revived {sunset.Revived}/{HillState.Trees}   <size=22>on the drying rack: {sunset.drying.Count}</size>\n");
        LakeState lake = story.lake;
        if (lake != null && lake.open)
        {
            string next = lake.IsClear ? "The lake is clear. Fireflies come out at night." : $"{lake.ToNextZone()} more to clear the next part";
            sb.Append($"<color=#8fd0ff>Lake</color>  clear parts {lake.ZonesClear}/{LakeState.Thresholds.Length}   <size=22>fish released {lake.released}, {next}</size>\n");
        }

        sb.Append("\n<color=#8fb8ff>Fish caught</color>  ");
        var fish = new List<string>();
        foreach (string f in new[] { "Minnow", "Carp", "Golden Koi", "Bluegill", "Moonfin", "Glassfish" })
        {
            int n = story.FishCount(f);
            fish.Add(n > 0 ? $"{f} x{n}" : "???");
        }
        sb.Append(string.Join("   ", fish));
        sb.Append("\n\n<size=22><color=#c8c0b0>[J] Close</color></size>");
        return sb.ToString();
    }
}

// 엔딩 A: 집에서 아침 요약이 닫힌 뒤 엔딩 대화를 시작합니다.
public class EndingRunner : MonoBehaviour
{
    float wait = 0.5f;

    void Update()
    {
        if (HUD.AnnouncementOpen || HUD.DialogueOpen) { wait = 0.5f; return; }
        wait -= Time.deltaTime;
        if (wait > 0f) return;
        enabled = false;
        StoryEvents.StartHomeEnding();
        Destroy(gameObject);
    }
}
