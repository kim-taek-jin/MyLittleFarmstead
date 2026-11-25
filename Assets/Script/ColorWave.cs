using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// 색이 번지는 순간 (D-2, 다시 만듦). 마음이 오르면 세상의 진짜 색이 나를 중심으로 동그랗게 번져 나갑니다.
//  - 방법: 채도 효과(포스트 프로세싱)를 끈 두 번째 카메라가 같은 화면을 텍스처에 그리고, 그걸 화면 위에
//    둥근 마스크로 잘라 보여 줍니다. 회색 세상 위로 원 안쪽만 원래 색이 보이고, 원이 화면 끝까지 커졌다가
//    네 단계로 옅어지며 새 채도의 세상으로 가라앉습니다. 원 가장자리는 픽셀 디더링.
//  - 가장자리에 작은 반짝임 몇 개. 계절이 바뀐 아침에만 그 계절의 꽃잎/잎을 몇 장 (봄 분홍, 여름 초록, 가을 단풍).
//  - 다른 창(아침 요약, 대화, 풍경, 타이틀)이 떠 있으면 기다렸다가 닫히면 번집니다.
public class ColorWave : MonoBehaviour
{
    static ColorWave instance;
    static bool pending, pendingBig;

    RectTransform root, maskRT, worldRT, sparkRoot;
    Mask mask;
    Image maskImage;
    RawImage world;
    Camera revealCam;
    RenderTexture rt;
    float age, grow, hold, fade;
    bool playing, big;
    Vector2 center;
    float maxRadius;
    readonly List<(RectTransform rt, Image img, float ang, float phase)> sparks = new List<(RectTransform, Image, float, float)>();
    readonly List<(RectTransform rt, Image img, Vector2 v, float spin)> leaves = new List<(RectTransform, Image, Vector2, float)>();

    public static void Request(bool bigWave)
    {
        Ensure();
        pending = true;
        pendingBig |= bigWave;
    }

    static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("[ColorWave]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ColorWave>();
        instance.Build();
    }

    void Build()
    {
        root = ScreenUI.Canvas("ColorWaveCanvas", 300);          // 세상 위, HUD(500) 아래
        root.SetParent(transform, false);
        Destroy(root.GetComponent<GraphicRaycaster>());          // 클릭은 그대로 아래로

        maskRT = new GameObject("Mask", typeof(RectTransform)).GetComponent<RectTransform>();
        maskRT.SetParent(root, false);
        maskRT.anchorMin = maskRT.anchorMax = new Vector2(0.5f, 0.5f);
        maskImage = maskRT.gameObject.AddComponent<Image>();
        maskImage.sprite = CircleSprite();
        maskImage.raycastTarget = false;
        mask = maskRT.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        worldRT = new GameObject("World", typeof(RectTransform)).GetComponent<RectTransform>();
        worldRT.SetParent(maskRT, false);
        worldRT.anchorMin = worldRT.anchorMax = new Vector2(0.5f, 0.5f);
        world = worldRT.gameObject.AddComponent<RawImage>();
        world.raycastTarget = false;

        sparkRoot = new GameObject("Sparks", typeof(RectTransform)).GetComponent<RectTransform>();
        sparkRoot.SetParent(root, false);
        sparkRoot.anchorMin = sparkRoot.anchorMax = new Vector2(0.5f, 0.5f);
        root.gameObject.SetActive(false);
    }

