using System.Collections.Generic;

// 하루 끝 일기 (K-1). "밥 +3, 일찍 잠 +4" 같은 점수 대신, 그날 있었던 일을 1인칭 일기 두세 줄로 적고
// 마음은 숫자 대신 "조금 가벼워졌다 / 그대로다 / 무거워졌다" 같은 말로 보여 줍니다.
// 유니티에 의존하지 않는 규칙만 담았습니다 (DataManager.EndDay가 아침 요약에 씀).
public class DayFacts
{
    public bool passedOut, lateNight, onTime;
    public int meals, harvested, talked;
    public bool drank, exhausted, starved, rested;
    public float delta;          // 마음 변화 (일기 문장 고르기에만 씀, 화면에는 숫자를 안 보임)
}

public static class DayDiary
{
    // 같은 일이 매일 같은 말로 반복되지 않게, 날짜로 문장을 돌려 씁니다.
    static string Pick(int day, params (string en, string ko)[] options)
    {
        var o = options[((day % options.Length) + options.Length) % options.Length];
        return Loc.T(o.en, o.ko);
    }

    // 일기 줄 (많아야 세 줄): 첫 줄은 하루의 느낌, 다음 줄들은 가장 마음에 남은 일
    public static List<string> Lines(DayFacts f, int day)
    {
        var lines = new List<string>();
        if (f.passedOut)
            lines.Add(Pick(day, ("I passed out in the field. Someone carried me home.", "밭에서 정신을 잃었다. 누군가 집까지 데려다줬다."),
                                ("Everything went dark. I woke up at home, not knowing how.", "눈앞이 캄캄해졌다. 어떻게 집에 왔는지 모르겠다.")));
        else if (f.delta >= 6f)
            lines.Add(Pick(day, ("Today was a pretty good day.", "오늘은 꽤 괜찮은 하루였다."),
                                ("A good day. I want to remember it.", "좋은 하루였다. 기억해 두고 싶다."),
                                ("The day went by gently.", "하루가 순하게 지나갔다.")));
        else if (f.delta >= 0f)
            lines.Add(Pick(day, ("An ordinary day. That's fine too.", "별일 없는 하루. 그것도 괜찮다."),
                                ("A quiet day.", "조용한 하루였다."),
                                ("Nothing special happened. I'm still here.", "특별한 일은 없었다. 그래도 나는 여기 있다.")));
        else
            lines.Add(Pick(day, ("A hard day.", "힘든 하루였다."),
                                ("Today felt heavy.", "오늘은 무거웠다."),
                                ("I didn't take care of myself today.", "오늘은 나를 잘 돌보지 못했다.")));

        // 마음에 남은 일: 나쁜 일을 먼저 (그래야 다음 날 고칠 수 있음), 그다음 좋은 일
        var notes = new List<string>();
        if (f.starved) notes.Add(Pick(day, ("I skipped meals again.", "또 끼니를 걸렀다."), ("My stomach growled all afternoon.", "오후 내내 배가 고팠다.")));
        if (f.exhausted && !f.passedOut) notes.Add(Pick(day, ("I worked until I had nothing left. Like back in the city.", "기력이 바닥날 때까지 일했다. 도시에서처럼."),
                                                           ("I pushed too hard again.", "또 무리했다.")));
        if (f.lateNight) notes.Add(Pick(day, ("I stayed up far too late.", "너무 늦게까지 깨어 있었다."), ("Midnight came and went.", "자정이 넘도록 잠들지 못했다.")));
        if (f.talked > 0) notes.Add(Pick(day, ("I talked with the neighbors for a while.", "이웃과 한참 이야기를 나눴다."),
                                              ("Someone smiled at me today.", "오늘 누군가 나를 보고 웃었다.")));
        if (f.rested) notes.Add(Pick(day, ("I sat down for a while and just watched. The wind was nice.", "잠깐 앉아서 그냥 바라봤다. 바람이 좋았다."),
                                          ("I let myself rest for a bit.", "잠시 쉬어도 된다고 나에게 말해 줬다.")));
        if (f.harvested > 0) notes.Add(Pick(day, ("I picked something I grew with my own hands.", "내 손으로 키운 걸 거뒀다."),
                                                 ("The harvest was small, but it was mine.", "많지는 않아도 내가 거둔 것이다.")));
        if (f.meals >= 2) notes.Add(Pick(day, ("I ate properly, twice.", "밥을 두 번이나 제대로 챙겨 먹었다."), ("Warm food, eaten slowly.", "따뜻한 밥을 천천히 먹었다.")));
        else if (f.meals == 1) notes.Add(Pick(day, ("I ate a proper meal.", "밥을 챙겨 먹었다."), ("I remembered to eat.", "밥 먹는 걸 잊지 않았다.")));
        if (f.onTime && !f.passedOut) notes.Add(Pick(day, ("Going to bed early tonight.", "오늘은 일찍 잔다."), ("Bed before midnight, for once.", "웬일로 자정 전에 눕는다.")));
        if (f.drank && notes.Count < 2) notes.Add(Pick(day, ("The well water was cold and sweet.", "우물물이 차고 달았다."), ("I drank plenty of water.", "물을 넉넉히 마셨다.")));

        for (int i = 0; i < notes.Count && lines.Count < 3; i++) lines.Add(notes[i]);
        return lines;
    }

    // 숫자 대신 마음의 말
    public static string Mood(float delta)
    {
        if (delta >= 6f) return Loc.T("My heart feels much lighter.", "마음이 한결 가벼워졌다.");
        if (delta > 0f) return Loc.T("My heart feels a little lighter.", "마음이 조금 가벼워졌다.");
        if (delta == 0f) return Loc.T("My heart is the same as yesterday.", "마음은 어제와 같다.");
        if (delta > -6f) return Loc.T("My heart feels a little heavier.", "마음이 조금 무거워졌다.");
        return Loc.T("My heart feels heavy.", "마음이 무겁다.");
    }
}
