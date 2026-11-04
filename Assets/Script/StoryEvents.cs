using System.Collections.Generic;
using System.Text;

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

    // 주민 3명 모두 회복 + 마음 90 이상이면 엔딩 (한 번만)
    public static void CheckEnding()
    {
        if (Data == null) return;
        StoryState story = Data.story;
        if (story.IsUnlocked("ending_seen") || !story.AllVillagersRestored || Data.heart < 90f) return;

        story.Unlock("ending_seen");
        string extra = story.IsUnlocked("chose_village") ? "\nYou chose this village over the city. You never looked back." : "";
        HUD.Announce(
            "<size=46>Happiness, rebuilt</size>\n\n" +
            "Rosa's bread smells of strawberries again.\nThe forge sings. Mina chases sunflowers.\n" +
            "The gray city feels like a dream you once had." + extra +
            "\n\n<size=24><color=#ff8fb1>Thank you for playing.</color>  Life on the farm goes on.</size>",
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
        sb.Append($"<color=#ff8fb1>Heart</color> {Data.heart:0}/{Data.maxHeart:0}    Gold {Data.gold}\n\n");

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

        sb.Append("\n<color=#8fb8ff>Fish caught</color>  ");
        var fish = new List<string>();
        foreach (string f in new[] { "Minnow", "Carp", "Golden Koi" })
        {
            int n = story.FishCount(f);
            fish.Add(n > 0 ? $"{f} x{n}" : "???");
        }
        sb.Append(string.Join("   ", fish));
        sb.Append("\n\n<size=22><color=#c8c0b0>[J] Close</color></size>");
        return sb.ToString();
    }
}
