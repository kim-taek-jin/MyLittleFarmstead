using System.Collections.Generic;

// 마을 방송 (K-1). 아침에 전봇대 확성기에서 이장님 목소리로: "아아, 마을 주민 여러분께 알려드립니다~"
// 장날, 날씨, 계절이 바뀐 것, 새로 열린 길, 새로 이사 온 사람 소식. 할 말이 없는 날은 방송도 없습니다.
// 유니티에 의존하지 않는 규칙만 담았습니다 (화면: BroadcastBanner).
public class MorningNews
{
    public int day;
    public Season before, after;
    public bool raining, snowing;
    public bool forestOpened, lakeOpened, hillOpened;
    public bool mejuSeason;        // 겨울인데 아직 장을 안 담갔음 (K-2)
    public bool lidsOpen;          // 장독 뚜껑이 열려 있음
}

public static class VillageBroadcast
{
    public static string Speaker => Loc.T("Village Head Park", "박 이장님");

    public static List<string> Lines(MorningNews n)
    {
        var body = new List<string>();
        if (n.day == 2)
            body.Add(Loc.T("Someone has moved into the old empty house down the lane. Do say hello, everyone.",
                           "이번에 아랫길 빈집에 새로 이사 온 분이 계십니다. 다들 반갑게 맞아 주시고요."));
        if (n.after > n.before)
            body.Add(n.after == Season.Spring ? Loc.T("The air has softened a lot. Looks like spring is on its way.", "날이 많이 풀렸습니다. 봄이 오나 봅니다.")
                   : n.after == Season.Summer ? Loc.T("It's getting properly warm. Drink plenty of water, everyone.", "날이 제법 덥습니다. 물들 많이 드시고요.")
                   : Loc.T("The mornings are crisp now. Autumn has come.", "아침저녁으로 선선합니다. 가을이 왔습니다."));
        else if (n.after < n.before)
            body.Add(Loc.T("A cold snap has come back. Dress warmly and don't push yourselves.", "꽃샘추위가 왔습니다. 따뜻하게 입으시고, 무리들 하지 마십시오."));
        if (n.forestOpened) body.Add(Loc.T("I hear the path up the back hill is open again.", "뒷산 올라가는 길이 다시 열렸다고 합니다."));
        if (n.lakeOpened) body.Add(Loc.T("The reeds by the reservoir have cleared. You can walk down there now.", "저수지 쪽 갈대가 걷혔답니다. 이제 내려가 보셔도 됩니다."));
        if (n.hillOpened) body.Add(Loc.T("The lane up to Sunset Hill is open.", "노을 언덕 올라가는 길이 열렸습니다."));
        if (MarketState.IsMarketDay(n.day))
            body.Add(Loc.T("Today is market day. The stalls are up in the lane, so drop by.", "오늘은 오일장 서는 날입니다. 장터에 한번들 들러 보세요."));
        else if (MarketState.DaysUntilMarket(n.day) == 1)
            body.Add(Loc.T("Tomorrow is market day. Get your things ready to sell.", "내일은 장날입니다. 내다 팔 것들 미리 챙겨 두시고요."));
        if (n.snowing) body.Add(Loc.T("Snow tonight made the roads slippery. Careful out there, especially our elders.", "눈이 와서 길이 미끄럽습니다. 어르신들 특히 조심하십시오."));
        else if (n.raining) body.Add(Loc.T("It's raining, so keep your sauce jar lids closed.", "비가 오니 장독 뚜껑들 꼭 덮어 두시고요."));
        if ((n.raining || n.snowing) && n.lidsOpen)
            body.Add(Loc.T("And whoever left their jar lids open, close them quick!", "장독 뚜껑 열어 둔 집은 얼른 가서 덮으시고요!"));
        if (n.mejuSeason && n.day >= 3 && n.day % 4 == 3)
            body.Add(Loc.T("Winter is meju season. Soybeans and salt are at the corner store in the market lane.", "겨울은 메주 쑤는 철입니다. 메주콩이랑 소금은 장터 행복슈퍼에 있습니다."));

        // 할 말이 없는 날에도 가끔 (사흘에 한 번) 안부 한 마디
        if (body.Count == 0 && n.day > 2 && n.day % 3 == 0)
            body.Add(Loc.T("Nothing special today. Just make sure you all eat well.", "오늘은 별일 없습니다. 다들 밥 잘 챙겨 드시고요."));
        if (body.Count == 0) return body;

        var lines = new List<string> { Loc.T("Ahem, ahem. Attention, everyone in the village.", "아아, 아아. 마을 주민 여러분께 알려드립니다.") };
        lines.AddRange(body);
        lines.Add(Loc.T("That's all from your village head.", "이상, 이장이었습니다."));
        return lines;
    }
}
