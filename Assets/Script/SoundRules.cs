// S-1/S-2: 어떤 음악과 바깥 소리를 낼지 정하는 규칙 (Unity 없이 — 로직 테스트에서도 씁니다).
//  음악 (국악풍, Tools/audio/make_music.py가 만듦)
//   - title: 타이틀·오프닝 / spring: 데모 끝 / boreum: 대보름 밤 달집 둘레 (풍물)
//   - farm: 농장·집 낮. 세 겹: 0 가야금(늘), 1 대금+생황, 2 장구+북. 세상 색(마음)이 돌아올수록 한 겹씩 들어옴
//   - market: 장터 낮. 장날이면 장구+꽹과리 겹이 들어옴
//   - 저녁 6시부터 7시까지 서서히 줄고, 밤(7시~새벽 6시)에는 음악 없이 바깥 소리만
//  바깥 소리: 겨울바람(낮), 밤바람, 아궁이·달집 불 (가까울수록 크게), 가끔 까치(아침)·먼 개 짖는 소리(밤)·새벽 닭
public static class SoundRules
{
    public const string Title = "title", Farm = "farm", Market = "market", Boreum = "boreum", Spring = "spring";

    public static int Layers(string piece) => piece == Farm ? 3 : piece == Market ? 2 : 1;

    // 지금 틀 곡 (null = 음악 없음)
    public static string Piece(string scene, bool titleOrOpening, bool demoEnd, bool boreumGathering, float hour)
    {
        if (demoEnd) return Spring;
        if (titleOrOpening) return Title;
        if (boreumGathering) return Boreum;
        if (hour >= 19f || hour < 6f) return null;
        if (scene == "Market") return Market;
        if (scene == "Farm" || scene == "Home") return Farm;
        return null;
    }

    // 곡 전체 크기: 해 질 녘(18~19시)에 줄고, 아침(6~7시)에 서서히. 방 안에서는 작게.
    public static float Level(string piece, float hour, bool indoors)
    {
        if (piece == null) return 0f;
        if (piece == Title || piece == Spring || piece == Boreum) return 1f;
        float k = 1f;
        float h = hour % 24f;
        if (h >= 18f && h < 19f) k = 1f - (h - 18f);
        else if (h >= 6f && h < 7f) k = h - 6f;
        if (indoors) k *= 0.55f;
        return Clamp01(k);
    }

    // 겹마다 크기. color01 = 세상 색 (DataManager.VisibleColor01: 겨울엔 대보름 전까지 반까지만)
    public static float LayerVolume(string piece, int layer, float color01, bool marketDay)
    {
        if (layer == 0) return 1f;
        if (piece == Farm)
        {
            if (layer == 1) return Smooth((color01 - 0.18f) / 0.14f);   // 색이 돌기 시작하면 대금과 생황
            if (layer == 2) return Smooth((color01 - 0.36f) / 0.12f);   // 반쯤 돌아오면 장구
        }
        if (piece == Market && layer == 1) return marketDay ? 1f : 0f;
        return 0f;
    }

    // ---------------------------------------------------------------- 바깥 소리
    public static bool Night(float hour) { float h = hour % 24f; return h >= 19f || h < 5f; }

    public static float WindVolume(bool outdoors, bool indoors, bool winter, bool snowing, float hour)
    {
        if (!winter || Night(hour)) return 0f;
        if (outdoors) return snowing ? 0.55f : 0.32f;
        return indoors ? 0.08f : 0f;
    }

    public static float NightVolume(bool outdoors, bool indoors, float hour)
    {
        if (!Night(hour)) return 0f;
        return outdoors ? 0.45f : indoors ? 0.1f : 0f;
    }

    // 불 (아궁이, 달집): 가까울수록. distance = 가장 가까운 불까지 (없으면 큰 값)
    public static float FireVolume(float distance, float reach = 6f) => Clamp01(1f - distance / reach) * 0.7f;

    // 가끔 나는 소리: 지금 낼 만한 것 (Ambience가 몇십 초마다 하나 고름)
    public static string Occasional(bool outdoors, float hour, float roll)
    {
        if (!outdoors) return null;
        float h = hour % 24f;
        if (h >= 6f && h < 10f) return roll < 0.6f ? "magpie" : null;         // 아침 까치 (반가운 소식)
        if (Night(hour)) return roll < 0.5f ? "dog_far" : null;               // 밤에 먼 데서 개
        return null;
    }

    // 새벽 닭: 하루 한 번, 6시를 조금 넘겨 (방 안에서도 멀리 들림)
    public static bool RoosterNow(float hour, int day, int lastDay) => day != lastDay && hour >= 6.05f && hour < 7f;

    // 발소리: 방 안은 장판, 겨울 바깥은 눈, 그 밖엔 흙
    public static string Step(bool indoors, bool winter) => indoors ? "step_floor" : winter ? "step_snow" : "step_dirt";

    // 먹고 마실 때: 마실 것이면 꿀꺽, 아니면 냠냠
    public static string EatSound(float hunger, float thirst) => thirst > hunger ? "gulp" : "eat";

    static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    static float Smooth(float t) { t = Clamp01(t); return t * t * (3f - 2f * t); }
}
