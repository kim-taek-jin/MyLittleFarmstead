using System.Collections.Generic;

// K-3: 이웃과 같이 보내는 시간 (품앗이, 새참, 마실). 유니티에 의존하지 않는 규칙만 담았습니다.
//  - 주민마다 이야기 3장. 한 장을 넘기면 색이 1/3 돌아오고, 다음 장은 Gap일 뒤에 열림 (하루에 몰아서 못 함)
//  - 품앗이: 말을 걸어 부탁을 들은 뒤(invited), 그 사람 일터의 자리에서 [E]를 steps번
//  - 새참: 낮에 음식을 들고 말을 걸면 같이 앉아 먹음. 이야기 장이면 장면, 아니면 짧은 한 마디 (주민마다 하루 한 번)
//  - 마실: 저녁(18~22시) 그 사람 자리에 찾아가 앉음. 하룻저녁에 한 집
//  - 선물 점수나 좋아하는 물건은 없습니다 (스타듀밸리의 선물/호감도와 다르게)
public static class Neighbors
{
    public const int Gap = 2;                    // 장과 장 사이 최소 날짜
    public const int EveningStart = 18, EveningEnd = 22;
    public const int NeverDay = -99;

    public static bool IsEvening(int hour) => hour >= EveningStart && hour < EveningEnd;
    public static bool IsDay(int hour) => hour >= 6 && hour < EveningStart;

    // F-1: 장마다 열리는 계절이 있음. SeasonNow = 지금 계절, Cap = 이 판에서 볼 수 있는 마지막 계절 (체험판은 겨울)
    //  DataManager가 아침마다, 불러올 때 맞춰 둠
    public static Season SeasonNow = Season.Winter;
    public static Season Cap = Season.Autumn;

    // 이 판에서 넘길 수 있는 장 수 (체험판은 겨울 장까지만 → 겨울 끝에 색을 다 되찾음)
    public static int InScope(VillagerDef d)
    {
        int n = 0;
        foreach (ChapterDef c in d.chapters) if (c.season <= Cap) n++;
        return n;
    }

    public static bool Restored(VillagerDef d, VillagerProgress p) => p.wishIndex >= InScope(d);

    // 이 판에서 이야기가 있는 이웃만 셈 (체험판엔 봄부터 이야기가 있는 명숙 아주머니는 빠짐)
    public static bool Counted(VillagerDef d) => InScope(d) > 0;
    public static int CountedVillagers(IList<VillagerDef> all) { int n = 0; foreach (var d in all) if (Counted(d)) n++; return n; }
    public static ChapterDef Next(VillagerDef d, VillagerProgress p) => p == null || Restored(d, p) ? null : d.chapters[p.wishIndex];

    // 그림 단계 0(회색)~3(다 돌아온 색): 넘긴 장의 비율
    public static int Stage(VillagerDef d, VillagerProgress p)
    {
        int n = InScope(d);
        if (p == null || n <= 0) return 0;
        int s = (int)System.Math.Round(3.0 * System.Math.Min(p.wishIndex, n) / n, System.MidpointRounding.AwayFromZero);
        return System.Math.Max(0, System.Math.Min(3, s));
    }

    // 다음 장이 열렸는지 (만난 뒤, 지난 장에서 Gap일이 지남, 그 장의 계절이 됨)
    public static bool Ready(VillagerDef d, VillagerProgress p, int today) =>
        p != null && p.met && !Restored(d, p) && today >= p.lastDay + Gap && Next(d, p).season <= SeasonNow;

    // 이 계절의 이야기는 다 넘겼는지 (다음 장이 다음 계절에 있음)
    public static bool WaitingForSeason(VillagerDef d, VillagerProgress p)
    {
        ChapterDef c = Next(d, p);
        return c != null && c.season > SeasonNow;
    }

    // 지금 이 장을 할 수 있는 시간인지 (마실은 저녁, 나머지는 낮)
    public static bool RightTime(ChapterDef c, int hour) => c != null && (c.kind == ChapterKind.Masil ? IsEvening(hour) : IsDay(hour) || c.kind == ChapterKind.Fish);

