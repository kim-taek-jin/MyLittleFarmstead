// F-1: 봄 — 심는 계절. 유니티에 의존하지 않는 규칙만 (로직 테스트에서도 씁니다).
//
//  봄 N일째 (봄이 온 아침 = 1일째):
//   3일째  이장님 방송: 내일부터 논에 물 댑니다, 집집마다 모판에 볍씨를
//   4일째~ 논물 대기: 큰 논 위쪽 물꼬에서 [E] 세 번 (이장님 품앗이). 안 하면 모내기 날 아침에 이장님이 혼자 댐
//          모판: 마당 모판에 [E]로 볍씨 뿌리기 → 물 준 날(비 온 날 포함) 3일 + 뿌린 뒤 6일이면 모가 다 자람
//   7일째  삼짇날 화전놀이: 뒷산 옹달샘 너럭바위에서 (10~16시). 진달래를 따 왔으면 더 좋고, 없어도 할머니가 따 둠
//          마지막 화전 한 장을 누구에게 건넬지 고름 → 그 이웃이 다음에 고마워함
//   19일째 모내기 (8~17시): 큰 논 위 못줄 자리에서 [E]로 한 포기씩 (실패 없음) → 올해 나에게 키우고 싶은 것 하나
//          → 논두렁 새참. 그 날을 놓치면 다음 아침 마을 사람들이 다 심어 둠 (내년에 같이). 모내기가 봄의 마지막 절기
[System.Serializable]
public class SpringState
{
    public int waterSteps;
    public bool watered;                 // 논에 물을 댔는지
    public bool wateredBySelf;           // 내가 거들었는지 (아니면 이장님 혼자)
    public int seedSown = -1;            // 모판에 볍씨를 뿌린 날
    public int seedWaterDays;            // 모판에 물 준 날 수 (비 온 날 포함)
    public int seedLastWater = -1;
    public bool samjitDone;
    public string hwajeonTo;             // 마지막 화전을 건넨 이웃
    public bool hwajeonThanked;
    public int planted;                  // 모내기에서 심은 포기
    public bool monaegiDone;
    public bool monaegiMissed;           // 모내기 날을 놓침 (마을 사람들이 다 심어 둠)
    public string hope;                  // 모와 함께 심은 "올해 나에게 키우고 싶은 것"
}

public static class Spring
{
    public const int Season = 1;
    public const int NoticeDay = 3, WaterFrom = 4, WaterSteps = 3;
    public const int SeedFrom = 4, SeedGrowDays = 6, SeedWaterNeeded = 3;
    public const int SamjitDay = 7, SamjitFrom = 10, SamjitUntil = 16, HwajeonAzaleas = 3;
    public const int MonaegiDay = 19, MonaegiFrom = 8, MonaegiUntil = 17, MonaegiNotice = 2, PlantSteps = 6;

    public static readonly string[] HopeIds = { "slow", "no", "ask", "play" };

    public static bool IsSpring(StoryState s) => s != null && s.season == Season;

    // 봄 몇째 날 (봄이 온 아침 = 1)
    public static int Day(StoryState s, int day) => day - s.seasonStartDay + 1;

    // ---------------------------------------------------------------- 논물 대기
    public static bool CanWaterPaddy(StoryState s, int day) => IsSpring(s) && !s.spring.watered && Day(s, day) >= WaterFrom && !s.spring.monaegiDone;

    // [E] 한 번. 다 댔으면 true
    public static bool WaterStep(SpringState sp)
    {
        if (sp.watered) return false;
        sp.waterSteps++;
        if (sp.waterSteps < WaterSteps) return false;
        sp.watered = true;
        sp.wateredBySelf = true;
        return true;
    }

    // ---------------------------------------------------------------- 모판
    public static bool CanSow(StoryState s, int day) => IsSpring(s) && s.spring.seedSown < 0 && Day(s, day) >= SeedFrom && !s.spring.monaegiDone;

    public static void Sow(SpringState sp, int day) { sp.seedSown = day; sp.seedWaterDays = 0; sp.seedLastWater = -1; }

    public static bool WaterSeedbed(SpringState sp, int day)
    {
        if (sp.seedSown < 0 || sp.seedLastWater == day || SeedStage(sp, day) >= 3) return false;
        sp.seedLastWater = day;
        sp.seedWaterDays++;
        return true;
    }

    // 0 빈 모판, 1 뿌림, 2 싹, 3 다 자란 모
    public static int SeedStage(SpringState sp, int day)
    {
        if (sp.seedSown < 0) return 0;
        if (sp.seedWaterDays >= SeedWaterNeeded && day >= sp.seedSown + SeedGrowDays) return 3;
        return sp.seedWaterDays >= 1 ? 2 : 1;
    }

    // ---------------------------------------------------------------- 삼짇날 화전놀이
    public static bool SamjitToday(StoryState s, int day) => IsSpring(s) && Day(s, day) == SamjitDay && !s.spring.samjitDone;
    public static bool SamjitNow(StoryState s, int day, int hour) => SamjitToday(s, day) && hour >= SamjitFrom && hour < SamjitUntil;

    public static void FinishSamjit(SpringState sp, string giveTo) { sp.samjitDone = true; sp.hwajeonTo = giveTo; sp.hwajeonThanked = false; }

