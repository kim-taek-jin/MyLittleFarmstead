// N-1: 시간대별 하늘빛 (Unity 없이 계산만 — 로직 테스트에서도 씁니다).
//  - 17~19시 노을 (주황), 19~21시 푸른 저녁, 21시~새벽 4시 밤, 4~6시 새벽.
//  - 화면 위에 이 색을 a만큼 덮고, 불빛이 있는 곳은 덜 덮습니다 (NightLight).
//  - 밤 어둡기는 '중간' (NightA = 0.58). 비·눈 오는 밤은 조금 더 어둡고, 대보름 밤은 달빛으로 조금 밝습니다.
public static class NightCurve
{
    public const float NightA = 0.58f;        // 한밤 어둡기 (중간)
    public const float WeatherScale = 1.12f;  // 비·눈 오는 밤
    public const float MoonScale = 0.92f;     // 대보름 달빛
    public const float MaxA = 0.68f;

    // 시각, 색(r, g, b), 덮는 정도(a)
    static readonly float[,] Keys =
    {
        { 0.00f, 0.06f, 0.08f, 0.20f, NightA },
        { 4.00f, 0.06f, 0.08f, 0.20f, NightA },
        { 5.00f, 0.20f, 0.24f, 0.46f, 0.38f },   // 새벽: 푸르게 밝아짐
        { 6.00f, 0.95f, 0.72f, 0.58f, 0.10f },   // 해 뜰 무렵 살짝 분홍
        { 6.75f, 1.00f, 0.85f, 0.70f, 0.00f },
        { 16.50f, 1.00f, 0.62f, 0.30f, 0.00f },
        { 17.50f, 1.00f, 0.55f, 0.25f, 0.11f },  // 노을
        { 18.50f, 0.86f, 0.38f, 0.30f, 0.19f },
        { 19.25f, 0.34f, 0.28f, 0.50f, 0.31f },  // 푸른 저녁
        { 20.25f, 0.12f, 0.14f, 0.34f, 0.46f },
        { 21.00f, 0.06f, 0.08f, 0.20f, NightA },
        { 24.00f, 0.06f, 0.08f, 0.20f, NightA },
    };

    // hour: 0~26 (24 이상은 자정 넘긴 새벽)
    public static void Sample(float hour, bool weather, bool moon, out float r, out float g, out float b, out float a)
    {
        float h = hour % 24f;
        if (h < 0f) h += 24f;
        int n = Keys.GetLength(0);
        int i = 0;
        while (i < n - 2 && h >= Keys[i + 1, 0]) i++;
        float t = Keys[i + 1, 0] > Keys[i, 0] ? (h - Keys[i, 0]) / (Keys[i + 1, 0] - Keys[i, 0]) : 0f;
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
        t = t * t * (3f - 2f * t);
        r = Keys[i, 1] + (Keys[i + 1, 1] - Keys[i, 1]) * t;
        g = Keys[i, 2] + (Keys[i + 1, 2] - Keys[i, 2]) * t;
        b = Keys[i, 3] + (Keys[i + 1, 3] - Keys[i, 3]) * t;
        a = Keys[i, 4] + (Keys[i + 1, 4] - Keys[i, 4]) * t;

        float night = Nightness(hour);
        if (weather) a *= 1f + (WeatherScale - 1f) * night;
        if (moon)
        {
            a *= 1f + (MoonScale - 1f) * night;
            r += (0.10f - r) * night; g += (0.15f - g) * night; b += (0.34f - b) * night;   // 달빛은 더 푸르고 밝게
        }
        if (a > MaxA) a = MaxA;
    }

    // 0 = 낮, 1 = 밤. 불빛(창, 가로등, 아궁이)이 얼마나 보이는지.
    public static float Nightness(float hour)
    {
        float h = hour % 24f;
        if (h < 0f) h += 24f;
        if (h >= 20.5f || h < 4.5f) return 1f;
        if (h >= 18f) return Smooth((h - 18f) / 2.5f);
        if (h >= 6.5f) return 0f;
        return 1f - Smooth((h - 4.5f) / 2f);
    }

    static float Smooth(float t)
    {
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
        return t * t * (3f - 2f * t);
    }

    // 불빛 세기를 띠로 나눕니다 (부드러운 번짐 대신 픽셀 그림 같은 동그란 띠).
    public const int Bands = 6;
    public static float Band(float light)
    {
        if (light <= 0f) return 0f;
        if (light >= 1f) return 1f;
        return (float)System.Math.Round(light * Bands) / Bands;
    }

    // 한 불빛이 거리 d (반지름 R에 대한 비율 t = d / R)에서 주는 빛: 가운데 1, 가장자리 0
    public static float Falloff(float t)
    {
        if (t >= 1f) return 0f;
        float k = 1f - t * t;
        return k * k;
    }

    // 덮개 한 칸의 색: 어둠(a)을 불빛(light)만큼 걷어내고, 따뜻한 색(warm, 0~1)을 얹습니다.
    public static void Mix(float nr, float ng, float nb, float a, float light, float wr, float wg, float wb, float warm,
                           out float r, out float g, out float b, out float outA)
    {
        float dark = a * (1f - light);
        outA = dark + warm;
        if (outA <= 0.0001f) { r = g = b = 0f; outA = 0f; return; }
        r = (nr * dark + wr * warm) / outA;
        g = (ng * dark + wg * warm) / outA;
        b = (nb * dark + wb * warm) / outA;
        if (outA > 1f) outA = 1f;
    }
}
