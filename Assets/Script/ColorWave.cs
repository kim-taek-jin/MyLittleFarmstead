using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 색이 번지는 순간 (D-2). 마음이 오르면 나를 중심으로 색 물결이 동그랗게 퍼져 나가고, 그 사이 세상 채도가
// 잠깐 확 올랐다가 새 값으로 가라앉습니다. 계절이 바뀌는 아침에는 물결 두 겹, 꽃잎도 더 많이.
//  - 물결과 꽃잎은 화면 위 UI(오버레이)라 화면 채도(포스트 프로세싱)에 물들지 않고 늘 컬러로 보입니다.
//  - 다른 창(아침 요약, 대화, 풍경, 타이틀)이 떠 있으면 기다렸다가, 닫히면 그때 퍼집니다.
//  - 픽셀 느낌: 점은 네모, 위치는 4픽셀 단위, 옅어짐은 네 단계.
public class ColorWave : MonoBehaviour
{
    static ColorWave instance;
    static bool pending, pendingBig;

    RectTransform root, origin;
    readonly List<Bit> bits = new List<Bit>();
    float age, duration;

    class Bit
    {
        public RectTransform rt;
        public Image img;
        public Vector2 dir;
        public float speed, delay, wobble, phase;
        public bool ring;
    }

    static readonly Color[] Petals =
    {
        new Color(1f, 0.62f, 0.77f), new Color(1f, 0.85f, 0.29f), new Color(0.58f, 0.84f, 0.42f),
        new Color(0.55f, 0.8f, 1f), new Color(1f, 0.69f, 0.42f), new Color(1f, 1f, 1f),
    };

    // 마음이 올랐을 때 부름. big: 계절이 바뀐 아침
    public static void Request(bool big)
    {
        Ensure();
        pending = true;
        pendingBig |= big;
    }

    static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("[ColorWave]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ColorWave>();
        instance.root = ScreenUI.Canvas("ColorWaveCanvas", 300);     // 세상 위, HUD(500) 아래
        instance.root.SetParent(go.transform, false);
        Object.Destroy(instance.root.GetComponent<GraphicRaycaster>()); // 클릭은 그대로 아래로
    }

    void Update()
    {
        if (pending && CanPlay())
        {
            bool big = pendingBig;
            pending = pendingBig = false;
            Play(big);
        }
        if (bits.Count == 0) return;
        age += Time.unscaledDeltaTime;
        float reach = 1500f;                                   // 화면 끝까지 (1920x1080 기준)
        foreach (Bit b in bits)
        {
            float t = (age - b.delay) / (duration - b.delay);
            if (t < 0f) { b.img.enabled = false; continue; }
            b.img.enabled = t < 1f;
            float r = reach * b.speed * Mathf.Sqrt(Mathf.Clamp01(t));   // 빠르게 퍼지다가 느려짐
            Vector2 side = new Vector2(-b.dir.y, b.dir.x) * Mathf.Sin(t * 9f + b.phase) * b.wobble;
            Vector2 p = b.dir * r + side;
            b.rt.anchoredPosition = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);
            float a = b.ring ? (t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f) : (t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f);
            Color c = b.img.color; c.a = Mathf.Round(Mathf.Clamp01(a) * 4f) / 4f; b.img.color = c;
        }
        if (age >= duration) Clear();
    }

    static bool CanPlay() => Time.timeScale > 0f && !HUD.AnnouncementOpen && !HUD.DialogueOpen && !PixelHUD.ViewOpen
                             && !TitleScreen.Blocking && !PauseMenu.Open;

    void Clear()
    {
        if (origin != null) Destroy(origin.gameObject);    // 점들은 모두 origin 아래에 있음
        origin = null;
        bits.Clear();
    }

    void Play(bool big)
    {
        Clear();
        age = 0f;
        duration = big ? 2.6f : 1.7f;

        // 시작점: 화면 위 플레이어 자리
        Vector2 center = Vector2.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Camera cam = Camera.main;
        if (player != null && cam != null)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, cam.WorldToScreenPoint(player.transform.position), null, out center);
        origin = new GameObject("Wave", typeof(RectTransform)).GetComponent<RectTransform>();
        origin.SetParent(root, false);
        origin.sizeDelta = Vector2.zero;
        origin.anchoredPosition = center;

        // 물결 고리 (큰 물결은 두 겹)
        // 한 겹 = 안쪽 금색 줄 + 바깥 분홍 줄 (눈 덮인 회색 위에서도 보이게 두껍게)
        int rings = big ? 2 : 1;
        for (int k = 0; k < rings; k++)
        {
            int n = 120;
            for (int i = 0; i < n; i++)
            {
                float ang = (i + 0.5f * k) / n * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                AddBit(origin, dir, 1f, k * 0.45f, 0f, 16f, new Color(1f, 0.85f, 0.29f), true);
                AddBit(origin, dir, 1.018f, k * 0.45f, 0f, 12f, new Color(1f, 0.62f, 0.77f), true);
            }
        }
        // 꽃잎과 반짝임
        int petals = big ? 130 : 70;
        for (int i = 0; i < petals; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            AddBit(origin, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)), 0.3f + Random.value * 0.7f, Random.value * 0.35f,
                   10f + Random.value * 18f, Random.value < 0.3f ? 12f : 16f, Petals[i % Petals.Length], false);
        }

        WorldColor.Pulse(big ? 60f : 35f, duration + 0.6f);
        Sfx.Play("heart", big ? 1f : 0.8f);
    }

    void AddBit(RectTransform parent, Vector2 dir, float speed, float delay, float wobble, float size, Color color, bool ring)
    {
        RectTransform rt = ScreenUI.Rect("Bit", parent, color);
        rt.sizeDelta = new Vector2(size, size);
        Image img = rt.GetComponent<Image>();
        img.enabled = false;
        bits.Add(new Bit { rt = rt, img = img, dir = dir, speed = speed, delay = delay, wobble = wobble, phase = Random.value * 6f, ring = ring });
    }
}
