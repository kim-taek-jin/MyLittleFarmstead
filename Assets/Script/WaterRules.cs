// W-1: 물 마시기 규칙 (Unity 없이 — 로직 테스트에서도 씁니다).
//  - 목마름은 게임 시간 1시간에 5씩 (예전 7): 아침에 한 번, 저녁에 한 번 마시면 되는 정도.
//  - 물을 가득 채우는 곳: 농장 우물, 뒷산 샘, 장터 마을회관 옆 수돗가.
//  - 보리차: 행복슈퍼 병 보리차, 볶은 보리를 사서 아궁이에서 끓인 집 보리차 (마음 +1).
//  - 복순 할머니 식혜: 목이 마른 채로 할머니에게 말을 걸면 하루 한 번 한 그릇.
public static class WaterRules
{
    public const float ThirstPerGameHour = 5f;

    public const string BottledTea = "Barley Tea";
    public const string HomemadeTea = "Homemade Barley Tea";
    public const string RoastedBarley = "Roasted Barley";
    public const int BottledTeaPrice = 30;
    public const int RoastedBarleyPrice = 15;
    public const int TeaPerBrew = 3;           // 볶은 보리 1 + 나무 1 → 집 보리차 3병

    public const string SikhyeGiver = "rosa";
    public const float SikhyeBelow = 0.6f;     // 목마름이 60% 아래일 때
    public const float SikhyeThirst = 45f, SikhyeHunger = 5f, SikhyeHeart = 1f;

    public static bool CanGetSikhye(StoryState s, string villagerId, int day, float thirst01) =>
        s != null && villagerId == SikhyeGiver && s.sikhyeDay != day && thirst01 < SikhyeBelow;

    public static void GiveSikhye(StoryState s, int day) { if (s != null) s.sikhyeDay = day; }

    // 들고 있는 것 중 먼저 마실 보리차 (집 보리차가 먼저)
    public static string TeaToDrink(System.Func<string, bool> has)
    {
        if (has(HomemadeTea)) return HomemadeTea;
        if (has(BottledTea)) return BottledTea;
        return null;
    }

    // 목이 마를 때 안내: 지금 있는 곳에 맞춰서
    public static string ThirstHint(string scene, bool hasTea)
    {
        if (hasTea) return Loc.T("You're thirsty. Drink your barley tea. (Right-click / F)", "목이 말라요. 보리차를 마셔요. (오른쪽 클릭 / F)");
        if (scene == "Market") return Loc.T("You're thirsty. Drink at the tap beside the village hall (E).", "목이 말라요. 마을회관 옆 수돗가에서 물을 마셔요 (E).");
        return Loc.T("You're thirsty. Drink at the well (E).", "목이 말라요. 우물에서 물을 마셔요 (E).");
    }

    public static string[] SikhyeLines() => new[]
    {
        Loc.T("You look parched. Here, have a bowl of sikhye before you go. I made it this morning.",
              "목마르제? 이거 한 그릇 묵고 가라. 아침에 해 놓은 식혜여."),
        Loc.T("(Sweet, cold rice punch with rice grains floating on top. It goes down in one breath.)",
              "(밥알이 동동 뜬 달고 시원한 식혜. 한숨에 다 마셨어요.)"),
    };
}