    public static void Meet(VillagerProgress p) { p.met = true; p.lastDay = NeverDay; }

    // 장을 넘김. 돌려받을 품(favor)과 해금은 부르는 쪽에서 story.Unlock
    public static void Complete(VillagerProgress p, int today)
    {
        p.wishIndex++;
        p.lastDay = today;
        p.invited = false;
        p.chore = 0;
    }

    // ---- 품앗이: 자리에서 [E] 한 번. 다 했으면 true
    public static bool ChoreStep(VillagerDef d, VillagerProgress p, int today, string spot, out int step)
    {
        step = p.chore;
        ChapterDef c = Next(d, p);
        if (c == null || c.kind != ChapterKind.Pumasi || c.spot != spot || !p.invited || !Ready(d, p, today)) return false;
        p.chore++;
        step = p.chore;
        return p.chore >= c.steps.Length;
    }

    public static bool CanChoreAt(VillagerDef d, VillagerProgress p, int today, int hour, string spot)
    {
        ChapterDef c = Next(d, p);
        return c != null && c.kind == ChapterKind.Pumasi && c.spot == spot && p.invited && Ready(d, p, today) && IsDay(hour);
    }

    // ---- 새참: 들고 있는 음식으로 이 장을 넘길 수 있는지 / 그냥 같이 먹을 수 있는지
    public static bool SaechamChapter(VillagerDef d, VillagerProgress p, int today, int hour, string held, bool heldIsFood)
    {
        ChapterDef c = Next(d, p);
        return c != null && c.kind == ChapterKind.Saecham && Ready(d, p, today) && IsDay(hour) && heldIsFood
               && (string.IsNullOrEmpty(c.needItem) || c.needItem == held);
    }

    public static bool CanSaecham(VillagerProgress p, int today, int hour, bool heldIsFood) => p != null && p.met && heldIsFood && IsDay(hour) && p.saechamDay != today;

    // ---- 마실: 하룻저녁에 한 집
    public static bool CanMasil(StoryState s, VillagerProgress p, int today, int hour) => p != null && p.met && IsEvening(hour) && s.masilDay != today;
    public static bool MasilChapter(VillagerDef d, VillagerProgress p, int today)
    {
        ChapterDef c = Next(d, p);
        return c != null && c.kind == ChapterKind.Masil && Ready(d, p, today);
    }

    // ---- 안내: 생활 일지 한 줄
    public static string Status(VillagerDef d, VillagerProgress p, int today)
    {
        if (p == null || !p.met) return Loc.T("not met yet", "아직 못 만났어요");
        if (Restored(d, p)) return Loc.T("colors restored", "색을 되찾았어요");
        ChapterDef c = Next(d, p);
        if (c.season > SeasonNow) return Loc.T($"more in {SeasonSystem.Name(c.season)}", $"{SeasonSystem.Name(c.season)}에 이어져요");
        string next = Loc.T("Next: ", "다음: ") + c.hint;
        return Ready(d, p, today) ? next : next + Loc.T(" (give it a day or two)", " (하루이틀 뒤에)");
    }

    // ---- 아침 방송: 일손이 필요한 이웃 (품앗이 장이 열린 사람), 없으면 null
    public static string HelpWanted(StoryState s, IList<VillagerDef> all, int today)
    {
        foreach (VillagerDef d in all)
        {
            VillagerProgress p = s.Get(d.id);
            ChapterDef c = Next(d, p);
            if (c != null && c.kind == ChapterKind.Pumasi && Ready(d, p, today)) return d.id;
        }
        return null;
    }

    // ---- 오늘 미리보기: 저녁에 마실 갈 수 있는 이웃 이름, 없으면 null
    public static string MasilTonight(StoryState s, IList<VillagerDef> all, int today)
    {
        foreach (VillagerDef d in all) if (MasilChapter(d, s.Get(d.id), today)) return d.name;
        return null;
    }

    // ---- 미나의 품: 아침에 가끔 (반반) 물 안 준 작물 몇 개에 물을 줌
    public const int MinaWaters = 6;
    public static bool MinaWatersToday(int day) => (day * 7 + 3) % 2 == 0;
}
