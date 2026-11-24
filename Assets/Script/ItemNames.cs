using System.Collections.Generic;

// 아이템 이름 (화면에 보이는 이름). 아이템의 ID는 영어 이름 그대로이고(저장 파일, 레시피, 상점이 이걸 씀),
// 한국어일 때만 아래 이름으로 보여 줍니다. 새 아이템을 ItemDB에 넣으면 여기에도 한 줄 넣어 주세요.
public static class ItemNames
{
    static readonly Dictionary<string, string> Ko = new Dictionary<string, string>
    {
        // 도구
        { "Hoe", "괭이" }, { "Fertile Hoe", "기름진 괭이" }, { "Watering Can", "물뿌리개" }, { "Copper Watering Can", "구리 물뿌리개" },
        { "Scythe", "낫" }, { "Mattock", "곡괭이" }, { "Fishing Rod", "낚싯대" },
        // 씨앗
        { "Carrot Seed", "당근 씨앗" }, { "Potato Seed", "씨감자" }, { "Corn Seed", "옥수수 씨앗" }, { "Strawberry Seed", "딸기 모종" },
        { "Sunflower Seed", "해바라기 씨앗" }, { "Pumpkin Seed", "호박 씨앗" }, { "Acorn", "도토리" },
        // 작물, 열매
        { "Carrot", "당근" }, { "Potato", "감자" }, { "Corn", "옥수수" }, { "Strawberry", "딸기" }, { "Sunflower", "해바라기" },
        { "Pumpkin", "호박" }, { "Apple", "사과" }, { "Pear", "배" }, { "Persimmon", "감" }, { "Dried Persimmon", "곶감" },
        // 음식
        { "Bread", "빵" }, { "Roasted Carrot", "당근 구이" }, { "Baked Potato", "군감자" }, { "Corn Soup", "옥수수 수프" },
        { "Strawberry Jam", "딸기잼" }, { "Grilled Fish", "생선구이" }, { "Carrot Cake", "당근 케이크" }, { "Pumpkin Pie", "호박 파이" },
        { "Spring Greens", "봄나물 무침" }, { "Morel Soup", "곰보버섯 수프" },
        // 채집물
        { "Wild Garlic", "달래" }, { "Fern Sprout", "고사리" }, { "Morel", "곰보버섯" }, { "Wild Raspberry", "산딸기" }, { "Watercress", "물냉이" },
        // 물고기
        { "Minnow", "피라미" }, { "Carp", "잉어" }, { "Golden Koi", "황금 잉어" }, { "Old Boot", "낡은 장화" },
        { "Bluegill", "블루길" }, { "Moonfin", "달빛지느러미" }, { "Glassfish", "유리고기" },
        // 자원, 특별한 것
        { "Wood", "나무" }, { "Stone", "돌" }, { "Campfire", "모닥불" }, { "Compost", "퇴비" },
        { "Sunshine Shard", "햇살 조각" }, { "Earth's Breath", "대지의 숨결" }, { "Star Fragment", "별 조각" },
    };

    // 지금 언어로 보이는 이름
    public static string Display(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        id = ItemDB.Normalize(id);
        return Loc.Ko && Ko.TryGetValue(id, out string ko) ? ko : id;
    }
}
