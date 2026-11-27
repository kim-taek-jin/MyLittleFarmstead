using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// N-1: 밤 화면.
//  - 카메라 앞에 화면만 한 '덮개' 그림을 하나 두고, 시간대 색(NightCurve)으로 덮습니다.
//  - 불빛(창, 가로등, 아궁이, 달집, 쥐불, 주인공 주변)이 있는 곳은 덜 덮고 따뜻한 색을 얹습니다.
//  - 덮개는 월드 그림이라 화면 UI(HUD, 단축칸, 대화창, 메뉴)는 어두워지지 않습니다.
//  - 2D 조명 설정이나 셰이더에 기대지 않아서 맥에서 따로 설정할 것이 없습니다.
//  - 방 안(집, 가게, 회관)은 어둡게 하지 않고 백열등처럼 따뜻한 빛만 살짝.
[DefaultExecutionOrder(1000)]   // 카메라가 움직인 뒤에 덮개를 맞춤
public class NightLight : MonoBehaviour
{
    private static NightLight instance;

    public const int TexelsPerUnit = 16;          // 덮개 한 칸 = 월드 그림 2픽셀
    public const float PlayerRadius = 1.9f;
    public const float PlayerLight = 0.34f;
    public const float IndoorWarm = 0.10f;
    public const float WarmMax = 0.48f;           // 불빛 한가운데 따뜻한 색 세기

    public static readonly Color WindowColor = new Color(1f, 0.74f, 0.40f);
    public static readonly Color LampColor = new Color(1f, 0.90f, 0.62f);
    public static readonly Color FireColor = new Color(1f, 0.56f, 0.22f);
    static readonly Color IndoorColor = new Color(1f, 0.72f, 0.42f);

    private SpriteRenderer sr;
    private Texture2D tex;
    private Color32[] pixels;
    private float[] lightSum, wr, wg, wb, tw;
    private int texW, texH;

