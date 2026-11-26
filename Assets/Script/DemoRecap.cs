using System.Collections.Generic;

// 데모 끝 화면의 "올겨울의 나" (K-5): 저장된 기록으로 쓰는 한 장. 유니티에 의존하지 않음 (테스트됨).
public static class DemoRecap
{
    public static List<string> Lines(StoryState s, int days)
    {
        var lines = new List<string>();
        lines.Add(Loc.T($"You lived in the village for {days} days.", $"이 마을에서 {days}일을 살았어요."));
        RiteState r = s.rites;
        if (r != null && !string.IsNullOrEmpty(r.dongjiMemory))
            lines.Add(Loc.T($"On the solstice, rolling rice balls, you remembered \"{Rites.MemoryText(r.dongjiMemory)}\".",
                            $"동짓날 새알심을 빚으며 '{Rites.MemoryText(r.dongjiMemory)}'을(를) 떠올렸어요."));
        if (r != null && !string.IsNullOrEmpty(r.burned))
            lines.Add(Loc.T($"You let the moon house fire take {LowerFirst(Rites.BurnText(r.burned))}.", $"달집에 {Rites.BurnText(r.burned)}을(를) 태워 보냈어요."));
        if (r != null && !string.IsNullOrEmpty(r.wish))
            lines.Add(Loc.T($"Your wish paper said: \"{Rites.WishText(r.wish)}\".", $"소원지에는 '{Rites.WishText(r.wish)}'라고 적었어요."));

        var restored = new List<string>();
        int close = 0;
        foreach (VillagerDef d in VillagerDB.All)
        {
            VillagerProgress p = s.Get(d.id);
            if (Neighbors.Restored(d, p)) restored.Add(d.name);
            else if (p.wishIndex > 0) close++;
        }
        if (restored.Count > 0)
            lines.Add(Loc.T($"Color came back to {string.Join(", ", restored)}.", $"{string.Join(", ", restored)}에게 색이 돌아왔어요."));
        if (close > 0)
            lines.Add(Loc.T($"You grew closer to {close} more neighbor(s). Their stories go on in spring.", $"이웃 {close}명과도 가까워졌어요. 이야기는 봄에 이어져요."));

        JangState j = s.jang;
        if (j != null && j.madeDoenjang + j.madeGanjang > 0)
            lines.Add(Loc.T($"You made your own sauces: {j.madeDoenjang} doenjang, {j.madeGanjang} ganjang.", $"손수 장을 담가 된장 {j.madeDoenjang}, 간장 {j.madeGanjang}을 얻었어요."));
        else if (j != null && j.Fermenting() > 0)
            lines.Add(Loc.T("Sauce is still ripening in your jars.", "장독에서는 장이 익어 가고 있어요."));

        if (s.saechamCount > 0 || s.masilCount > 0)
            lines.Add(Loc.T($"You shared {s.saechamCount} work-break meal(s) and spent {s.masilCount} evening(s) visiting.",
                            $"새참을 {s.saechamCount}번 나눠 먹고, 저녁 마실을 {s.masilCount}번 갔어요."));
        return lines;
    }

    // 정식판에서 이어지는 것
    public static List<string> FullGame() => new List<string>
    {
        Loc.T("Spring: flower pancakes on Samjinnal, the back hill wakes up", "봄: 삼짇날 진달래 화전, 깨어나는 뒷산"),
        Loc.T("Summer: washing your hair in iris water on Dano, the reservoir", "여름: 단오 창포물에 머리 감기, 저수지"),
        Loc.T("Autumn: the Chuseok full moon, Sunset Hill, kimchi-making with the whole village", "가을: 추석 보름달, 노을 언덕, 온 마을 김장"),
        Loc.T("Every neighbor's spring, summer and autumn stories", "이웃마다 봄·여름·가을 이야기"),
    };

    static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
}
