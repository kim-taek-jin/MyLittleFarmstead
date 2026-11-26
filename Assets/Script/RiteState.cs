using System.Collections.Generic;

// K-4: 절기 의식 (축제가 아님: 가게, 대회, 점수, 실패 없음). 함께 준비 → 의식 → 나에게 하는 일.
// 유니티에 의존하지 않는 규칙만 담았습니다 (장면은 RiteScenes).
//
// 동지 (7일째)
//  - 5~7일 낮: 방앗간 팥죽 솥에서 [E] 세 번 (팥 고르기, 팥 거르기, 새알심 빚기) → 새알심을 빚으며 도시의 기억 하나를 고름
//  - 7일: 할머니 팥죽 다섯 그릇 → 이웃 네 집에 한 그릇씩 + 우리 집 대문에 팥 뿌리기 (액막이)
//  - 놓쳐도 벌 없음
// 정월대보름 (데모의 마지막)
//  - 마음이 봄 문턱에 닿으면 봄이 바로 오지 않고, 이틀 뒤(빨라도 12일째)가 대보름으로 정해짐
//  - 그동안 농장 남쪽 빈 논에 달집 쌓기 (나무 5, 이장님 품앗이를 했으면 3)
//  - 대보름 아침: 미나와 부럼 깨기. 밤(19시~): 쥐불놀이 → 달맞이 → 달집에 도시의 물건 하나 태우기 → 소원지
//  - 대보름 밤을 보내야 다음 날 아침 봄이 옴. 그날 밤을 놓쳐도 달집은 그대로, 다음 저녁에 해도 됨
[System.Serializable]
public class RiteState
{
    // 동지
    public int dongjiPrep;                                   // 팥죽 솥에서 거든 횟수 (0~3)
    public string dongjiMemory;                              // 새알심을 빚으며 떠올린 도시의 기억
    public bool dongjiPot;                                   // 팥죽 다섯 그릇을 받았는지
    public List<string> dongjiDelivered = new List<string>(); // 이웃 id, 그리고 "gate"
    public bool dongjiDone;

    // 정월대보름
    public int boreumDay = -1;                               // 정해진 날 (-1: 아직)
    public int daljipWood;
    public bool bureom, jwibul, lit;
    public string burned;                                    // 달집에 넣은 것
    public string wish;                                      // 소원지
    public bool boreumDone;

    public void Ensure() { if (dongjiDelivered == null) dongjiDelivered = new List<string>(); }
}

public static class Rites
{
    // ---------------------------------------------------------------- 동지
    public const int DongjiDay = 7, DongjiPrepFrom = 5, PrepSteps = 3, Bowls = 5;
    public static readonly string[] Neighbors4 = { "rosa", "theo", "mina", "mayor" };

    public static bool DongjiWindow(int day) => day >= DongjiPrepFrom && day <= DongjiDay;
    public static bool CanPrep(RiteState r, int day, int hour) => DongjiWindow(day) && hour >= 6 && hour < 20 && r.dongjiPrep < PrepSteps && !r.dongjiDone;
    public static bool PrepStep(RiteState r) { r.dongjiPrep++; return r.dongjiPrep >= PrepSteps; }
    public static bool CanTakePot(RiteState r, int day) => day == DongjiDay && r.dongjiPrep >= PrepSteps && !r.dongjiPot && !r.dongjiDone;

    public static bool CanDeliver(RiteState r, int day, string who)
    {
        r.Ensure();
        return day == DongjiDay && r.dongjiPot && !r.dongjiDone && !r.dongjiDelivered.Contains(who);
    }

    // 한 그릇 나름: 다섯 곳(이웃 넷 + 대문) 다 했으면 true
    public static bool Deliver(RiteState r, string who)
    {
        r.Ensure();
        if (!r.dongjiDelivered.Contains(who)) r.dongjiDelivered.Add(who);
        r.dongjiDone = r.dongjiDelivered.Count >= Bowls;
        return r.dongjiDone;
    }

    public static bool DongjiPassed(RiteState r, int day) => day > DongjiDay && !r.dongjiDone;

    // ---------------------------------------------------------------- 정월대보름
    public const int BoreumEarliest = 12, Notice = 2, DaljipWood = 5, DaljipWoodHelped = 3;

    // 겨울 → 봄은 대보름 밤을 보낸 뒤에만
    public static bool HoldSpring(RiteState r) => !r.boreumDone;

