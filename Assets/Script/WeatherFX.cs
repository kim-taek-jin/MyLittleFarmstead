using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 비와 눈을 화면에 그립니다 (야외 씬: Farm, Market). 코드로 만든 화면 오버레이라 씬 설정이 필요 없습니다.
// 픽셀 느낌을 살리려고 작은 사각형 몇십 개만 씁니다.
public class WeatherFX : MonoBehaviour
{
    private static WeatherFX instance;
    private const int Count = 80;

    private RectTransform[] drops;
    private Image[] images;
    private Vector2[] speed;
    private RectTransform root;
    private bool lastSnow;
    private bool visible = true;

    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[WeatherFX]", typeof(RectTransform));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WeatherFX>();
    }

    void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;   // HUD(500) 아래, 게임 화면 위
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root = (RectTransform)transform;

        drops = new RectTransform[Count];
        images = new Image[Count];
        speed = new Vector2[Count];
        for (int i = 0; i < Count; i++)
        {
            GameObject d = new GameObject("p", typeof(RectTransform));
            d.transform.SetParent(transform, false);
            images[i] = d.AddComponent<Image>();
            images[i].raycastTarget = false;
            drops[i] = (RectTransform)d.transform;
            drops[i].anchorMin = drops[i].anchorMax = new Vector2(0f, 0f);
            Respawn(i, true, false);
        }
    }

    void Respawn(int i, bool anywhere, bool snow)
    {
        float x = Random.Range(-100f, 2020f);
        float y = anywhere ? Random.Range(0f, 1100f) : Random.Range(1090f, 1200f);
        drops[i].anchoredPosition = new Vector2(x, y);
        if (snow)
        {
            float s = Random.value < 0.3f ? 8f : 5f;
            drops[i].sizeDelta = new Vector2(s, s);
            images[i].color = new Color(1f, 1f, 1f, 0.85f);
            speed[i] = new Vector2(Random.Range(-20f, 20f), -Random.Range(50f, 110f));
        }
        else
        {
            drops[i].sizeDelta = new Vector2(3f, 22f);
            images[i].color = new Color(0.75f, 0.85f, 1f, 0.55f);
            speed[i] = new Vector2(-120f, -Random.Range(900f, 1200f));
        }
    }

    void Update()
    {
        DataManager data = DataManager.instance;
        string scene = SceneManager.GetActiveScene().name;
        bool outdoors = scene == "Farm" || scene == "Market";
        bool snow = data != null && data.snowing;
        bool rain = data != null && data.raining;
        bool show = outdoors && (snow || rain) && !StarHill.Active;

        if (visible != show)
        {
            visible = show;
            for (int i = 0; i < Count; i++) images[i].enabled = show;
        }
        if (!show) return;

        if (snow != lastSnow)
        {
            for (int i = 0; i < Count; i++) Respawn(i, true, snow);
            lastSnow = snow;
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < Count; i++)
        {
            Vector2 p = drops[i].anchoredPosition;
            Vector2 v = speed[i];
            if (snow) v.x += Mathf.Sin(Time.time * 1.5f + i) * 25f;   // 눈송이는 살랑살랑
            p += v * dt;
            drops[i].anchoredPosition = p;
            if (p.y < -30f || p.x < -150f || p.x > 2100f) Respawn(i, false, snow);
        }
    }
}
