using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 픽셀 아트 화면 UI (브라우저 미리보기와 같은 모양). 장면 파일은 건드리지 않고 실행 중에 만듭니다.
//  - 왼쪽 위: 계절, 날짜, 시간, 날씨, 골드, 마음
//  - 오른쪽 위: 체력 / 배고픔 / 목마름 / 에너지 막대
//  - 아래: 핫바 8칸 (Hotbar가 이 칸들을 씁니다) + 들고 있는 아이템 이름
//  - I 키: 가방 (가진 아이템 전부, 칸에 마우스를 올리면 설명)
// 예전 글자 UI(Wood/Stone/Gold, 오른쪽 아이템 목록, 시간/날짜, 슬라이더)는 숨깁니다.
public class PixelHUD : MonoBehaviour
{
    public static PixelHUD Instance { get; private set; }
    public static bool BagOpen => Instance != null && Instance.bag != null && Instance.bag.activeSelf;
    public static bool ViewOpen => Instance != null && Instance.view != null && Instance.view.activeSelf;

    public const int HotbarSlotCount = 8;

    static readonly Color Fill = new Color(20 / 255f, 14 / 255f, 11 / 255f, 0.86f);
    static readonly Color Border = new Color(200 / 255f, 149 / 255f, 82 / 255f, 1f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.29f, 1f);
    static readonly Color Text = new Color(0.965f, 0.937f, 0.886f, 1f);

    // 예전 장면 UI 중 새 UI가 대신하는 것들
    static readonly string[] OldUI = { "WoodText", "StoneText", "GoldText", "DayText", "TimeText",
        "HealthSlider", "HungerSlider", "ThirstSlider", "EnergySlider", "SpecialItemPanel1" };

    private RectTransform root;
    private TextMeshProUGUI infoLine1, infoLine2, heldName;
    private readonly List<RectTransform> barFills = new List<RectTransform>();
    private RectTransform hotbarRoot;
    private readonly List<HotbarSlot> hotbarSlots = new List<HotbarSlot>();
    private GameObject bag;
    private RectTransform bagGrid;
    private TextMeshProUGUI bagInfo;
    private readonly List<BagSlot> bagSlots = new List<BagSlot>();
    private GameObject view;
    private RectTransform viewLayers;
    private TextMeshProUGUI viewCaption;
    private int viewOpenedFrame;

    private float nextFontPass;

