using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ESC 일시정지 메뉴 (픽셀 HUD와 같은 갈색/금색 상자). 장면 파일은 건드리지 않고 실행 중에 만듭니다.
//  - 계속하기 / 저장하기 / 볼륨 3개(전체, 음악, 효과음) / 전체 화면 / 저장하고 끝내기
//  - 다른 창(가방, 대화, 앨범, 상점, 상자, 제작, 풍경)이 열려 있으면 ESC는 그 창을 닫는 데만 씁니다.
//  - 인트로나 게임 오버처럼 이미 시간이 멈춘 상태에서는 열리지 않습니다.
[DefaultExecutionOrder(-1000)]
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool Open => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

    static readonly Color Fill = new Color(20 / 255f, 14 / 255f, 11 / 255f, 1f);
    static readonly Color Border = new Color(200 / 255f, 149 / 255f, 82 / 255f, 1f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.29f, 1f);
    static readonly Color Text = new Color(0.965f, 0.937f, 0.886f, 1f);
    static readonly Color ButtonFill = new Color(0.16f, 0.11f, 0.09f, 1f);

    private GameObject panel;
    private TextMeshProUGUI fullscreenLabel, status;
    private float timeScaleBefore = 1f;
    private bool otherUIOpenAtFrameStart;
    private float statusUntil;
    private readonly List<GameObject> scenePanels = new List<GameObject>();
    // 언어가 바뀌면 다시 쓰는 글자들
    private readonly List<KeyValuePair<TextMeshProUGUI, System.Func<string>>> localized = new List<KeyValuePair<TextMeshProUGUI, System.Func<string>>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Instance != null) return;
        var go = new GameObject("PauseMenu", typeof(RectTransform));
        DontDestroyOnLoad(go);
        go.AddComponent<PauseMenu>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        panel.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Loc.Changed += Relabel;
        FindScenePanels();
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; Loc.Changed -= Relabel; }

    void Bind(TextMeshProUGUI t, System.Func<string> text)
    {
        t.text = text();
        localized.Add(new KeyValuePair<TextMeshProUGUI, System.Func<string>>(t, text));
    }

    void Relabel()
    {
        foreach (var pair in localized) if (pair.Key != null) pair.Key.text = pair.Value();
        RefreshFullscreenLabel();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (Open) Close();
        FindScenePanels();
    }

    // 장면에 있는 메뉴 창들 (ESC로 각자 닫힘)
    void FindScenePanels()
    {
        scenePanels.Clear();
        foreach (Shop x in FindObjectsByType<Shop>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (x.shopPanel) scenePanels.Add(x.shopPanel);
        foreach (Storage x in FindObjectsByType<Storage>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (x.storagePanel) scenePanels.Add(x.storagePanel);
        foreach (CraftingManager x in FindObjectsByType<CraftingManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (x.craftingPanel) scenePanels.Add(x.craftingPanel);
    }

    bool OtherUIOpen()
    {
        if (HUD.DialogueOpen || HUD.AlbumOpen || HUD.AnnouncementOpen || PixelHUD.BagOpen || PixelHUD.ViewOpen) return true;
        if (Time.frameCount - HUD.DialogueClosedFrame <= 1) return true;
        foreach (GameObject p in scenePanels) if (p != null && p.activeInHierarchy) return true;
        return false;
    }

    // 다른 스크립트가 ESC로 자기 창을 닫기 전에, 프레임 시작 때 열린 창이 있었는지 기억합니다.
    void Update()
    {
        otherUIOpenAtFrameStart = !Open && OtherUIOpen();
    }

    void LateUpdate()
    {
        if (status != null && status.text.Length > 0 && Time.unscaledTime > statusUntil) status.text = "";
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (Open) { Close(); return; }
        if (otherUIOpenAtFrameStart || Time.timeScale == 0f) return;
        DataManager data = DataManager.instance;
        if (data != null && data.IsGameOver) return;
        Show();
    }

    public void Show()
    {
        EnsureEventSystem();
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        RefreshFullscreenLabel();
        status.text = "";
        panel.SetActive(true);
        Sfx.Play("open");
    }

    public void Close()
    {
        panel.SetActive(false);
        Time.timeScale = timeScaleBefore <= 0f ? 1f : timeScaleBefore;
        Sfx.Play("close");
    }

    void SaveNow()
    {
        DataManager data = DataManager.instance;
        if (data == null || data.IsGameOver) { SetStatus(Loc.T("Can't save right now.", "지금은 저장할 수 없어요.")); return; }
        data.SaveGame();
        SetStatus(Loc.T("Saved.", "저장했어요."));
    }

    void SaveAndQuit()
    {
        DataManager data = DataManager.instance;
        if (data != null && !data.IsGameOver) data.SaveGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ToggleFullscreen()
    {
        bool goFull = !Screen.fullScreen;
        if (goFull)
        {
            Resolution r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        }
        fullscreenLabel.text = FullscreenText(goFull);
    }

    static string FullscreenText(bool on) => on ? Loc.T("Fullscreen: On", "전체 화면: 켬") : Loc.T("Fullscreen: Off", "전체 화면: 끔");
    void RefreshFullscreenLabel() { if (fullscreenLabel != null) fullscreenLabel.text = FullscreenText(Screen.fullScreen); }

    void SetStatus(string s) { status.text = s; statusUntil = Time.unscaledTime + 2f; }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    // ---------------------------------------------------------------- 만들기
    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // 모든 UI 위에
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIScale.Reference;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        panel = Rect("Panel", transform, new Color(0.04f, 0.03f, 0.05f, 0.7f)).gameObject;
        RectTransform shade = (RectTransform)panel.transform;
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;

        const float W = 620f, H = 770f;
        RectTransform frame = Rect("Frame", panel.transform, Border);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(W, H);
        RectTransform inner = Rect("Fill", frame, Fill);
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(6f, 6f); inner.offsetMax = new Vector2(-6f, -6f);

        TextMeshProUGUI title = Label("Title", frame, 48f, TextAlignmentOptions.Center, Gold);
        Bind(title, () => Loc.T("Paused", "일시정지"));
        Place(title.rectTransform, 0f, -26f, W, 60f);

        float y = -104f;
        AddButton(frame, () => Loc.T("Resume", "계속하기"), ref y, Close);
        AddButton(frame, () => Loc.T("Save", "저장하기"), ref y, SaveNow);
        y -= 8f;
        AddSlider(frame, () => Loc.T("Master", "전체"), Sfx.Master, Sfx.SetMaster, ref y);
        AddSlider(frame, () => Loc.T("Music", "음악"), Sfx.Music, Sfx.SetMusic, ref y);
        AddSlider(frame, () => Loc.T("Sound", "효과음"), Sfx.Effects, v => { Sfx.SetEffects(v); Sfx.Play("pickup", 1f, 0.15f); }, ref y);
        y -= 8f;
        fullscreenLabel = AddButton(frame, () => FullscreenText(Screen.fullScreen), ref y, ToggleFullscreen);
        AddButton(frame, () => Loc.T("Language: English", "언어: 한국어"), ref y, Loc.Toggle);
        AddButton(frame, () => Loc.T("Save & Quit", "저장하고 끝내기"), ref y, SaveAndQuit);

        status = Label("Status", frame, 24f, TextAlignmentOptions.Center, Gold);
        Place(status.rectTransform, 0f, y - 4f, W, 32f);
        TextMeshProUGUI hint = Label("Hint", frame, 20f, TextAlignmentOptions.Center, new Color(Text.r, Text.g, Text.b, 0.6f));
        Bind(hint, () => Loc.T("ESC: back to game", "ESC: 게임으로 돌아가기"));
        Place(hint.rectTransform, 0f, -H + 44f, W, 28f);
    }

    TextMeshProUGUI AddButton(RectTransform parent, System.Func<string> text, ref float y, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform border = Rect("Button", parent, Border);
        Place(border, 110f, y, 400f, 58f);
        Image fill = Rect("Fill", border, ButtonFill).GetComponent<Image>();
        RectTransform frt = (RectTransform)fill.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3f, 3f); frt.offsetMax = new Vector2(-3f, -3f);
        Button b = border.gameObject.AddComponent<Button>();
        b.targetGraphic = fill;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.8f, 1.8f, 1.5f, 1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.selectedColor = Color.white; cb.colorMultiplier = 1.5f;
        b.colors = cb;
        b.onClick.AddListener(onClick);
        TextMeshProUGUI label = Label("Text", border, 30f, TextAlignmentOptions.Center, Text);
        Bind(label, text);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        y -= 70f;
        return label;
    }

    void AddSlider(RectTransform parent, System.Func<string> text, float value, System.Action<float> onChange, ref float y)
    {
        string name = "Volume" + localized.Count;
        TextMeshProUGUI label = Label(name, parent, 26f, TextAlignmentOptions.Left, Text);
        Bind(label, text);
        Place(label.rectTransform, 70f, y - 4f, 160f, 36f);

        RectTransform root = new GameObject(name + "Slider", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, 230f, y - 8f, 320f, 28f);
        RectTransform back = Rect("Back", root, new Color(0.1f, 0.08f, 0.07f, 1f));
        back.anchorMin = Vector2.zero; back.anchorMax = Vector2.one; back.offsetMin = back.offsetMax = Vector2.zero;
        RectTransform area = Rect("FillArea", root, Color.clear);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(4f, 4f); area.offsetMax = new Vector2(-4f, -4f);
        RectTransform fill = Rect("Fill", area, Gold);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        RectTransform handleArea = Rect("HandleArea", root, Color.clear);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f); handleArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform handle = Rect("Handle", handleArea, Text);
        handle.sizeDelta = new Vector2(16f, 0f);
        handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
        handle.offsetMin = new Vector2(-8f, -6f); handle.offsetMax = new Vector2(8f, 6f);

        Slider s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill; s.handleRect = handle; s.targetGraphic = handle.GetComponent<Image>();
        s.minValue = 0f; s.maxValue = 1f; s.value = value;
        s.onValueChanged.AddListener(v => onChange(v));
        y -= 50f;
    }

    static RectTransform Rect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI Label(string name, Transform parent, float size, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.alignment = align; t.color = color;
        t.raycastTarget = false;
        PixelFont.Apply(t);
        return t;
    }

    // 왼쪽 위 기준 배치 (x, y는 부모 왼쪽 위에서의 거리, y는 아래로 음수)
    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
