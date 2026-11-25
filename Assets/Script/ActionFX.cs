using UnityEngine;

// 행동 반응 (D-2): 땅을 갈면 흙먼지, 물을 주면 물방울, 씨앗을 심으면 작은 반짝임, 수확하면 작물이 톡 튀어 오름.
// 모두 잠깐 생겼다 사라지는 작은 픽셀 조각들입니다 (부드러운 흐림 없이, 픽셀 단위로 움직이고 네 단계로 옅어짐).
public class ActionFX : MonoBehaviour
{
    static Sprite dot;                 // 2x2 흰 점 (색은 SpriteRenderer.color로)

    SpriteRenderer[] parts;
    Vector2[] vel;
    Vector3[] pos;
    float life, age, gravity;

    static Sprite Dot()
    {
        if (dot != null) return dot;
        var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        dot = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), PixelArt.PixelsPerUnit);
        return dot;
    }

    static ActionFX Burst(string name, Vector2 at, int n, Color[] colors, float speed, float up, float gravity, float life)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        ActionFX fx = go.AddComponent<ActionFX>();
        fx.parts = new SpriteRenderer[n];
        fx.vel = new Vector2[n];
        fx.pos = new Vector3[n];
        fx.life = life;
        fx.gravity = gravity;
        for (int i = 0; i < n; i++)
        {
            var p = new GameObject("p");
            p.transform.SetParent(go.transform, false);
            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = Dot();
            sr.color = colors[i % colors.Length];
            sr.sortingOrder = 25;
            fx.parts[i] = sr;
            float a = (i + Random.value * 0.6f) / n * Mathf.PI * 2f;
            fx.vel[i] = new Vector2(Mathf.Cos(a) * speed * (0.6f + Random.value * 0.6f), up * (0.6f + Random.value * 0.8f));
            fx.pos[i] = Vector3.zero;
        }
        return fx;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / life;
        float alpha = Mathf.Round((1f - Mathf.Clamp01(t)) * 4f) / 4f;
        for (int i = 0; i < parts.Length; i++)
        {
            vel[i].y -= gravity * Time.deltaTime;
            pos[i] += (Vector3)(vel[i] * Time.deltaTime);
            // 1/32 칸 단위로 끊어서 (픽셀이 번지지 않게)
            parts[i].transform.localPosition = new Vector3(Mathf.Round(pos[i].x * 32f) / 32f, Mathf.Round(pos[i].y * 32f) / 32f, 0f);
            Color c = parts[i].color; c.a = alpha; parts[i].color = c;
        }
        if (t >= 1f) Destroy(gameObject);
    }

    // ---------------------------------------------------------------- 쓰는 곳 (PlayerAction)
    static readonly Color[] Soil = { new Color(0.47f, 0.31f, 0.18f), new Color(0.6f, 0.42f, 0.26f), new Color(0.36f, 0.23f, 0.13f) };
    static readonly Color[] Water = { new Color(0.75f, 0.9f, 1f), new Color(0.35f, 0.64f, 0.89f) };
    static readonly Color[] Sprout = { new Color(0.58f, 0.84f, 0.42f), new Color(1f, 0.95f, 0.6f) };
    static readonly Color[] Chaff = { new Color(0.9f, 0.8f, 0.45f), new Color(0.58f, 0.84f, 0.42f) };

    public static void Dust(Vector2 at) => Burst("Dust", at + new Vector2(0f, -0.25f), 8, Soil, 1.4f, 1.6f, 7f, 0.45f);
    public static void Splash(Vector2 at) => Burst("Splash", at + new Vector2(0f, -0.1f), 7, Water, 1.1f, 2.2f, 9f, 0.5f);
    public static void Seed(Vector2 at) => Burst("Seed", at + new Vector2(0f, -0.1f), 5, Sprout, 0.6f, 1.4f, 4f, 0.5f);
    public static void Swish(Vector2 at) => Burst("Swish", at, 10, Chaff, 2f, 1.2f, 5f, 0.45f);

    // 수확: 작물 그림이 톡 튀어 올랐다가 네 단계로 사라짐
    public static void Pop(Vector2 at, string item)
    {
        Sprite icon = PixelArt.Item(item);
        if (icon == null) { Burst("Pop", at, 6, Sprout, 1f, 2f, 6f, 0.5f); return; }
        var go = new GameObject("Pop");
        go.transform.position = at;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = icon;
        sr.sortingOrder = 26;
        float scale = PixelArt.PixelsPerUnit / icon.pixelsPerUnit * 0.75f;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.AddComponent<PopUp>();
        Burst("PopSpark", at, 6, Sprout, 1.2f, 1.6f, 5f, 0.45f);
    }

    class PopUp : MonoBehaviour
    {
        float age;
        Vector3 start;
        SpriteRenderer sr;
        void Start() { start = transform.position; sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            age += Time.deltaTime;
            const float life = 0.7f;
            float t = age / life;
            float h = 0.9f * (1f - (1f - Mathf.Min(1f, t * 1.6f)) * (1f - Mathf.Min(1f, t * 1.6f)));   // up fast, then hang
            transform.position = start + new Vector3(0f, Mathf.Round(h * 32f) / 32f, 0f);
            float a = t < 0.6f ? 1f : Mathf.Round((1f - (t - 0.6f) / 0.4f) * 4f) / 4f;
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
