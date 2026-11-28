using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 화면과 오프닝이 함께 쓰는 UI 도구 (픽셀 HUD와 같은 갈색/금색). 장면 파일은 건드리지 않고 실행 중에 만듭니다.
public static class ScreenUI
{
    public static readonly Color Fill = new Color(20 / 255f, 14 / 255f, 11 / 255f, 0.96f);
    public static readonly Color Border = new Color(200 / 255f, 149 / 255f, 82 / 255f, 1f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.29f, 1f);
    public static readonly Color Text = new Color(0.965f, 0.937f, 0.886f, 1f);
    public static readonly Color Dim = new Color(0.78f, 0.75f, 0.69f, 1f);

    // 화면 전체를 덮는 캔버스 (sortingOrder가 클수록 위)
    public static RectTransform Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Object.DontDestroyOnLoad(go);
        UnityEngine.Canvas c = go.AddComponent<UnityEngine.Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIScale.Reference;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return (RectTransform)go.transform;
    }

    public static RectTransform Rect(string name, Transform parent, Color color, bool raycast = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    // 가운데 기준 배치
    public static void At(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, float size, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        PixelFont.Apply(t);
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    // 나무색 테두리 + 어두운 속
    public static RectTransform Box(string name, Transform parent, Color border, Color fill, float edge = 4f)
    {
        RectTransform b = Rect(name, parent, border);
        RectTransform inner = Rect("Fill", b, fill);
        Stretch(inner, edge);
        return b;
    }

    // 장면마다 EventSystem이 하나씩 들어 있고, 이 코드가 만든 것(장면을 넘어 남음)이 따로 있어서
    // 장터에 들어가면 둘이 되었음 ("There are 2 event systems"). 장면이 열릴 때마다 하나만 남김.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void KeepOneEventSystemOnLoad()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => KeepOneEventSystem();
        KeepOneEventSystem();
    }

    public static void KeepOneEventSystem()
    {
        EventSystem[] all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (all.Length <= 1) return;
        EventSystem keep = null;
        foreach (EventSystem es in all)                                       // 장면을 넘어 남는 것이 있으면 그걸 남김
            if (es.gameObject.scene.name == "DontDestroyOnLoad") { keep = es; break; }
        if (keep == null) keep = all[0];
        foreach (EventSystem es in all)
        {
            if (es == keep) continue;
            es.enabled = false;                                               // 이번 프레임부터 바로 빠지게
            Object.Destroy(es.gameObject);
        }
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    // 픽셀 그림 한 장을 화면 이미지로 (점 필터 그대로)
    public static Image Picture(string name, Transform parent, string key)
    {
        RectTransform rt = Rect(name, parent, Color.white);
        Image img = rt.GetComponent<Image>();
        img.sprite = PixelArt.Load("World/" + key);
        img.preserveAspect = false;
        return img;
    }

    public static bool Confirm() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                                    || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
}