    // 가장자리가 디더링된 흰 원 (512px, 점 필터): 마스크로 써서 원 안만 보이게
    static Sprite CircleSprite()
    {
        const int size = 512;
        int[,] bayer = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        float r = size * 0.5f, edge = 36f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                bool on = d <= r - edge || (d <= r && (r - d) / edge * 16f > bayer[(y / 2) % 4, (x / 2) % 4]);
                px[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool CanPlay() => Time.timeScale > 0f && !HUD.AnnouncementOpen && !HUD.DialogueOpen && !PixelHUD.ViewOpen
                             && !TitleScreen.Blocking && !PauseMenu.Open;

    void Update()
    {
        if (pending && !playing && CanPlay())
        {
            bool b = pendingBig;
            pending = pendingBig = false;
            Play(b);
        }
        if (!playing) return;

        Camera main = Camera.main;
        if (main == null || revealCam == null) { Stop(); return; }
        Follow(main);

        age += Time.unscaledDeltaTime;
        Vector2 canvasSize = root.rect.size;
        // 원: 처음엔 빠르게, 끝으로 갈수록 천천히 커짐
        float g = Mathf.Clamp01(age / grow);
        float radius = maxRadius * (1f - (1f - g) * (1f - g));
        maskRT.anchoredPosition = center;
        maskRT.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        // 안의 화면은 늘 화면 전체에 맞춤 (원이 어디 있든 같은 자리)
        worldRT.sizeDelta = canvasSize;
        worldRT.anchoredPosition = -center;

        // 다 퍼진 뒤 잠깐 머물다가 네 단계로 옅어짐
        float a = 1f;
        float after = age - grow - hold;
        if (after > 0f) a = Mathf.Round((1f - Mathf.Clamp01(after / fade)) * 4f) / 4f;
        world.color = new Color(1f, 1f, 1f, a);

        // 가장자리 반짝임: 원을 따라 돌며 깜빡임
        float edgeA = g < 1f ? 1f : Mathf.Max(0f, 1f - (age - grow) / 0.4f);
        foreach (var s in sparks)
        {
            float ang = s.ang + age * 0.25f;
            Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius * 0.97f;
            s.rt.anchoredPosition = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);
            float tw = Mathf.Sin(age * 10f + s.phase) > 0.2f ? 1f : 0.5f;
            s.img.color = new Color(1f, 0.97f, 0.85f, Mathf.Round(edgeA * tw * 4f) / 4f);
        }
        // 계절 꽃잎/잎: 바깥으로 천천히 흩날림
        foreach (var l in leaves)
        {
            l.rt.anchoredPosition += l.v * Time.unscaledDeltaTime;
            l.rt.anchoredPosition = new Vector2(Mathf.Round(l.rt.anchoredPosition.x / 4f) * 4f, Mathf.Round(l.rt.anchoredPosition.y / 4f) * 4f);
            l.rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Round(age * l.spin / 45f) * 45f);
            Color c = l.img.color; c.a = a; l.img.color = c;
        }

        if (after >= fade) Stop();
    }

