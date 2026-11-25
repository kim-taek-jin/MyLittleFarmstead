using System.Collections.Generic;

// 장 담그기 (K-2). 기다려야 익는 살림. 유니티에 의존하지 않는 규칙만 담았습니다.
//  1) 메주 쑤기: 아궁이에서 메주콩 4 → 생메주 1 (요리처럼 만들기)
//  2) 메주 말리기: 집 옆 메주 걸이에 걸면 DryDays일 뒤 마른 메주 (한 번에 RackSize개)
//  3) 장 담그기: 장독대의 빈 독에 마른 메주 1 + 소금 2
//  4) 장독 돌보기: 맑은 날 뚜껑을 열어 두면 볕을 먹어 맛이 깊어지고(care), 비·눈 오는 날 열어 두면 빗물이 들어감(rain)
//     뚜껑은 장독대 전체를 한 번에 열고 닫음. 깜빡해도 망하지는 않음 (조금 덜 깊은 맛)
//  5) 장 가르기: 담근 지 FermentDays일 뒤 → 된장과 간장. 많이 돌볼수록 더 많이
[System.Serializable]
public class JarState
{
    public int startDay = -1;      // 장을 담근 날 (-1: 빈 독)
    public int care;               // 맑은 날 뚜껑을 열어 둔 날 수
    public int rain;               // 비·눈 오는 날 뚜껑을 열어 둔 날 수
    public bool Empty => startDay < 0;
}

[System.Serializable]
public class JangState
{
    public const int Jars = 3;
    public const int RackSize = 4;
    public const int DryDays = 3;
    public const int FermentDays = 8;
    public const int SoybeansPerMeju = 4;
    public const int SaltPerJar = 2;

    public List<int> hung = new List<int>();          // 걸어 둔 메주마다 다 마르는 날
    public JarState[] jars;
    public bool lidsOpen;
    public bool fireToday;                            // 오늘 아궁이에 불을 땠는지 (밤에 방이 뜨끈함)

    public void Ensure()
    {
        if (hung == null) hung = new List<int>();
        if (jars == null || jars.Length != Jars)
        {
            var old = jars;
            jars = new JarState[Jars];
            for (int i = 0; i < Jars; i++) jars[i] = old != null && i < old.Length && old[i] != null ? old[i] : new JarState();
        }
    }

    // ---------------------------------------------------------------- 메주 걸이
    public int RackRoom { get { Ensure(); return RackSize - hung.Count; } }
    public int DriedCount(int today) { Ensure(); return hung.FindAll(d => today >= d).Count; }

    // 생메주를 걸어 둠: 실제로 건 개수
    public int Hang(int count, int today)
    {
        Ensure();
        int n = System.Math.Min(count, RackRoom);
        for (int i = 0; i < n; i++) hung.Add(today + DryDays);
        return n;
    }

    // 다 마른 메주를 걷음: 걷은 개수
    public int TakeDried(int today)
    {
        Ensure();
        int n = DriedCount(today);
        hung.RemoveAll(d => today >= d);
        return n;
    }

    // ---------------------------------------------------------------- 장독
    public int EmptyJar() { Ensure(); for (int i = 0; i < Jars; i++) if (jars[i].Empty) return i; return -1; }
    public bool IsReady(int i, int today) => !jars[i].Empty && today - jars[i].startDay >= FermentDays;
    public int ReadyJar(int today) { Ensure(); for (int i = 0; i < Jars; i++) if (IsReady(i, today)) return i; return -1; }
    public int Fermenting() { Ensure(); int n = 0; foreach (JarState j in jars) if (!j.Empty) n++; return n; }

    // 빈 독에 장을 담금 (재료는 부르는 쪽에서 뺌): 담근 독 번호, 빈 독이 없으면 -1
    public int Start(int today)
    {
        int i = EmptyJar();
        if (i < 0) return -1;
        jars[i] = new JarState { startDay = today };
        return i;
    }

    // 하루가 끝날 때: 그날 날씨와 뚜껑으로 돌봄을 셈. 오늘 불 땐 기록은 지움
    public void AdvanceNight(bool rainOrSnowToday)
    {
        Ensure();
        foreach (JarState j in jars)
        {
            if (j.Empty || !lidsOpen) continue;
            if (rainOrSnowToday) j.rain++; else j.care++;
        }
        fireToday = false;
    }

    // 장 가르기: 된장, 간장 개수. 돌본 날이 많을수록 늘고, 빗물이 두 번 넘게 들면 하나 줄어듦 (최소 1)
    public static (int doenjang, int ganjang) Yield(JarState j)
    {
        int bonus = (j.care >= 3 ? 1 : 0) + (j.care >= 6 ? 1 : 0) - (j.rain >= 2 ? 1 : 0);
        int d = System.Math.Max(1, 2 + bonus);
        int g = System.Math.Max(1, 1 + bonus);
        return (d, g);
    }

    // 맛 한 마디 (돌봄에 따라)
    public static string Taste(JarState j)
    {
        if (j.rain >= 2) return Loc.T("A little watery, but it's still yours.", "조금 싱겁지만, 그래도 내가 담근 장이다.");
        if (j.care >= 6) return Loc.T("Deep and rich. The sun is in it.", "깊고 진하다. 볕이 다 들어 있다.");
        if (j.care >= 3) return Loc.T("A good, honest taste.", "구수하고 정직한 맛이다.");
        return Loc.T("Mild. Next time I'll let it see more sun.", "순한 맛. 다음엔 볕을 더 쬐어 줘야지.");
    }

    public int Split(int i, out int doenjang, out int ganjang)
    {
        (doenjang, ganjang) = Yield(jars[i]);
        jars[i] = new JarState();
        return i;
    }
}
