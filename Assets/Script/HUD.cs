using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 코드로 자동 생성되는 화면 안내 UI (씬에 따로 배치할 필요 없음)
//  - 상단 상태줄: 마음, 날씨, 물뿌리개, 들고 있는 아이템
//  - 토스트 메시지: 짧은 알림 (몇 초 뒤 사라짐)
//  - 공지 패널: 하루 요약, 마음 단계 달성 같은 큰 메시지
// GraphicRaycaster가 없어서 마우스 클릭을 막지 않습니다.
public class HUD : MonoBehaviour
{
    private static HUD instance;

    private class ToastEntry
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TextMeshProUGUI text;
        public float timeLeft;
        public float duration;
    }

    private readonly List<ToastEntry> toasts = new List<ToastEntry>();
    private RectTransform root;
    private TextMeshProUGUI statusText;
    private GameObject announcePanel;
    private CanvasGroup announceGroup;
    private TextMeshProUGUI announceText;
    private float announceTimeLeft;
    private float announceShownAt;

    private const int MaxToasts = 4;
    private const float ToastHeight = 46f;

    public static HUD Ensure()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("[HUD]", typeof(RectTransform));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HUD>();
        }
        return instance;
    }

    public static void Toast(string message, float duration = 3.5f)
    {
        if (string.IsNullOrEmpty(message)) return;
        Ensure().AddToast(message, duration);
    }

    public static void Announce(string message, float duration = 5f)
    {
        Ensure().ShowAnnouncement(message, duration);
    }

    public static void SetStatus(string text)
    {
        HUD hud = Ensure();
        if (hud.statusText != null) hud.statusText.text = text;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        Build();
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = (RectTransform)transform;

        // 상단 상태줄
        RectTransform statusBg = CreatePanel("StatusBar", root, new Color(0f, 0f, 0f, 0.45f));
        SetAnchors(statusBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(1000f, 44f));
        statusText = CreateText("StatusText", statusBg, 24f);
        Stretch(statusText.rectTransform);

        // 공지 패널
        RectTransform panel = CreatePanel("Announcement", root, new Color(0.05f, 0.05f, 0.08f, 0.82f));
        SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(820f, 440f));
        announcePanel = panel.gameObject;
        announceGroup = announcePanel.AddComponent<CanvasGroup>();
        announceGroup.blocksRaycasts = false;
        announceText = CreateText("AnnouncementText", panel, 30f);
        Stretch(announceText.rectTransform, 30f);
        announcePanel.SetActive(false);
    }

    void AddToast(string message, float duration)
    {
        // 같은 메시지가 이미 떠 있으면 시간만 갱신 (도배 방지)
        foreach (var t in toasts)
        {
            if (t.text.text == message)
            {
                t.timeLeft = duration;
                return;
            }
        }

        if (toasts.Count >= MaxToasts)
        {
            Destroy(toasts[0].rect.gameObject);
            toasts.RemoveAt(0);
        }

        RectTransform bg = CreatePanel("Toast", root, new Color(0f, 0f, 0f, 0.6f));
        SetAnchors(bg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, ToastHeight - 6f));
        TextMeshProUGUI text = CreateText("Text", bg, 24f);
        Stretch(text.rectTransform, 8f);
        text.text = message;

        ToastEntry entry = new ToastEntry
        {
            rect = bg,
            group = bg.gameObject.AddComponent<CanvasGroup>(),
            text = text,
            timeLeft = duration,
            duration = duration,
        };
        entry.group.blocksRaycasts = false;
        toasts.Add(entry);
    }

    void ShowAnnouncement(string message, float duration)
    {
        if (announcePanel == null) return;
        announceText.text = message;
        announceTimeLeft = duration;
        announceShownAt = Time.unscaledTime;
        announceGroup.alpha = 1f;
        announcePanel.SetActive(true);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            ToastEntry t = toasts[i];
            t.timeLeft -= dt;
            if (t.timeLeft <= 0f)
            {
                Destroy(t.rect.gameObject);
                toasts.RemoveAt(i);
                continue;
            }
            t.group.alpha = Mathf.Clamp01(t.timeLeft / 0.5f);
        }

        for (int i = 0; i < toasts.Count; i++)
        {
            toasts[i].rect.anchoredPosition = new Vector2(0f, -64f - i * ToastHeight);
        }

        if (announcePanel != null && announcePanel.activeSelf)
        {
            announceTimeLeft -= dt;
            announceGroup.alpha = Mathf.Clamp01(announceTimeLeft / 0.6f);
            // 공지가 뜨자마자 같은 입력으로 닫히지 않도록 최소 0.5초는 보여줍니다.
            bool canSkip = Time.unscaledTime - announceShownAt > 0.5f;
            bool skip = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return);
            if (announceTimeLeft <= 0f || (canSkip && skip))
            {
                announcePanel.SetActive(false);
            }
        }
    }

    // ---------- UI 생성 헬퍼 ----------
    static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, max.y >= 1f && min.y >= 1f ? 1f : 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding * 0.5f);
        rect.offsetMax = new Vector2(-padding, -padding * 0.5f);
    }
}