    // 지금 화면이 얼마나 밤인지 (눈·비 그림을 어둡게 할 때 씀): 0 = 낮
    public static float Dim { get; private set; }

    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[NightLight]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<NightLight>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameObject view = new GameObject("NightOverlay");
        view.transform.SetParent(transform, false);
        sr = view.AddComponent<SpriteRenderer>();
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (unlit != null) sr.sharedMaterial = new Material(unlit);   // 2D 조명을 받지 않게 (없으면 기본 머티리얼)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;   // 맨 위 정렬 층
        sr.sortingOrder = 32000;
        sr.enabled = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Farm" || scene.name == "Market") PlaceSceneLights(scene);
    }

    // ---------------------------------------------------------------- 장면마다 놓는 불빛
    static void PlaceSceneLights(Scene scene)
    {
        var lights = new List<(Vector2 at, float r, float i, Color c, float flicker)>();
        if (scene.name == "Farm" && FarmMap.Map != null)
        {
            // 우리 집 창호지 (기와집 앞면: 가운데 두 짝 문과 양쪽 작은 창). 그림: kfarm.py house()
            Vector2 door = FarmMap.Anchor("door", new Vector2(3.5f, -0.15f));
            lights.Add((door + new Vector2(0.03f, 1.30f), 1.25f, 0.85f, WindowColor, 0f));
            lights.Add((door + new Vector2(-1.32f, 1.36f), 0.8f, 0.7f, WindowColor, 0f));
            lights.Add((door + new Vector2(1.36f, 1.36f), 0.8f, 0.7f, WindowColor, 0f));
            lights.Add((door + new Vector2(0f, -0.1f), 2.0f, 0.32f, WindowColor, 0f));   // 마당에 비치는 불빛

            // 전봇대 보안등: farm.json의 '|' 칸마다 (그림: kvillage.pole(lamp=True), 왼쪽으로 뻗은 등)
            MapData m = FarmMap.Map;
            for (int r = 0; r < m.rows.Length; r++)
                for (int c = 0; c < m.rows[r].Length; c++)
                    if (m.rows[r][c] == '|') AddPoleLamp(lights, PoleLampHead(m, c, r), PoleGround(m, c, r));
        }
        if (scene.name == "Market")
        {
            // 가게 문과 창 (그림: kmarket.py, 585x328 = 18.28 x 10.24칸)
            lights.Add((new Vector2(-6.07f, 2.70f), 1.3f, 0.72f, WindowColor, 0f));   // 방앗간 열린 셔터
            lights.Add((new Vector2(-1.39f, 2.90f), 1.0f, 0.66f, WindowColor, 0f));   // 철물점 유리문
            lights.Add((new Vector2(7.51f, 2.90f), 1.15f, 0.74f, WindowColor, 0f));   // 행복슈퍼 유리문
            lights.Add((new Vector2(7.20f, -2.27f), 1.15f, 0.74f, WindowColor, 0f));   // 마을회관 유리문
            lights.Add((new Vector2(-6.07f, 1.55f), 1.5f, 0.28f, WindowColor, 0f));   // 문 앞 바닥
            lights.Add((new Vector2(-1.39f, 1.75f), 1.3f, 0.26f, WindowColor, 0f));
            lights.Add((new Vector2(7.51f, 1.75f), 1.4f, 0.28f, WindowColor, 0f));
            lights.Add((new Vector2(7.20f, -3.35f), 1.4f, 0.28f, WindowColor, 0f));
            AddPoleLamp(lights, new Vector2(-4.13f, 3.83f), new Vector2(-4.13f, 1.70f));   // 확성기 전봇대의 보안등
        }

        var root = new GameObject("[NightLights]");
        SceneManager.MoveGameObjectToScene(root, scene);
        foreach (var l in lights)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(l.at.x, l.at.y, 0f);
            NightGlow g = go.AddComponent<NightGlow>();
            g.radius = l.r; g.intensity = l.i; g.color = l.c; g.flicker = l.flicker;
        }
    }

    static void AddPoleLamp(List<(Vector2, float, float, Color, float)> lights, Vector2 head, Vector2 ground)
    {
        lights.Add((head, 0.6f, 0.95f, LampColor, 0f));
        lights.Add((Vector2.Lerp(head, ground, 0.62f), 2.0f, 0.45f, LampColor, 0f));
        lights.Add((ground + new Vector2(0f, 0.3f), 1.5f, 0.32f, LampColor, 0f));
    }

    // 칸 (c, r)의 전봇대: 그림 아래 가운데 = 칸 아래 가운데에서 2픽셀 위, 등은 그 왼쪽 6픽셀, 70픽셀 위
    public static Vector2 PoleLampHead(MapData m, int c, int r) =>
        new Vector2(m.originX + (c * 32 + 10) / 32f, m.originY - (r * 32 - 38) / 32f);
    public static Vector2 PoleGround(MapData m, int c, int r) =>
        new Vector2(m.originX + (c * 32 + 10) / 32f, m.originY - (r * 32 + 30) / 32f);

    // ---------------------------------------------------------------- 매 프레임
    static bool Outdoors(string scene) => (scene == "Farm" || scene == "Market") && !InteriorRooms.Inside;
    static bool Indoors(string scene) => scene == "Home" || ((scene == "Farm" || scene == "Market") && InteriorRooms.Inside);

    void LateUpdate()
    {
        Camera cam = Camera.main;
        GameTimeManager time = GameTimeManager.instance;
        DataManager data = DataManager.instance;
        string scene = SceneManager.GetActiveScene().name;
        bool outdoors = Outdoors(scene), indoors = Indoors(scene);
        bool hidden = cam == null || !cam.orthographic || time == null || StarHill.Active || TitleScreen.Open || Opening.Open || (!outdoors && !indoors);

        float hour = time != null ? time.hour + time.minute / 60f : 12f;
        bool weather = data != null && (data.raining || data.snowing);
        bool moon = data != null && time != null && MoonTonight(data.story.rites, time.day, time.hour);
        NightCurve.Sample(hour, weather, moon, out float nr, out float ng, out float nb, out float a);
        float night = NightCurve.Nightness(hour);
        Dim = hidden || indoors ? 0f : a;

        if (hidden || (indoors && night <= 0f) || (outdoors && a <= 0.003f))
        {
            sr.enabled = false;
            return;
        }

        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Fit(Mathf.CeilToInt(halfW * 2f * TexelsPerUnit) + 8, Mathf.CeilToInt(halfH * 2f * TexelsPerUnit) + 8);   // 가장자리 여유 4칸씩

        // 덮개를 월드 그림 2픽셀 칸에 맞춰 놓아서 카메라가 움직여도 불빛 띠가 떨리지 않게
        Vector3 cp = cam.transform.position;
        float step = 1f / TexelsPerUnit;
        float ox = Mathf.Floor((cp.x - halfW) / step) * step - 4f * step, oy = Mathf.Floor((cp.y - halfH) / step) * step - 4f * step;

        if (indoors)
        {
            Color32 c = new Color(IndoorColor.r, IndoorColor.g, IndoorColor.b, IndoorWarm * night);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        }
        else
        {
            System.Array.Clear(lightSum, 0, lightSum.Length);
            System.Array.Clear(wr, 0, wr.Length); System.Array.Clear(wg, 0, wg.Length); System.Array.Clear(wb, 0, wb.Length); System.Array.Clear(tw, 0, tw.Length);

            PlayerAction p = PlayerAction.instance;
            if (p != null) Splat(ox, oy, (Vector2)p.transform.position + new Vector2(0f, 0.35f), PlayerRadius, PlayerLight, Color.white, 0f);
            float tnow = Time.time;
            foreach (NightGlow g in NightGlow.All)
            {
                if (g == null || !g.Active) continue;
                float f = g.flicker > 0f ? 1f + g.flicker * (Mathf.PerlinNoise(tnow * 3.1f, g.seed) - 0.5f) * 2f : 1f;
                Splat(ox, oy, g.Center, g.radius * f, g.intensity * g.Alpha * night, g.color, g.tint);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                float l = NightCurve.Band(lightSum[i] > 1f ? 1f : lightSum[i]);
                float warm = 0f, r = 0f, gg = 0f, b = 0f;
                if (tw[i] > 0f)
                {
                    r = wr[i] / tw[i]; gg = wg[i] / tw[i]; b = wb[i] / tw[i];
                    warm = NightCurve.Band(tw[i] > 1f ? 1f : tw[i]) * WarmMax * night;
                }
                NightCurve.Mix(nr, ng, nb, a, l, r, gg, b, warm, out float cr, out float cg, out float cb, out float ca);
                pixels[i] = new Color32((byte)(cr * 255f), (byte)(cg * 255f), (byte)(cb * 255f), (byte)(ca * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false);

        sr.enabled = true;
        // 텍스처 왼쪽 아래 = (ox, oy), 한 칸 = 1/16 월드
        sr.transform.position = new Vector3(ox + texW * step * 0.5f, oy + texH * step * 0.5f, cp.z + 1f);
    }

    // 불빛 하나를 덮개 칸에 더함
    void Splat(float ox, float oy, Vector2 at, float radius, float intensity, Color color, float tint)
    {
        if (intensity <= 0f || radius <= 0f) return;
        float step = 1f / TexelsPerUnit;
        int x0 = Mathf.Max(0, Mathf.FloorToInt((at.x - radius - ox) / step));
        int x1 = Mathf.Min(texW - 1, Mathf.CeilToInt((at.x + radius - ox) / step));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((at.y - radius - oy) / step));
        int y1 = Mathf.Min(texH - 1, Mathf.CeilToInt((at.y + radius - oy) / step));
        float inv = 1f / radius;
        for (int y = y0; y <= y1; y++)
        {
            float dy = (oy + (y + 0.5f) * step - at.y) * inv;
            for (int x = x0; x <= x1; x++)
            {
                float dx = (ox + (x + 0.5f) * step - at.x) * inv;
                float v = NightCurve.Falloff(Mathf.Sqrt(dx * dx + dy * dy)) * intensity;
                if (v <= 0f) continue;
                int i = y * texW + x;
                lightSum[i] += v;
                float t = v * tint; tw[i] += t; wr[i] += t * color.r; wg[i] += t * color.g; wb[i] += t * color.b;
            }
        }
    }

    void Fit(int w, int h)
    {
        if (tex != null && w == texW && h == texH) return;
        texW = w; texH = h;
        if (tex != null) Destroy(tex);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[w * h];
        lightSum = new float[w * h]; wr = new float[w * h]; wg = new float[w * h]; wb = new float[w * h]; tw = new float[w * h];
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), TexelsPerUnit, 0, SpriteMeshType.FullRect);
        sr.transform.localScale = Vector3.one;
    }

    // 대보름 밤: 달집 태우는 날 저녁부터 새벽까지
    public static bool MoonTonight(RiteState r, int day, int hour) =>
        r != null && r.boreumDay > 0 && day == r.boreumDay && hour >= 17;
}

