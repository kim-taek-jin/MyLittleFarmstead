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

    // dialogue box
    private GameObject dialoguePanel;
    private Image dialoguePortrait;
    private TextMeshProUGUI dialogueName;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI dialogueHint;
    private string[] dialogueLines;
    private int dialogueIndex;
    private System.Action dialogueDone;
    private System.Action choiceYes, choiceNo;
    private bool isChoice;
    private int dialogueOpenedFrame;

    // album
    private GameObject albumPanel;
    private TextMeshProUGUI albumTitle;
    private TextMeshProUGUI albumFooter;
    private readonly Image[] albumPhotos = new Image[6];
    private readonly Image[] albumFrames = new Image[6];
    private readonly TextMeshProUGUI[] albumCaptions = new TextMeshProUGUI[6];
    private int albumPage;
    private int albumOpenedFrame;

    public static bool AlbumOpen => instance != null && instance.albumPanel != null && instance.albumPanel.activeSelf;

    public static bool DialogueOpen => instance != null && instance.dialoguePanel != null && instance.dialoguePanel.activeSelf;
    public static int DialogueClosedFrame { get; private set; } = -10;
    public static bool AnnouncementOpen => instance != null && instance.announcePanel != null && instance.announcePanel.activeSelf;

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

        // 대화 상자 (아래쪽, 핫바 위)
        RectTransform box = CreatePanel("Dialogue", root, new Color(0.62f, 0.42f, 0.24f, 0.97f));
        SetAnchors(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(1100f, 200f));
        RectTransform border = CreatePanel("Border", box, new Color(0.78f, 0.56f, 0.32f, 1f));
        Stretch(border, 0f);
        border.offsetMin = new Vector2(6f, 6f); border.offsetMax = new Vector2(-6f, -6f);
        border.GetComponent<Image>().color = new Color(0.16f, 0.11f, 0.09f, 1f);
        GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform));
        portraitGO.transform.SetParent(box, false);
        dialoguePortrait = portraitGO.AddComponent<Image>();
        dialoguePortrait.preserveAspect = true;
        dialoguePortrait.raycastTarget = false;
        RectTransform pr = (RectTransform)portraitGO.transform;
        pr.anchorMin = pr.anchorMax = new Vector2(0f, 0.5f);
        pr.pivot = new Vector2(0f, 0.5f);
        pr.anchoredPosition = new Vector2(20f, 0f);
        pr.sizeDelta = new Vector2(170f, 170f);
        dialogueName = CreateText("Name", box, 28f);
        dialogueName.alignment = TextAlignmentOptions.TopLeft;
        dialogueName.color = new Color(1f, 0.84f, 0.45f);
        Place(dialogueName.rectTransform, 210f, -18f, 860f, 40f);
        dialogueText = CreateText("Text", box, 26f);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        Place(dialogueText.rectTransform, 210f, -60f, 860f, 100f);
        dialogueHint = CreateText("Hint", box, 20f);
        dialogueHint.alignment = TextAlignmentOptions.Right;
        dialogueHint.color = new Color(0.85f, 0.8f, 0.7f);
        Place(dialogueHint.rectTransform, 210f, -162f, 860f, 30f);
        dialoguePanel = box.gameObject;
        dialoguePanel.SetActive(false);

        BuildAlbum();
    }

    // ---------- season album ----------
    void BuildAlbum()
    {
        RectTransform panel = CreatePanel("Album", root, new Color(0.62f, 0.42f, 0.24f, 0.98f));
        SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1120f, 700f));
        RectTransform inner = CreatePanel("Inner", panel, new Color(0.95f, 0.9f, 0.8f, 1f));
        Stretch(inner, 0f);
        inner.offsetMin = new Vector2(10f, 10f); inner.offsetMax = new Vector2(-10f, -10f);

        albumTitle = CreateText("Title", panel, 36f);
        albumTitle.color = new Color(0.3f, 0.2f, 0.12f);
        Place(albumTitle.rectTransform, 0f, -20f, 1120f, 50f);
        albumFooter = CreateText("Footer", panel, 22f);
        albumFooter.color = new Color(0.4f, 0.3f, 0.22f);
        Place(albumFooter.rectTransform, 0f, -650f, 1120f, 34f);

        for (int i = 0; i < 6; i++)
        {
            float x = 70f + (i % 3) * 340f;
            float y = -90f - (i / 3) * 275f;
            RectTransform frame = CreatePanel("Frame" + i, panel, new Color(1f, 1f, 1f, 1f));
            frame.anchorMin = frame.anchorMax = new Vector2(0f, 1f);
            frame.pivot = new Vector2(0f, 1f);
            frame.anchoredPosition = new Vector2(x, y);
            frame.sizeDelta = new Vector2(300f, 200f);
            albumFrames[i] = frame.GetComponent<Image>();

            GameObject photoGO = new GameObject("Photo", typeof(RectTransform));
            photoGO.transform.SetParent(frame, false);
            Image photo = photoGO.AddComponent<Image>();
            photo.preserveAspect = true;
            photo.raycastTarget = false;
            RectTransform pr = (RectTransform)photoGO.transform;
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(14f, 14f); pr.offsetMax = new Vector2(-14f, -14f);
            albumPhotos[i] = photo;

            TextMeshProUGUI cap = CreateText("Caption" + i, panel, 22f);
            cap.color = new Color(0.3f, 0.2f, 0.12f);
            Place(cap.rectTransform, x - 10f, y - 204f, 320f, 64f);
            albumCaptions[i] = cap;
        }
        albumPanel = panel.gameObject;
        albumPanel.SetActive(false);
    }

    public static void ToggleAlbum()
    {
        HUD hud = Ensure();
        if (hud.albumPanel.activeSelf)
        {
            hud.albumPanel.SetActive(false);
            return;
        }
        DataManager data = DataManager.instance;
        hud.albumPage = data != null ? data.story.season : 0;
        hud.albumPanel.SetActive(true);
        hud.albumOpenedFrame = Time.frameCount;
        hud.DrawAlbum();
    }

    void DrawAlbum()
    {
        DataManager data = DataManager.instance;
        if (data == null) return;
        StoryState story = data.story;
        Season page = (Season)albumPage;
        bool reached = albumPage <= story.season || albumPage <= story.highestSeason;
        var photos = AlbumDB.ForSeason(page);

        albumTitle.text = $"<color={SeasonSystem.Color(page)}>{SeasonSystem.Name(page)}</color>   <size=26>{Album.Count(story, page)}/{AlbumDB.PerSeason}</size>";
        albumFooter.text = reached
            ? $"[<-] [->] Season    [P] Close        Total {story.photos.Count}/{AlbumDB.All.Count}"
            : "This season hasn't come yet. Keep living well.    [<-] [->]  [P]";

        for (int i = 0; i < 6; i++)
        {
            PhotoDef p = i < photos.Count ? photos[i] : null;
            bool taken = p != null && story.photos.Contains(p.id);
            albumPhotos[i].sprite = p != null ? PixelArt.Load(p.sprite) : null;
            albumPhotos[i].enabled = p != null && albumPhotos[i].sprite != null;
            albumPhotos[i].color = taken ? Color.white : new Color(0.16f, 0.15f, 0.17f, 1f);   // 못 찍은 사진은 실루엣
            albumFrames[i].color = taken ? Color.white : new Color(0.72f, 0.7f, 0.68f, 1f);
            if (p == null) albumCaptions[i].text = "";
            else if (taken) albumCaptions[i].text = $"<b>{p.title}</b>";
            else albumCaptions[i].text = reached ? $"???\n<size=18><color=#7a6a5a>{p.hint}</color></size>" : "???";
        }
    }

    void UpdateAlbum()
    {
        if (!AlbumOpen || Time.frameCount == albumOpenedFrame) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) { albumPanel.SetActive(false); return; }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { albumPage = (albumPage + 3) % 4; DrawAlbum(); }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { albumPage = (albumPage + 1) % 4; DrawAlbum(); }
    }

    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    // ---------- dialogue API ----------
    public static void Say(string speaker, string[] lines, Sprite portrait = null, System.Action onDone = null)
    {
        HUD hud = Ensure();
        hud.isChoice = false;
        hud.dialogueLines = lines;
        hud.dialogueIndex = 0;
        hud.dialogueDone = onDone;
        hud.OpenDialogue(speaker, portrait);
    }

    public static void Ask(string speaker, string question, Sprite portrait, System.Action onYes, System.Action onNo = null, string yes = "Yes", string no = "No")
    {
        HUD hud = Ensure();
        hud.isChoice = true;
        hud.dialogueLines = new[] { question };
        hud.dialogueIndex = 0;
        hud.choiceYes = onYes;
        hud.choiceNo = onNo;
        hud.OpenDialogue(speaker, portrait);
        hud.dialogueHint.text = $"[Y] {yes}     [N] {no}";
    }

    public static void HideAnnouncement()
    {
        if (instance != null && instance.announcePanel != null) instance.announcePanel.SetActive(false);
    }

    void OpenDialogue(string speaker, Sprite portrait)
    {
        dialogueName.text = speaker;
        dialoguePortrait.sprite = portrait;
        dialoguePortrait.enabled = portrait != null;
        float x = portrait != null ? 210f : 40f;
        Place(dialogueName.rectTransform, x, -18f, 1060f - x, 40f);
        Place(dialogueText.rectTransform, x, -60f, 1060f - x, 100f);
        dialoguePanel.SetActive(true);
        dialogueOpenedFrame = Time.frameCount;
        ShowLine();
    }

    void ShowLine()
    {
        dialogueText.text = dialogueLines[dialogueIndex];
        if (!isChoice) dialogueHint.text = dialogueIndex < dialogueLines.Length - 1 ? "[E] Next" : "[E] Close";
    }

    void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        DialogueClosedFrame = Time.frameCount;
    }

    void UpdateDialogue()
    {
        if (!DialogueOpen || Time.frameCount == dialogueOpenedFrame) return;

        if (isChoice)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                CloseDialogue();
                choiceYes?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDialogue();
                choiceNo?.Invoke();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            dialogueIndex++;
            if (dialogueIndex >= dialogueLines.Length)
            {
                CloseDialogue();
                System.Action done = dialogueDone;
                dialogueDone = null;
                done?.Invoke();
            }
            else
            {
                ShowLine();
            }
        }
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
        UpdateDialogue();
        UpdateAlbum();

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