    // 화전을 건넨 이웃이 다음에 말을 걸 때 한 번 고마워함
    public static bool ThanksDue(SpringState sp, string villagerId) => sp.samjitDone && sp.hwajeonTo == villagerId && !sp.hwajeonThanked;

    // ---------------------------------------------------------------- 모내기 (봄의 마지막 절기)
    public static bool MonaegiToday(StoryState s, int day) => IsSpring(s) && Day(s, day) >= MonaegiDay && !s.spring.monaegiDone;
    public static bool MonaegiNow(StoryState s, int day, int hour) => MonaegiToday(s, day) && hour >= MonaegiFrom && hour < MonaegiUntil;

    // [E] 한 포기. 다 심었으면 true
    public static bool Plant(SpringState sp)
    {
        if (sp.monaegiDone || sp.planted >= PlantSteps) return false;
        sp.planted++;
        return sp.planted >= PlantSteps;
    }

    public static void FinishMonaegi(SpringState sp, string hope) { sp.monaegiDone = true; sp.hope = hope; }

    // ---------------------------------------------------------------- 아침 (newDay = 막 시작된 날)
    // 비는 모판을 적시고, 모내기 날 아침엔 이장님이 논물을 대 두고, 모내기 날을 놓쳤으면 마을 사람들이 다 심어 둠
    public static string Morning(StoryState s, int newDay, bool rain)
    {
        if (!IsSpring(s)) return null;
        SpringState sp = s.spring;
        if (rain && sp.seedSown >= 0) WaterSeedbed(sp, newDay);
        int d = Day(s, newDay);
        if (d >= MonaegiDay && !sp.watered) sp.watered = true;
        if (d > MonaegiDay && !sp.monaegiDone)
        {
            sp.monaegiDone = true;
            sp.monaegiMissed = true;
            return Loc.T("While you were away, the village planted the paddy without you. \"Next year, together!\" says the note on your gate.",
                         "마을 사람들이 모를 다 심어 두었어요. 대문에 쪽지: \"내년엔 같이 심자!\"");
        }
        return null;
    }

    // 아침 방송 한 줄 (없으면 null)
    public static string Broadcast(StoryState s, int day)
    {
        if (!IsSpring(s)) return null;
        int d = Day(s, day);
        SpringState sp = s.spring;
        if (d == NoticeDay)
            return Loc.T("From tomorrow we let water into the paddies. Anyone with free hands, the sluice above the big paddy! And sow your rice seed in the seedbeds, everyone.",
                         "내일부터 논에 물 댑니다. 손 있는 사람은 큰 논 위 물꼬로 나와 주이소! 집집마다 모판에 볍씨도 뿌리시고요.");
        if (d == SamjitDay - 1)
            return Loc.T("Tomorrow is Samjitnal, the third of the third month. The azaleas are out on the back hill: Grandma Boksun is frying flower pancakes by the spring, late morning!",
                         "내일은 삼월 삼짇날입니다. 뒷산에 진달래가 한창이라, 복순 할머니가 옹달샘 너럭바위에서 화전을 부친답니다. 오전 늦게 모이이소!");
        if (d == SamjitDay && !sp.samjitDone)
            return Loc.T("Today's Samjitnal! Flower pancakes on the back hill, by the spring.", "오늘 삼짇날입니다! 뒷산 옹달샘에서 화전 부칩니다.");
        if (d == MonaegiDay - MonaegiNotice)
            return Loc.T($"Rice planting is in two days (day {s.seasonStartDay + MonaegiDay - 1}). Look after your seedlings!",
                         $"모내기는 모레({s.seasonStartDay + MonaegiDay - 1}일째)입니다. 모판 모 잘 키워 두이소!");
        if (d == MonaegiDay - 1) return Loc.T("Rice planting tomorrow! Morning at the big paddy.", "내일 모내기입니다! 아침에 큰 논으로.");
        if (d == MonaegiDay && !sp.monaegiDone) return Loc.T("Today we plant the rice! Everyone to the big paddy, the line is waiting.", "오늘 모내기입니다! 다들 큰 논으로, 못줄 잡고 기다립니다.");
        return null;
    }

    // ---------------------------------------------------------------- 글
    public static string HopeText(string id)
    {
        switch (id)
        {
            case "slow": return Loc.T("walking slowly", "천천히 걷기");
            case "no": return Loc.T("saying no when I need to", "필요할 땐 거절하기");
            case "ask": return Loc.T("asking for help", "도와 달라고 말하기");
            case "play": return Loc.T("playing, just for fun", "그냥 재미로 놀기");
            default: return "";
        }
    }

    public static string SeedText(int stage)
    {
        switch (stage)
        {
            case 1: return Loc.T("Rice seed sown. Water it.", "볍씨를 뿌렸어요. 물을 주세요.");
            case 2: return Loc.T("Green shoots in the seedbed.", "모판에 파란 싹이 올라왔어요.");
            case 3: return Loc.T("The seedlings are ready to plant.", "모가 다 자랐어요. 심을 때예요.");
            default: return Loc.T("An empty seedbed.", "빈 모판.");
        }
    }
}
