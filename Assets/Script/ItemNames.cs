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
        { "Rice Cake", "떡" }, { "Roasted Carrot", "당근 구이" }, { "Baked Potato", "군감자" }, { "Steamed Corn", "찐 옥수수" },
        { "Strawberry Jam", "딸기잼" }, { "Grilled Fish", "생선구이" }, { "Carrot Pancake", "당근전" }, { "Pumpkin Porridge", "호박죽" },
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

    // 설명 (한국어). 영어 설명은 ItemDB의 description
    static readonly Dictionary<string, string> DescKo = new Dictionary<string, string>
    {
        { "Hoe", "텃밭 이랑을 갈아요." },
        { "Fertile Hoe", "한 번에 세 칸을 갈아요." },
        { "Watering Can", "작물에 물을 줘요. 우물에서 채워요." },
        { "Copper Watering Can", "한 번에 세 칸에 물을 줘요." },
        { "Scythe", "주변의 다 익은 작물을 한꺼번에 거둬요." },
        { "Rice Cake", "방앗간에서 갓 뽑은 쫄깃한 떡. 든든해요." },
        { "Steamed Corn", "껍질째 쪄서 달고 짭짤한 옥수수. 여름 맛이에요." },
        { "Sunshine Shard", "작물에 쓰면 황금빛으로 익고 기쁨이 더해져요." },
        { "Earth's Breath", "작물에 쓰면 하루만큼 바로 자라요." },
        { "Fishing Rod", "개울이나 저수지에서 E를 눌러 낚시해요." },
        { "Carrot Pancake", "복순 할머니 비법. 가장자리는 바삭, 가운데는 따끈." },
        { "Pumpkin Porridge", "새알심 동동 띄운 노란 호박죽. 풍년의 맛이에요." },
        { "Compost", "더미에서 잘 삭은 퇴비. 회색으로 굳은 묵정밭에 뿌리면 되살아나요." },
        { "Star Fragment", "떨어진 별 한 조각. 정령이 아주 좋아해요 (네 가지 색 모두)." },
        { "Mattock", "그루터기, 바위, 쓰러진 통나무를 파내요." },
        { "Acorn", "숲 빈터에 심고 며칠 물을 주세요." },
        { "Spring Greens", "숲 바닥에서 갓 캔 봄나물." },
        { "Morel Soup", "흙내 나고 따뜻한 수프. 숲길을 걷는 맛이에요." },
        { "Moonfin", "밤에만 물어요." },
        { "Glassfish", "속이 훤히 비쳐요. 맑은 물에서만 살아요." },
        { "Persimmon", "달고 말랑해요. 노을 언덕의 곶감 걸이에 걸면 곶감이 돼요." },
        { "Dried Persimmon", "가을바람에 천천히 말린 쫀득하고 달콤한 곶감." },
        { "Campfire", "요리할 때 필요해요." },
    };

    public static string Description(ItemDef def)
    {
        if (def == null) return "";
        return Loc.Ko && DescKo.TryGetValue(def.name, out string ko) ? ko : def.description;
    }

    // 한국어 조사: 마지막 글자에 받침이 있으면 withBatchim ("을", "이", "은", "과"), 없으면 without ("를", "가", "는", "와")
    public static string Josa(string word, string withBatchim, string without)
    {
        if (string.IsNullOrEmpty(word)) return withBatchim;
        char last = word[word.Length - 1];
        if (last < 0xAC00 || last > 0xD7A3) return withBatchim;
        return (last - 0xAC00) % 28 != 0 ? withBatchim : without;
    }

    // 아이템 이름 + 조사 (한국어용): ItemNames.With("Carrot", "을", "를") → "당근을"
    public static string With(string id, string withBatchim, string without)
    {
        string name = Display(id);
        return name + Josa(name, withBatchim, without);
    }

    // 지금 언어로 보이는 이름
    public static string Display(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        id = ItemDB.Normalize(id);
        return Loc.Ko && Ko.TryGetValue(id, out string ko) ? ko : id;
    }
}