    public IReadOnlyList<HotbarSlot> HotbarSlots => hotbarSlots;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Ensure();
        SceneManager.sceneLoaded += (scene, mode) => HideOldUI(scene);
        HideOldUI(SceneManager.GetActiveScene());
    }

    public static PixelHUD Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("PixelHUD", typeof(RectTransform));
        DontDestroyOnLoad(go);
        return go.AddComponent<PixelHUD>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        HUD.SetStatusVisible(false);   // 위쪽 상태줄은 새 정보 상자가 대신함
    }

    static void HideOldUI(Scene scene)
    {
        if (!scene.IsValid()) return;
        foreach (GameObject rootGO in scene.GetRootGameObjects())
            foreach (Transform t in rootGO.GetComponentsInChildren<Transform>(true))
                if (OldUI.Contains(t.name)) t.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- 만들기
    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;   // 장면의 메뉴(상점, 제작, 상자)가 이 위에 그려지도록
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIScale.Reference;
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)transform;

        // 왼쪽 위 정보 상자
        RectTransform info = Box("Info", root, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(440f, 104f));
        infoLine1 = Label("Line1", info, 34f, TextAlignmentOptions.Left);
        Place(infoLine1.rectTransform, 22f, -12f, 400f, 44f);
        infoLine2 = Label("Line2", info, 28f, TextAlignmentOptions.Left);
        Place(infoLine2.rectTransform, 22f, -56f, 400f, 38f);

        // 오른쪽 위 막대 상자
        RectTransform bars = Box("Bars", root, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(400f, 160f));
        string[] names = { "HP", "Food", "Water", "Energy" };
        Color[] colors = { new Color(0.88f, 0.28f, 0.28f), new Color(0.88f, 0.63f, 0.25f), new Color(0.35f, 0.64f, 0.89f), new Color(0.47f, 0.82f, 0.38f) };
        for (int i = 0; i < 4; i++)
        {
            TextMeshProUGUI label = Label(names[i], bars, 24f, TextAlignmentOptions.Left);
            label.text = names[i];
            Place(label.rectTransform, 20f, -14f - i * 34f, 110f, 30f);
            RectTransform back = Rect("Back", bars, new Color(0.1f, 0.08f, 0.07f, 1f));
            Place(back, 130f, -18f - i * 34f, 250f, 22f);
            RectTransform fill = Rect("Fill", back, colors[i]);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(1f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(3f, 3f); fill.offsetMax = new Vector2(-3f, -3f);
            barFills.Add(fill);
        }

        // 아래 핫바
        float slot = 88f, gap = 8f;
        float width = HotbarSlotCount * slot + (HotbarSlotCount - 1) * gap;
        hotbarRoot = new GameObject("Hotbar", typeof(RectTransform)).GetComponent<RectTransform>();
        hotbarRoot.SetParent(root, false);
        hotbarRoot.anchorMin = hotbarRoot.anchorMax = new Vector2(0.5f, 0f);
        hotbarRoot.pivot = new Vector2(0.5f, 0f);
        hotbarRoot.anchoredPosition = new Vector2(0f, 20f);
        hotbarRoot.sizeDelta = new Vector2(width, slot + 46f);
        heldName = Label("Held", hotbarRoot, 28f, TextAlignmentOptions.Center);
        heldName.rectTransform.anchorMin = heldName.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        heldName.rectTransform.pivot = new Vector2(0.5f, 1f);
        heldName.rectTransform.anchoredPosition = Vector2.zero;
        heldName.rectTransform.sizeDelta = new Vector2(width, 40f);
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            RectTransform s = Slot("Slot" + (i + 1), hotbarRoot, out Image icon, out TextMeshProUGUI count, out GameObject selector);
            s.anchorMin = s.anchorMax = new Vector2(0f, 0f);
            s.pivot = new Vector2(0f, 0f);
            s.anchoredPosition = new Vector2(i * (slot + gap), 0f);
            s.sizeDelta = new Vector2(slot, slot);
            TextMeshProUGUI key = Label("Key", s, 18f, TextAlignmentOptions.TopLeft);
            key.text = (i + 1).ToString();
            key.color = new Color(1f, 1f, 1f, 0.45f);
            Place(key.rectTransform, 8f, -4f, 30f, 24f);
            HotbarSlot hs = s.gameObject.AddComponent<HotbarSlot>();
            hs.icon = icon; hs.itemCountText = count; hs.selector = selector;
            hs.ClearSlot(); hs.SetSelector(false);
            hotbarSlots.Add(hs);
        }

        BuildBag();
        BuildView();
    }

    // 노을 언덕 전망대에서 보는 풍경 (그림 층을 겹쳐 보여 줌)
    void BuildView()
    {
        var go = new GameObject("View", typeof(RectTransform));
        go.transform.SetParent(root, false);
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 470;
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        RectTransform shade = Rect("Shade", rt, new Color(0.04f, 0.03f, 0.05f, 0.92f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
        shade.offsetMin = Vector2.zero; shade.offsetMax = Vector2.zero;
        RectTransform frame = Box("Frame", rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1296f, 736f));
        frame.pivot = new Vector2(0.5f, 0.5f);
        viewLayers = new GameObject("Layers", typeof(RectTransform)).GetComponent<RectTransform>();
        viewLayers.SetParent(frame, false);
        viewLayers.anchorMin = viewLayers.anchorMax = new Vector2(0.5f, 0.5f);
        viewLayers.sizeDelta = new Vector2(1280f, 720f);
        viewCaption = Label("Caption", rt, 26f, TextAlignmentOptions.Center);
        viewCaption.rectTransform.anchorMin = viewCaption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        viewCaption.rectTransform.anchoredPosition = new Vector2(0f, -360f);
        viewCaption.rectTransform.sizeDelta = new Vector2(1400f, 90f);
        view = go;
        view.SetActive(false);
    }

    public static void ShowView(List<string> layers, string caption)
    {
        PixelHUD hud = Ensure();
        foreach (Transform child in hud.viewLayers) Destroy(child.gameObject);
        foreach (string key in layers)
        {
            Sprite sprite = PixelArt.Load("World/" + key);
            if (sprite == null) continue;
            RectTransform img = Rect(key, hud.viewLayers, Color.white);
            img.anchorMin = Vector2.zero; img.anchorMax = Vector2.one;
            img.offsetMin = Vector2.zero; img.offsetMax = Vector2.zero;
            img.GetComponent<Image>().sprite = sprite;
        }
        hud.viewCaption.text = caption + "\n<size=20><color=#c8c0b0>[E] or [Esc] Close</color></size>";
        hud.view.SetActive(true);
        hud.viewOpenedFrame = Time.frameCount;
    }

    void BuildBag()
    {
        // 가방은 다른 UI 위에 그려지고, 마우스를 받아야 해서 따로 캔버스를 둡니다.
        var go = new GameObject("Bag", typeof(RectTransform));
        go.transform.SetParent(root, false);
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 480;
        go.AddComponent<GraphicRaycaster>();
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(920f, 560f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        RectTransform frame = Box("Frame", rt, new Vector2(0.5f, 0.5f), Vector2.zero, rt.sizeDelta);
        frame.pivot = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI title = Label("Title", frame, 36f, TextAlignmentOptions.Center);
        title.text = "<color=#ffd84a>Bag</color>";
        Place(title.rectTransform, 0f, -14f, 920f, 46f);
        bagGrid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
        bagGrid.SetParent(frame, false);
        Place(bagGrid, 30f, -70f, 860f, 360f);
        bagInfo = Label("Info", frame, 24f, TextAlignmentOptions.TopLeft);
        Place(bagInfo.rectTransform, 34f, -440f, 852f, 80f);
        TextMeshProUGUI hint = Label("Hint", frame, 20f, TextAlignmentOptions.Right);
        hint.text = "[I] or [Esc] Close";
        hint.color = new Color(0.78f, 0.75f, 0.69f);
        Place(hint.rectTransform, 30f, -520f, 860f, 30f);
        bag = go;
        bag.SetActive(false);
    }

    // ---------------------------------------------------------------- 매 프레임
    void Update()
    {
        DataManager data = DataManager.instance;
        GameTimeManager time = GameTimeManager.instance;
        if (data != null)
        {
            Season s = data.CurrentSeason;
            string clock = time != null ? time.FormattedTime : "";
            int day = time != null ? time.day : 1;
            infoLine1.text = $"<color={SeasonSystem.Color(s)}>{SeasonSystem.Name(s)}</color> Day {day}  {clock}";
            string weather = data.snowing ? "<color=#dfe8ff>Snow</color>" : data.raining ? "<color=#8fd0ff>Rain</color>" : "<color=#ffd84a>Sunny</color>";
            infoLine2.text = $"{weather}   <color=#ffd84a>{data.gold}G</color>   <color=#ff8fb1>Heart</color> {data.heart:0}";
            SetBar(0, data.health / Mathf.Max(1f, data.maxHealth));
            SetBar(1, data.hunger / Mathf.Max(1f, data.maxHunger));
            SetBar(2, data.thirst / Mathf.Max(1f, data.maxThirst));
            SetBar(3, data.energy / Mathf.Max(1f, data.MaxEnergyNow));
        }

        bool hasHotbar = Hotbar.Instance != null;
        if (hotbarRoot.gameObject.activeSelf != hasHotbar) hotbarRoot.gameObject.SetActive(hasHotbar);
        if (hasHotbar)
        {
            string held = Hotbar.SelectedItem;
            heldName.text = string.IsNullOrEmpty(held) ? "" : held;
        }

        // 상점·상자·제작 창처럼 나중에 생기는 글자도 픽셀 글꼴로 (1초에 한 번 가볍게 확인)
        if (Time.unscaledTime >= nextFontPass && PixelFont.Asset != null)
        {
            nextFontPass = Time.unscaledTime + 1f;
            foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if (t.font != PixelFont.Asset) t.font = PixelFont.Asset;
        }

        if (ViewOpen && Time.frameCount > viewOpenedFrame + 1 &&
            (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            view.SetActive(false);
            return;
        }
        if (Input.GetKeyDown(KeyCode.I) && !HUD.DialogueOpen && !HUD.AlbumOpen && !ViewOpen) ToggleBag();
        else if (BagOpen && Input.GetKeyDown(KeyCode.Escape)) bag.SetActive(false);
    }

    void SetBar(int i, float t)
    {
        RectTransform fill = barFills[i];
        fill.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
        fill.offsetMax = new Vector2(t >= 0.999f ? -3f : 0f, -3f);
    }

    // ---------------------------------------------------------------- 가방
    public void ToggleBag()
    {
        if (BagOpen) { bag.SetActive(false); return; }
        bag.SetActive(true);
        RefreshBag();
    }

    void RefreshBag()
    {
        DataManager data = DataManager.instance;
        foreach (BagSlot b in bagSlots) Destroy(b.gameObject);
        bagSlots.Clear();
        bagInfo.text = "<color=#c8c0b0>Point at an item to see what it is.</color>";
        if (data == null) return;
        var items = data.inventory.Where(p => p.Value > 0).OrderBy(p => ItemDB.SortKey(p.Key)).ToList();
        const int cols = 10;
        const float size = 78f, gap = 8f;
        for (int i = 0; i < items.Count && i < 40; i++)
        {
            RectTransform s = Slot("Item", bagGrid, out Image icon, out TextMeshProUGUI count, out GameObject selector);
            s.anchorMin = s.anchorMax = new Vector2(0f, 1f);
            s.pivot = new Vector2(0f, 1f);
            s.anchoredPosition = new Vector2((i % cols) * (size + gap), -(i / cols) * (size + gap));
            s.sizeDelta = new Vector2(size, size);
            icon.gameObject.SetActive(true);
            icon.sprite = ItemAssets.SpriteFor(items[i].Key);
            icon.preserveAspect = true;
            count.text = items[i].Value > 1 ? items[i].Value.ToString() : "";
            s.GetComponent<Image>().raycastTarget = true;
            BagSlot b = s.gameObject.AddComponent<BagSlot>();
            b.itemName = items[i].Key;
            b.selector = selector;
            b.hud = this;
            bagSlots.Add(b);
        }
        if (items.Count == 0) bagInfo.text = "Your bag is empty.";
    }

    public void ShowItem(string itemName)
    {
        ItemDef def = ItemDB.Get(itemName);
        if (def == null) return;
        string price = ItemDB.CanSell(itemName) ? $"   <color=#ffd84a>{def.sellPrice}G</color>" : "";
        string desc = string.IsNullOrEmpty(def.description) ? "" : "\n<color=#d8d0c0>" + def.description + "</color>";
        string food = def.IsEdible ? $"\n<color=#9dff9d>Food +{def.hunger:0}  Water +{def.thirst:0}" + (def.heart > 0 ? $"  Heart +{def.heart:0}" : "") + "</color>" : "";
        bagInfo.text = $"<color=#ffd84a>{def.name}</color>{price}{desc}{food}";
    }

    // ---------------------------------------------------------------- 그리기 도구
    static RectTransform Rect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    // 나무색 테두리 + 어두운 속 (미리보기의 상자와 같은 색)
    static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform border = Rect(name, parent, Border);
        border.anchorMin = border.anchorMax = anchor;
        border.pivot = anchor;
        border.anchoredPosition = pos;
        border.sizeDelta = size;
        RectTransform inner = Rect("Fill", border, new Color(Fill.r, Fill.g, Fill.b, 1f));
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(4f, 4f); inner.offsetMax = new Vector2(-4f, -4f);
        inner.GetComponent<Image>().color = Fill;
        border.GetComponent<Image>().color = Border;
        return border;
    }

    static RectTransform Slot(string name, Transform parent, out Image icon, out TextMeshProUGUI count, out GameObject selector)
    {
        RectTransform s = Rect(name, parent, Border);
        RectTransform inner = Rect("Fill", s, new Color(0.16f, 0.11f, 0.09f, 0.92f));
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(4f, 4f); inner.offsetMax = new Vector2(-4f, -4f);
        RectTransform iconRT = Rect("Icon", s, Color.white);
        iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(12f, 12f); iconRT.offsetMax = new Vector2(-12f, -12f);
        icon = iconRT.GetComponent<Image>();
        icon.gameObject.SetActive(false);
        count = Label("Count", s, 22f, TextAlignmentOptions.BottomRight);
        count.rectTransform.anchorMin = Vector2.zero; count.rectTransform.anchorMax = Vector2.one;
        count.rectTransform.offsetMin = new Vector2(6f, 4f); count.rectTransform.offsetMax = new Vector2(-8f, -4f);
        // 선택 표시: 노란 테두리 (위아래좌우 네 줄)
        var sel = new GameObject("Selector", typeof(RectTransform));
        sel.transform.SetParent(s, false);
        RectTransform selRT = (RectTransform)sel.transform;
        selRT.anchorMin = Vector2.zero; selRT.anchorMax = Vector2.one;
        selRT.offsetMin = new Vector2(-3f, -3f); selRT.offsetMax = new Vector2(3f, 3f);
        Edge(selRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);
        Edge(selRT, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 6f));
        Edge(selRT, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
        Edge(selRT, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-6f, 0f), Vector2.zero);
        sel.SetActive(false);
        selector = sel;
        return s;
    }

    static void Edge(RectTransform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        RectTransform e = Rect("Edge", parent, Gold);
        e.anchorMin = min; e.anchorMax = max;
        e.offsetMin = offMin; e.offsetMax = offMax;
    }

    static TextMeshProUGUI Label(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        PixelFont.Apply(t);
        t.fontSize = size;
        t.alignment = align;
        t.color = Text;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    // 왼쪽 위 기준으로 배치
    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}

// 가방의 한 칸: 마우스를 올리면 아래에 설명을 보여 줍니다.
public class BagSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string itemName;
    public GameObject selector;
    public PixelHUD hud;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selector != null) selector.SetActive(true);
        if (hud != null) hud.ShowItem(itemName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selector != null) selector.SetActive(false);
    }
}
