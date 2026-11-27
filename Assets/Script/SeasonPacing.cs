// F-0: 네 계절의 흐름 (Unity 없이 — 로직 테스트에서도 씁니다).
//
//  예전: 마음 30·60·85를 넘기면 계절이 바뀜 → 겨울에 마음이 70까지 차 있어서 대보름이 끝나면
//        이틀 만에 봄·여름·가을이 다 지나가 버렸음.
//  이제:
//   - 계절마다 그 계절의 **마지막 절기**를 지나야 다음 계절로: 겨울 = 정월대보름, 봄 = 모내기(F-1),
//     여름 = 복날(F-2), 가을 = 김장(F-3, 엔딩으로). 아직 없는 절기는 그 계절을 만들기 전까지 임시로
//     "그 계절에서 TargetDays일을 보내면" 넘어감.
//   - 마음은 계절마다 한 칸(띠)씩: 겨울 0~35, 봄 35~60, 여름 60~85, 가을 85~100.
//     계절 안에서는 그 띠의 위쪽 끝을 넘지 않고, 끝에 가까울수록 천천히 참.
//     계절이 바뀌는 아침에는 새 띠의 시작까지 올라감 (큰 색 물결).
//   - 계절은 앞으로만 감 (예전 '꽃샘추위'로 한 계절 돌아가던 것은 뺌: 절기를 지난 계절이 되돌아가면 이상해서).
//   - 세상의 색 = (마음 - 10) / 90 → 첫날(마음 20)은 예전처럼 거의 회색, 가을 끝에 다 돌아옴.
public static class SeasonPacing
{
    public static readonly int[] Start = { 0, 35, 60, 85 };          // 계절마다 마음 띠의 시작 (= SeasonSystem.Thresholds)
    public static readonly int[] TargetDays = { 13, 20, 20, 20 };    // 계절 하나의 길이 (날). 겨울은 대보름(12일째 이후)까지
    public const float GainScale = 0.6f;                              // 좋은 일로 오르는 마음을 이만큼만 (한 해를 다 채우려면)
    public const float MinRoom = 0.15f;                               // 띠 끝에 다 와도 이만큼은 오름

    public static float Top(Season s) => s == Season.Autumn ? 100f : Start[(int)s + 1];
    public static float Bottom(Season s) => Start[(int)s];
    static float Band(Season s) => Top(s) - Bottom(s);

    // 좋은 일로 마음이 오를 때 곱하는 값: 띠 끝에 가까울수록 작아짐
    public static float Scale(Season s, float heart)
    {
        float room = (Top(s) - heart) / Band(s);
        if (room < MinRoom) room = MinRoom;
        if (room > 1f) room = 1f;
        return GainScale * room;
    }

    // 계절 안에서 마음이 넘지 않는 선
    public static float Clamp(Season s, float heart) => heart > Top(s) ? Top(s) : heart;

    // 다음 계절 절기를 준비할 만큼 마음이 찼는지 (겨울: 이때 대보름 날이 정해짐)
    public static bool Ready(Season s, float heart) => heart >= Bottom(s) + Band(s) * 0.5f;

    public static float Color01(float heart)
    {
        float c = (heart - 10f) / 90f;
        return c < 0f ? 0f : c > 1f ? 1f : c;
    }

    // 이 계절을 마쳤는지 (마지막 절기를 지났는지)
    public static bool Closed(Season s, StoryState st, int day)
    {
        if (s == Season.Winter) return st.rites.boreumDone;
        if (s == Season.Spring) return st.spring != null && st.spring.monaegiDone;   // F-1: 모내기
        return DaysIn(st, day) >= TargetDays[(int)s];       // F-2, F-3에서 각 계절의 절기로 바꿈
    }

    public static int DaysIn(StoryState st, int day) => day - st.seasonStartDay;

    // 아침에 정하는 계절: 지난 계절을 마쳤으면 한 칸 앞으로 (cap: 체험판은 겨울까지)
    public static Season Next(Season current, StoryState st, int day, Season cap)
    {
        if (current >= cap || current >= Season.Autumn) return current;
        return Closed(current, st, day) ? current + 1 : current;
    }

    // 계절이 바뀐 아침의 마음: 새 띠의 시작까지는 올라감
    public static float EnterHeart(Season s, float heart) => heart < Bottom(s) ? Bottom(s) : heart;

    // ---------------------------------------------------------------- 저장 파일
    public const int SaveVersion = 2;   // 2 = F-0 계절 흐름

    // 예전 저장(1)을 새 계절 흐름에 맞춤: 지금 계절을 오늘부터 다시 세고, 마음을 그 계절의 띠 안으로
    public static float Migrate(int version, StoryState st, float heart, int day)
    {
        if (version >= SaveVersion || st == null) return heart;
        Season s = (Season)System.Math.Max(0, System.Math.Min(3, st.season));
        st.seasonStartDay = day;
        if (heart > Top(s)) heart = Top(s);
        if (heart < Bottom(s) && s != Season.Winter) heart = Bottom(s);
        return heart;
    }

    // 일지·아침 예고에 쓰는 "다음 계절은 언제" 한 줄
    public static string NextText(Season s, StoryState st, int day)
    {
        switch (s)
        {
            case Season.Winter:
                return st.rites.boreumDay > 0
                    ? Loc.T($"Spring comes after Jeongwol Daeboreum (day {st.rites.boreumDay})", $"정월대보름({st.rites.boreumDay}일째)이 지나면 봄")
                    : Loc.T("Spring comes after Jeongwol Daeboreum", "정월대보름이 지나면 봄");
            case Season.Spring:
                int md = st.seasonStartDay + Spring.MonaegiDay - 1;
                return Loc.T($"Summer comes after the rice planting (day {md})", $"모내기({md}일째)가 지나면 여름");
            case Season.Autumn:
                return Loc.T("The last season of the year", "한 해의 마지막 계절");
            default:
                int left = TargetDays[(int)s] - DaysIn(st, day);
                if (left < 0) left = 0;
                return Loc.T($"{SeasonSystem.Name(s + 1)} in about {left} day(s)", $"{SeasonSystem.Name(s + 1)}까지 약 {left}일");
        }
    }
}