    // 마음이 봄 문턱에 닿은 아침: 대보름 날을 정함 (한 번만). newDay = 막 시작된 날
    public static bool Schedule(RiteState r, int newDay)
    {
        if (r.boreumDay >= 0) return false;
        r.boreumDay = System.Math.Max(newDay + Notice, BoreumEarliest);
        return true;
    }

    public static bool Scheduled(RiteState r) => r.boreumDay >= 0 && !r.boreumDone;
    public static int WoodNeeded(bool helped) => helped ? DaljipWoodHelped : DaljipWood;
    public static bool Built(RiteState r, bool helped) => r.daljipWood >= WoodNeeded(helped);
    public static bool CanBuild(RiteState r, int day, bool helped) => Scheduled(r) && day >= r.boreumDay - Notice && !Built(r, helped);

    // 나무를 보탬: 실제로 넣은 개수
    public static int AddWood(RiteState r, int have, bool helped)
    {
        int n = System.Math.Max(0, System.Math.Min(have, WoodNeeded(helped) - r.daljipWood));
        r.daljipWood += n;
        return n;
    }

    public static bool BureomTime(RiteState r, int day, int hour) => Scheduled(r) && day >= r.boreumDay && hour >= 6 && hour < 18 && !r.bureom;
    public static bool Night(RiteState r, int day, int hour) => Scheduled(r) && day >= r.boreumDay && hour >= 19;
    // 대보름 밤: 이웃들이 논의 달집 둘레에 모여 있음
    public static bool Gathering(RiteState r, int day, int hour, bool helped) => Night(r, day, hour) && Built(r, helped);

    public static readonly string[] BurnIds = { "badge", "overtime", "mail" };
    public static readonly string[] WishIds = { "sleep", "laugh", "eat", "rest" };
    public static readonly string[] MemoryIds = { "dinner", "train", "office" };

    public static void Finish(RiteState r) { r.lit = true; r.boreumDone = true; }

    // 소원지가 봄부터 조용히 지켜 주는 것 (마음 +1)
    public static bool WishKept(string wish, DayFacts f)
    {
        switch (wish)
        {
            case "sleep": return f.onTime && !f.passedOut;
            case "laugh": return f.masil;
            case "eat": return f.meals >= 1;
            case "rest": return f.rested;
            default: return false;
        }
    }

    // ---------------------------------------------------------------- 글 (한국어/영어)
    public static string MemoryText(string id)
    {
        switch (id)
        {
            case "dinner": return Loc.T("Convenience-store dinners, eaten alone", "혼자 먹던 편의점 저녁");
            case "train": return Loc.T("The last train home", "막차 지하철");
            default: return Loc.T("Office lights that never went off", "꺼지지 않던 사무실 불");
        }
    }

    public static string MemoryDiary(string id)
    {
        switch (id)
        {
            case "dinner": return Loc.T("Rolling rice balls, I remembered all those dinners eaten alone. Tonight I ate with someone.", "새알심을 빚다가 혼자 먹던 편의점 저녁이 떠올랐다. 오늘은 누군가와 같이 먹었다.");
            case "train": return Loc.T("Rolling rice balls, I remembered the last train home. I won't need to run for it anymore.", "새알심을 빚다가 막차 지하철이 떠올랐다. 이제 그걸 타려고 뛸 일은 없다.");
            default: return Loc.T("Rolling rice balls, I remembered the office lights that never went off. Here, the lights go out at night.", "새알심을 빚다가 꺼지지 않던 사무실 불이 떠올랐다. 여기는 밤이 되면 불이 꺼진다.");
        }
    }

    public static string BurnText(string id)
    {
        switch (id)
        {
            case "badge": return Loc.T("My company ID badge", "사원증");
            case "overtime": return Loc.T("My overtime log", "야근 일지");
            default: return Loc.T("A printout of unread emails", "안 읽은 메일 출력물");
        }
    }

    public static string BurnLine(string id)
    {
        switch (id)
        {
            case "badge": return Loc.T("The badge that hung around my neck curls up in the fire. My neck feels lighter.", "목에 걸려 있던 사원증이 불 속에서 오그라든다. 목이 가벼워진다.");
            case "overtime": return Loc.T("Page after page of late nights goes up in sparks. Every one of those hours was mine.", "늦은 밤들이 적힌 종이가 불티가 되어 올라간다. 그 시간들은 다 내 것이었다.");
            default: return Loc.T("Three hundred unread emails, and none of them can reach me here.", "안 읽은 메일 삼백 통. 여기까지는 아무것도 닿지 못한다.");
        }
    }

