using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 데모 끝 (K-5). 정월대보름 다음 날 아침, 봄이 오는 장면 → 올겨울의 나 → 정식판에서 → 찜하기/팔로우, 타이틀로.
// [E]/Space/Enter/클릭: 다음. 마지막 장: [1] 가게 페이지 열기, [2] 타이틀로.
public class DemoEnd : MonoBehaviour
{
    static DemoEnd instance;
    public static bool Open => instance != null && instance.root != null && instance.root.gameObject.activeSelf;

    RectTransform root;
    Image picture;
    TextMeshProUGUI title, body, hint;
    RectTransform buttons;
    int page, openedFrame;

    public static void Show()
    {
        if (instance == null)
        {
            var go = new GameObject("DemoEnd");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DemoEnd>();
            instance.Build();
        }
        instance.page = 0;
        instance.openedFrame = Time.frameCount;
        instance.root.gameObject.SetActive(true);
        instance.Render();
    }

    void Build()
    {
        ScreenUI.EnsureEventSystem();
        RectTransform canvas = ScreenUI.Canvas("DemoEndCanvas", 512);
        canvas.SetParent(transform, false);
        root = ScreenUI.Rect("Back", canvas, new Color(0.05f, 0.04f, 0.04f, 1f), true);
        ScreenUI.Stretch(root);
        picture = ScreenUI.Picture("Picture", root, "bg_farmmap_spring");
        ScreenUI.At((RectTransform)picture.transform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1152f, 640f));
        RectTransform plate = ScreenUI.Box("Plate", root, ScreenUI.Border, ScreenUI.Fill);
        ScreenUI.At(plate, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1500f, 420f));
        title = ScreenUI.Label("Title", plate, 46f, TextAlignmentOptions.Center, ScreenUI.Gold);
        ScreenUI.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(1400f, 64f));
        body = ScreenUI.Label("Body", plate, 28f, TextAlignmentOptions.Top, ScreenUI.Text);
        ScreenUI.At(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(1400f, 300f));
        hint = ScreenUI.Label("Hint", root, 22f, TextAlignmentOptions.Center, ScreenUI.Dim);
        ScreenUI.At(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 30f));
        buttons = (RectTransform)new GameObject("Buttons", typeof(RectTransform)).transform;
        buttons.SetParent(plate, false);
        ScreenUI.At(buttons, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 70f));
        AddButton(0, () => Application.OpenURL(DemoConfig.StoreUrl));
        AddButton(1, BackToTitle);
        root.gameObject.SetActive(false);
    }

    void AddButton(int i, System.Action act)
    {
        RectTransform b = ScreenUI.Box("Button" + i, buttons, ScreenUI.Border, new Color(0.13f, 0.09f, 0.07f, 1f), 3f);
        ScreenUI.At(b, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -290f : 290f, 0f), new Vector2(540f, 64f));
        b.GetComponent<Image>().raycastTarget = true;
        b.gameObject.AddComponent<Button>().onClick.AddListener(() => act());
        TextMeshProUGUI t = ScreenUI.Label("Label", b, 28f, TextAlignmentOptions.Center, ScreenUI.Text);
        ScreenUI.Stretch(t.rectTransform, 8f);
    }

    void Render()
    {
        DataManager data = DataManager.instance;
        buttons.gameObject.SetActive(page == 3);
        picture.gameObject.SetActive(page == 0 || page == 2);
        hint.text = page < 3 ? Loc.T("[E] / Space / click: next", "[E] / Space / 클릭: 다음") : Loc.T("[1] or [2], or click", "[1] 또는 [2], 또는 클릭");
        switch (page)
        {
            case 0:
                picture.sprite = PixelArt.Load("World/bg_farmmap_spring");
                title.text = Loc.T("Spring has come", "봄이 왔어요");
                body.text = Loc.T("The snow on the ridges melts. The stream runs again.\nThe village you came to with a gray heart is green this morning.",
                                  "이랑 위 눈이 녹고, 개울이 다시 흘러요.\n잿빛 마음으로 찾아온 마을이, 오늘 아침엔 초록이에요.");
                break;
            case 1:
                title.text = Loc.T("Me, this winter", "올겨울의 나");
                body.text = data != null ? string.Join("\n", DemoRecap.Lines(data.story, data.Today)) : "";
                break;
            case 2:
                picture.sprite = PixelArt.Load("World/bg_lake_summer") ?? PixelArt.Load("World/bg_forest_spring");
                title.text = Loc.T("The rest of the year, in the full game", "남은 한 해는 정식판에서");
                body.text = "· " + string.Join("\n· ", DemoRecap.FullGame());
                break;
            default:
                title.text = Loc.T("Thank you for playing the demo", "체험판을 플레이해 주셔서 고마워요");
                body.text = Loc.T($"If you'd like to see the rest of the year, follow the game on {DemoConfig.StoreName}.\nIt helps a small developer more than you know.",
                                  $"남은 한 해가 궁금하다면 {DemoConfig.StoreName}에서 팔로우해 주세요.\n혼자 만드는 게임에 정말 큰 힘이 돼요.");
                buttons.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = Loc.T($"[1] Follow on {DemoConfig.StoreName}", $"[1] {DemoConfig.StoreName}에서 팔로우");
                buttons.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = Loc.T("[2] Back to title", "[2] 타이틀로");
                break;
        }
    }

    void BackToTitle()
    {
        root.gameObject.SetActive(false);
        TitleScreen.Reopen();
    }

    void Update()
    {
        if (!Open || Time.frameCount <= openedFrame + 1) return;
        if (page < 3)
        {
            if (ScreenUI.Confirm() || Input.GetMouseButtonDown(0)) { page++; Sfx.Play("click"); Render(); }
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Application.OpenURL(DemoConfig.StoreUrl);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) BackToTitle();
    }
}