// 밤에 빛나는 것 하나. 그림(watch)이 있으면 그 그림이 보이고 이름에 needName이 있을 때만 빛납니다 (예: "fire").
public class NightGlow : MonoBehaviour
{
    public static readonly List<NightGlow> All = new List<NightGlow>();

    public SpriteRenderer watch;
    public string needName;
    public Vector2 offset;                 // watch가 있으면 그림 아래 가운데에서, 없으면 이 물체 자리에서
    public float radius = 1.5f;
    public float intensity = 1f;
    public float flicker;                  // 불꽃 일렁임 (반지름이 이만큼 커졌다 작아졌다)
    public float tint = 1f;                // 따뜻한 색을 얼마나 얹을지
    public Color color = NightLight.WindowColor;
    [System.NonSerialized] public float seed;

    public static NightGlow Add(GameObject host, SpriteRenderer watch, string needName, Vector2 offset, float radius, float intensity, Color color, float flicker)
    {
        NightGlow g = host.AddComponent<NightGlow>();
        g.watch = watch; g.needName = needName; g.offset = offset;
        g.radius = radius; g.intensity = intensity; g.color = color; g.flicker = flicker;
        return g;
    }

    void Awake() => seed = Random.value * 100f;
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Active
    {
        get
        {
            if (watch == null) return true;
            if (!watch.enabled || !watch.gameObject.activeInHierarchy || watch.sprite == null) return false;
            return string.IsNullOrEmpty(needName) || watch.sprite.name.Contains(needName);
        }
    }

    public float Alpha => watch != null ? watch.color.a : 1f;

    public Vector2 Center
    {
        get
        {
            if (watch == null) return (Vector2)transform.position + offset;
            Bounds b = watch.bounds;
            return new Vector2(b.center.x, b.min.y) + offset;
        }
    }
}