    public static string WishText(string id)
    {
        switch (id)
        {
            case "sleep": return Loc.T("I want to sleep well", "푹 자고 싶다");
            case "laugh": return Loc.T("I want to laugh with someone", "누군가와 웃고 싶다");
            case "eat": return Loc.T("I want to eat slowly", "천천히 밥 먹고 싶다");
            default: return Loc.T("It's okay to do nothing", "아무것도 안 해도 괜찮다");
        }
    }

    public static string WishKeptLine(string id)
    {
        switch (id)
        {
            case "sleep": return Loc.T("Just like I wrote on my wish paper, I slept well.", "소원지에 쓴 대로, 푹 잤다.");
            case "laugh": return Loc.T("Just like I wrote on my wish paper, I laughed with someone.", "소원지에 쓴 대로, 누군가와 웃었다.");
            case "eat": return Loc.T("Just like I wrote on my wish paper, I ate slowly.", "소원지에 쓴 대로, 천천히 밥을 먹었다.");
            default: return Loc.T("Just like I wrote on my wish paper, I did nothing for a while. It was fine.", "소원지에 쓴 대로, 한동안 아무것도 안 했다. 괜찮았다.");
        }
    }

    // 동지 이웃마다 팥죽을 받을 때 한 마디
    public static string DongjiLine(string who)
    {
        switch (who)
        {
            case "rosa": return Loc.T("Me too? Ha, first time in years someone's served me a bowl. ...It's sweet.", "나도? 허허, 남이 퍼 주는 팥죽은 몇 해 만이네. ...달다.");
            case "theo": return Loc.T("Red beans keep the bad stuff away, they say. ...Thanks. I'll eat it while it's hot.", "팥이 나쁜 걸 쫓는다지. ...고맙다. 식기 전에 먹을게.");
            case "mina": return Loc.T("Grandma says I have to eat as many rice balls as I am old! ...That's nine. I can do it.", "할머니가 새알심은 나이만큼 먹는 거래! ...아홉 개. 할 수 있어.");
            default: return Loc.T("Ah, patjuk on the winter solstice! Now it really feels like the village is alive.", "아이고, 동짓날 팥죽이여! 이제야 마을이 사는 것 같구먼.");
        }
    }

    // 아침 방송
    public static string Broadcast(RiteState r, int day)
    {
        if (!r.dongjiDone && day == DongjiPrepFrom)
            return Loc.T("The winter solstice is the day after tomorrow. Grandma Boksun is making red bean porridge at the mill. Lend a hand sorting beans, eh?",
                         "모레가 동지입니다. 복순 할매 방앗간에서 팥죽 쑵니다. 팥 고르는 일손 좀 보태 주시고요.");
        if (!r.dongjiDone && day == DongjiDay)
            return Loc.T("Today is the winter solstice. Share a bowl of red bean porridge, and don't forget to scatter beans at your gate.",
                         "오늘은 동지입니다. 팥죽 한 그릇씩 나눠 드시고, 대문에 팥 뿌리는 것도 잊지 마시고요.");
        if (!Scheduled(r)) return null;
        int left = r.boreumDay - day;
        if (left == 2) return Loc.T("The first full moon, Jeongwol Daeboreum, is in two days! Bring wood to the empty paddy south of the new house for the moon-house bonfire.",
                                    "이틀 뒤가 정월대보름입니다! 새로 이사 온 집 남쪽 빈 논에 달집 쌓을 나무 좀 모아 주십시오.");
        if (left == 1) return Loc.T("Tomorrow is Daeboreum. Anyone helping stack the moon house, head down to the paddy.", "내일이 대보름입니다. 달집 쌓는 거 거들어 주실 분은 논으로 나오시고요.");
        if (left == 0) return Loc.T("Today is Jeongwol Daeboreum! Crack your nuts this morning, and at seven tonight we light the moon house in the paddy!",
                                    "오늘이 정월대보름입니다! 아침엔 부럼 깨시고, 저녁 일곱 시에 논에서 달집 태웁니다!");
        return Loc.T("The moon house is still standing. Come down to the paddy this evening, even if it's a day late.", "달집은 그대로 있습니다. 하루 늦었어도 오늘 저녁에 논으로 나오십시오.");
    }
}
