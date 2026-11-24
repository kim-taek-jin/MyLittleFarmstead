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
            HUD.Ask(Loc.T("A letter from the city", "도시에서 온 편지"),
                Loc.T("\"It's your old boss. We're short-staffed. Come back this weekend, just two days. I'll pay 400G.\"\n<color=#c8c0b0>(Accept: +400G, but the gray creeps back.)</color>",
                      "\"예전 팀장이야. 일손이 모자라. 이번 주말 딱 이틀만 나와 줘. 400G 줄게.\"\n<color=#c8c0b0>(받아들이면: +400G, 대신 잿빛이 다시 스며들어요.)</color>"),
                null,
                () =>
                {
                    Data.AddGold(400);
                    Data.energy *= 0.5f;
                    Data.ChangeHeart(-12f, Loc.T("You worked all weekend. The gray creeps back in.", "주말 내내 일했어요. 잿빛이 다시 스며들어요."));
                    story.Unlock("letter1_accepted");
                },
                () => Data.ChangeHeart(5f, Loc.T("You wrote back: \"No, thank you.\" It felt good.", "\"괜찮습니다\"라고 답장했어요. 속이 시원해요.")),
                Loc.T("Accept", "받아들이기"), Loc.T("Decline", "거절하기"));
            return;
        }

        if (day >= 10 && story.IsUnlocked("letter1") && !story.IsUnlocked("letter2"))
        {
            story.Unlock("letter2");
            HUD.Ask(Loc.T("A letter from the city", "도시에서 온 편지"),
                Loc.T("\"Final offer. Full-time position, 1500G signing bonus. You'll be back on the 7:10 train by Monday.\"\n<color=#c8c0b0>(Accept: +1500G. The city will take its toll.)</color>",
                      "\"마지막 제안이야. 정규직, 입사 보너스 1500G. 월요일이면 다시 7시 10분 기차를 타게 될 거야.\"\n<color=#c8c0b0>(받아들이면: +1500G. 도시가 대가를 가져가요.)</color>"),
                null,
                () =>
                {
                    Data.AddGold(1500);
                    Data.ChangeHeart(-25f, Loc.T("Your phone buzzes all night. Everything looks gray again.", "밤새 휴대폰이 울려요. 모든 게 다시 잿빛이에요."));
                    story.Unlock("letter2_accepted");
                },
                () =>
                {
                    Data.ChangeHeart(8f, Loc.T("You tore up the letter. This is home now.", "편지를 찢었어요. 이제 여기가 집이에요."));
                    story.Unlock("chose_village");
                },
                Loc.T("Accept", "받아들이기"), Loc.T("Tear it up", "찢어 버리기"));
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
            HUD.Toast(Loc.T("Something feels different at home... Maybe it's time to go back.", "집에 뭔가 달라진 느낌이에요... 돌아가 볼 때인가 봐요."), 5f);
        }
    }

    public static void StartHomeEnding()
    {
        if (!EndingReady) return;
        HUD.Say("", new[]
        {
            Loc.T("Morning light slips through the window. On the table: one more letter from the city.", "아침 햇살이 창으로 스며들어요. 상 위에 도시에서 온 편지가 하나 더 놓여 있어요."),
            Loc.T("\"Your old desk is still empty. Come back. We'll pay double.\"", "\"네 자리 아직 비어 있어. 돌아와. 두 배 줄게.\""),
            Loc.T("You look around the little house. Everything outside has its colors back. Only these walls are still gray.", "작은 집을 둘러봐요. 바깥은 모두 색을 되찾았는데, 이 벽들만 아직 회색이에요."),
        }, null, () =>
        {
            HUD.Ask(Loc.T("A letter from the city", "도시에서 온 편지"), Loc.T("Write back?", "답장을 쓸까요?"), null, StayForGood,
                () => HUD.Toast(Loc.T("You fold the letter away. Maybe tomorrow.", "편지를 접어 두었어요. 내일 쓸까 봐요."), 3f),
                Loc.T("\"I'm staying. This is home now.\"", "\"저 여기 남을게요. 이제 여기가 집이에요.\""), Loc.T("Not yet", "아직"));
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
            Loc.T("<size=46><color=#ffb6d0>The last color was yours</color></size>\n\n" +
                  "The walls warm up. The curtains blush. The kettle shines.\n" +
                  "The gray was never the city's. It was yours, and now it's gone.\n\n" +
                  "<size=24><color=#ff8fb1>Thank you for playing.</color>  Life in the village goes on.</size>",
                  "<size=46><color=#ffb6d0>마지막 색은 나였어요</color></size>\n\n" +
                  "벽이 따스해지고, 커튼이 발그레해지고, 주전자가 반짝여요.\n" +
                  "잿빛은 도시의 것이 아니었어요. 내 것이었고, 이제 사라졌어요.\n\n" +
                  "<size=24><color=#ff8fb1>플레이해 주셔서 고마워요.</color>  마을의 삶은 계속돼요.</size>"),
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
        sb.Append("<size=40>" + Loc.T("Journal", "생활 일지") + "</size>   <size=24>" + Loc.T($"Day {day}", $"{day}일째") + "</size>\n\n");
        Season season = Data.CurrentSeason;
        int toNext = SeasonSystem.HeartToNext(Data.heart, season);
        sb.Append("<color=#ff8fb1>" + Loc.T("Heart", "마음") + $"</color> {Data.heart:0}/{Data.maxHeart:0}    " + Loc.T("Gold", "골드") + $" {Data.gold}    " +
                  $"<color={SeasonSystem.Color(season)}>{SeasonSystem.Name(season)}</color>" +
                  (toNext > 0 ? " <size=22>(" + Loc.T($"{SeasonSystem.Name(season + 1)} at {toNext} more Heart", $"{SeasonSystem.Name(season + 1)}까지 마음 {toNext}") + ")</size>" : "") +
                  "    " + Loc.T("Album", "앨범") + $" {story.photos.Count}/{AlbumDB.All.Count} [P]\n\n");

        sb.Append("<color=#ffd84a>Villagers</color>\n");
        foreach (VillagerDef def in VillagerDB.All)
        {
            VillagerProgress p = story.Get(def.id);
            string dots = new string('#', p.wishIndex) + new string('-', def.wishes.Length - p.wishIndex);
            string status;
            if (!p.met) status = "<color=#a0a0a0>" + Loc.T("not met yet", "아직 못 만났어요") + "</color>";
            else if (p.wishIndex >= def.wishes.Length) status = "<color=#9dff9d>" + Loc.T("colors restored", "색을 되찾았어요") + "</color>";
            else status = Loc.T("wants ", "소원: ") + VillagerDB.ItemsText(def.wishes[p.wishIndex].items);
            sb.Append($"{def.name}  [{dots}]  {status}\n");
        }

        SpiritState sp = story.spirit;
        if (sp.found)
        {
            string key = SpiritDB.FormKey(sp);
            sb.Append("\n<color=#ff9fc4>" + Loc.T("Spirit", "정령") + $"</color>  {SpiritDB.FormName(key)}   " +
                      "<color=#ffd84a>" + Loc.T("Sun", "햇빛") + $" {sp.sun}</color>  <color=#9dff9d>" + Loc.T("Earth", "대지") + $" {sp.earth}</color>  <color=#8fd0ff>" + Loc.T("Water", "물") + $" {sp.water}</color>  <color=#ff9fc4>" + Loc.T("Bloom", "꽃") + $" {sp.bloom}</color>" +
                      "   " + Loc.T("forms", "모습") + $" {sp.formsSeen.Count}/{SpiritDB.AllForms.Length}\n");
            string perk = SpiritDB.PerkText(sp);
            if (!string.IsNullOrEmpty(perk)) sb.Append($"<size=22>{perk}</size>\n");
        }

        string hill = story.IsUnlocked("starhill")
            ? Loc.T($"Star Hill open on clear nights. Stars caught: {story.starsTotal}", $"맑은 밤마다 별의 언덕이 열려요. 주운 별: {story.starsTotal}")
            : Loc.T($"{story.kindness}/{StarHill.KindnessToUnlock} to open Star Hill  (sharing box today: {StoryState.ShareCapPerDay - story.ShareRoom(day)}/{StoryState.ShareCapPerDay})",
                    $"별의 언덕까지 {story.kindness}/{StarHill.KindnessToUnlock}  (오늘 나눔 상자: {StoryState.ShareCapPerDay - story.ShareRoom(day)}/{StoryState.ShareCapPerDay})");
        sb.Append("\n<color=#ffb6d0>" + Loc.T("Kindness", "나눔") + $"</color> {story.kindness}   <size=22>{hill}</size>\n");

        ForestState forest = story.forest;
        if (forest != null && forest.open)
        {
            string tree = forest.treeAwake ? Loc.T("The old zelkova is awake.", "느티나무가 깨어났어요.") : Loc.T("The old zelkova is asleep.", "느티나무는 잠들어 있어요.");
            sb.Append("<color=#9dff9d>" + Loc.T("Forest", "뒷산") + "</color>  " + Loc.T("clearings restored", "되살린 빈터") + $" {forest.Restored}/{ForestState.Clearings}   <size=22>{tree}</size>\n");
        }
        HillState sunset = story.hill;
        if (sunset != null && sunset.open)
            sb.Append("<color=#ffb070>" + Loc.T("Sunset Hill", "노을 언덕") + "</color>  " + Loc.T("orchard trees revived", "되살린 과일나무") + $" {sunset.Revived}/{HillState.Trees}   <size=22>" + Loc.T("on the drying rack: ", "곶감 걸이: ") + $"{sunset.drying.Count}</size>\n");
        LakeState lake = story.lake;
        if (lake != null && lake.open)
        {
            string next = lake.IsClear ? Loc.T("The lake is clear. Fireflies come out at night.", "저수지가 맑아요. 밤이면 반딧불이가 떠요.") : Loc.T($"{lake.ToNextZone()} more to clear the next part", $"다음 구역까지 {lake.ToNextZone()}");
            sb.Append("<color=#8fd0ff>" + Loc.T("Lake", "저수지") + "</color>  " + Loc.T("clear parts", "맑은 구역") + $" {lake.ZonesClear}/{LakeState.Thresholds.Length}   <size=22>" + Loc.T("fish released", "놓아준 물고기") + $" {lake.released}, {next}</size>\n");
        }

        sb.Append("\n<color=#8fb8ff>" + Loc.T("Fish caught", "잡은 물고기") + "</color>  ");
        var fish = new List<string>();
        foreach (string f in new[] { "Minnow", "Carp", "Golden Koi", "Bluegill", "Moonfin", "Glassfish" })
        {
            int n = story.FishCount(f);
            fish.Add(n > 0 ? $"{ItemNames.Display(f)} x{n}" : "???");
        }
        sb.Append(string.Join("   ", fish));
        sb.Append("\n\n<size=22><color=#c8c0b0>" + Loc.T("[J] Close", "[J] 닫기") + "</color></size>");
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
