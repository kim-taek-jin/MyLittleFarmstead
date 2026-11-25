// 마음의 계절: 달력이 아니라 마음(Heart)이 계절을 정합니다.
//   마음 0~29 겨울(번아웃) / 30~59 봄(회복) / 60~84 여름(활력) / 85~ 가을(결실)
// 계절은 아침에만 바뀌고, 다시 무리해서 기준보다 5 넘게 떨어지면 '꽃샘추위'로 한 계절 돌아갑니다.
public enum Season { Winter = 0, Spring = 1, Summer = 2, Autumn = 3 }

public static class SeasonSystem
{
    public static readonly int[] Thresholds = { 0, 30, 60, 85 };   // 각 계절이 시작되는 마음
    public const int ColdSnapMargin = 5;

    public static Season ForHeart(float heart)
    {
        for (int i = Thresholds.Length - 1; i >= 0; i--)
        {
            if (heart >= Thresholds[i]) return (Season)i;
        }
        return Season.Winter;
    }

    // 아침 평가: 올라갈 땐 기준을 넘기면 바로, 내려갈 땐 기준보다 5 넘게 떨어져야 (깜빡임 방지)
    public static Season Evaluate(float heart, Season current, Season cap = Season.Autumn)
    {
        Season target = ForHeart(heart);
        if (target > cap) target = cap;
        if (target > current) return target;
        if (target < current)
        {
            int idx = (int)current;
            if (heart < Thresholds[idx] - ColdSnapMargin) return (Season)(idx - 1);
        }
        return current;
    }

    public static int HeartToNext(float heart, Season current)
    {
        int next = (int)current + 1;
        if (next >= Thresholds.Length) return 0;
        return System.Math.Max(0, (int)System.Math.Ceiling(Thresholds[next] - heart));
    }

    public static string Name(Season s)
    {
        switch (s)
        {
            case Season.Spring: return Loc.T("Spring", "봄");
            case Season.Summer: return Loc.T("Summer", "여름");
            case Season.Autumn: return Loc.T("Autumn", "가을");
            default: return Loc.T("Winter", "겨울");
        }
    }

    // 그림 이름, 저장 키에 쓰는 계절 이름: 언어와 상관없이 항상 영어 ("winter" 등).
    // (화면에 보이는 이름 Name은 한국어일 수 있어서 키로 쓰면 안 됨: 한국어일 때 그림을 못 찾던 버그)
    public static string Key(Season s)
    {
        switch (s)
        {
            case Season.Spring: return "spring";
            case Season.Summer: return "summer";
            case Season.Autumn: return "autumn";
            default: return "winter";
        }
    }

    public static string Color(Season s)
    {
        switch (s)
        {
            case Season.Spring: return "#ffb6d0";
            case Season.Summer: return "#9dff9d";
            case Season.Autumn: return "#ffc46a";
            default: return "#c8d8ff";
        }
    }

    // 계절의 기본 비/눈 확률 (겨울은 눈)
    public static double BaseRainChance(Season s)
    {
        switch (s)
        {
            case Season.Spring: return 0.30;
            case Season.Summer: return 0.15;
            case Season.Autumn: return 0.25;
            default: return 0.30;
        }
    }

    // 마음을 따라가는 날씨: 전날을 잘 보냈으면 맑을 확률↑, 과로했으면 흐림↑
    public static double RainChance(Season s, float yesterdayDelta)
    {
        double c = BaseRainChance(s);
        if (yesterdayDelta >= 6f) c *= 0.5;
        else if (yesterdayDelta < 0f) c = System.Math.Min(0.9, c + 0.3);
        return c;
    }

    public static string Arrival(Season s)
    {
        switch (s)
        {
            case Season.Spring: return Loc.T("The snow is melting. Little green shoots push through the ground.\nYou can finally breathe again.", "눈이 녹아요. 땅을 뚫고 파릇한 싹이 올라와요.\n이제야 숨이 쉬어져요.");
            case Season.Summer: return Loc.T("Long, bright days. The fields hum with life,\nand so do you.", "길고 환한 날들. 들판이 생기로 웅성거려요,\n나도 그렇고요.");
            case Season.Autumn: return Loc.T("Golden light over everything you've grown.\nThis is what fruition feels like.", "내가 키운 모든 것 위로 금빛 햇살.\n결실이란 이런 거구나.");
            default: return Loc.T("The world is quiet and gray.", "세상이 조용하고 잿빛이에요.");
        }
    }
}