    void Play(bool bigWave)
    {
        Camera main = Camera.main;
        if (main == null) return;
        big = bigWave;
        grow = big ? 2.0f : 1.3f;
        hold = big ? 0.6f : 0.3f;
        fade = big ? 1.0f : 0.7f;
        age = 0f;

        EnsureCamera(main);
        Follow(main);
        root.gameObject.SetActive(true);
        world.texture = rt;
        world.color = Color.white;

        // 시작점: 화면 위 플레이어 자리, 끝: 가장 먼 화면 모서리까지
        center = Vector2.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, main.WorldToScreenPoint(player.transform.position), null, out center);
        Vector2 half = root.rect.size * 0.5f;
        maxRadius = 0f;
        foreach (Vector2 corner in new[] { new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y), new Vector2(-half.x, half.y), half })
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(center, corner));
        maxRadius *= 1.08f;                                          // 디더링 가장자리까지 화면 밖으로

        ClearBits();
        int n = big ? 14 : 9;
        for (int i = 0; i < n; i++) AddSpark(i / (float)n * Mathf.PI * 2f, Random.value * 6f);
        if (big) AddLeaves();

        Sfx.Play("heart", big ? 1f : 0.75f);
        playing = true;
    }

    void Stop()
    {
        playing = false;
        root.gameObject.SetActive(false);
        if (revealCam != null) revealCam.enabled = false;
        ClearBits();
    }

    // ---------------------------------------------------------------- 채도 효과 없이 그리는 두 번째 카메라
    void EnsureCamera(Camera main)
    {
        int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
        if (rt == null || rt.width != w || rt.height != h)
        {
            if (rt != null) rt.Release();
            rt = new RenderTexture(w, h, 24) { filterMode = FilterMode.Point, name = "ColorReveal" };
        }
        if (revealCam == null)
        {
            var go = new GameObject("[ColorRevealCamera]");
            DontDestroyOnLoad(go);
            revealCam = go.AddComponent<Camera>();
            UniversalAdditionalCameraData data = revealCam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = false;      // 회색으로 만드는 채도 효과를 끔 = 진짜 색
        }
        revealCam.targetTexture = rt;
        revealCam.enabled = true;
    }

    void Follow(Camera main)
    {
        revealCam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
        revealCam.orthographic = main.orthographic;
        revealCam.orthographicSize = main.orthographicSize;
        revealCam.fieldOfView = main.fieldOfView;
        revealCam.nearClipPlane = main.nearClipPlane;
        revealCam.farClipPlane = main.farClipPlane;
        revealCam.cullingMask = main.cullingMask;
        revealCam.clearFlags = main.clearFlags;
        revealCam.backgroundColor = main.backgroundColor;
        revealCam.rect = main.rect;
    }

    // WorldColor가 장면마다 모든 카메라의 후처리를 켜므로, 이 카메라는 다시 끔
    void LateUpdate()
    {
        if (revealCam == null) return;
        UniversalAdditionalCameraData data = revealCam.GetUniversalAdditionalCameraData();
        if (data != null && data.renderPostProcessing) data.renderPostProcessing = false;
    }

    // ---------------------------------------------------------------- 반짝임, 계절 꽃잎
    void AddSpark(float ang, float phase)
    {
        var go = new GameObject("Spark", typeof(RectTransform));
        RectTransform s = go.GetComponent<RectTransform>();
        s.SetParent(sparkRoot, false);
        s.sizeDelta = new Vector2(20f, 20f);
        // 작은 네 갈래 별: 가로 막대 + 세로 막대
        Image h = ScreenUI.Rect("H", s, Color.white).GetComponent<Image>();
        ((RectTransform)h.transform).sizeDelta = new Vector2(20f, 4f);
        Image v = ScreenUI.Rect("V", s, Color.white).GetComponent<Image>();
        ((RectTransform)v.transform).sizeDelta = new Vector2(4f, 20f);
        Image core = go.AddComponent<Image>();
        core.color = Color.clear;
        core.raycastTarget = false;
        sparks.Add((s, h, ang, phase));
        sparks.Add((s, v, ang, phase));
    }

    void AddLeaves()
    {
        DataManager data = DataManager.instance;
        Season s = data != null ? data.CurrentSeason : Season.Spring;
        Color[] cols = s == Season.Summer ? new[] { new Color(0.35f, 0.65f, 0.28f), new Color(0.58f, 0.84f, 0.42f) }
                     : s == Season.Autumn ? new[] { new Color(0.93f, 0.54f, 0.17f), new Color(0.86f, 0.23f, 0.24f), new Color(0.94f, 0.81f, 0.28f) }
                     : new[] { new Color(1f, 0.71f, 0.82f), new Color(1f, 0.88f, 0.92f) };        // 봄: 벚꽃 분홍
        for (int i = 0; i < 16; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            RectTransform l = ScreenUI.Rect("Leaf", sparkRoot, cols[i % cols.Length]);
            l.sizeDelta = new Vector2(12f, 8f);
            l.anchoredPosition = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (40f + Random.value * 60f);
            leaves.Add((l, l.GetComponent<Image>(), new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) - 0.3f) * (120f + Random.value * 160f), 90f + Random.value * 180f));
        }
    }

    void ClearBits()
    {
        foreach (Transform t in sparkRoot) Destroy(t.gameObject);
        sparks.Clear();
        leaves.Clear();
    }
}
