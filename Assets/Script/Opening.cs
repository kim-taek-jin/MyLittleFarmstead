using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 새로 시작할 때 나오는 짧은 오프닝 (D-1): 그림 네 장, 한 줄씩. Space/Enter/클릭으로 넘기고 Esc로 건너뜁니다.
//   회색 도시 → 시골 가는 버스 정류장 → 아무도 살지 않던 회색 집 → 나를 중심으로 색이 조금 돌아옴
// 그림: World/bg_city, World/op_bus, World/op_arrive, World/op_arrive_color (Tools/pixelart/opening.py)
// 끝나면 첫날 안내(FirstDayGuide)가 화면 왼쪽에 나옵니다.
public class Opening : MonoBehaviour
{
    public static bool Open => instance != null && instance.root != null && instance.root.gameObject.activeSelf;
    public static bool PlayOnNextLoad;          // 저장을 지우고 농장을 다시 불러온 다음에 틀기

    static Opening instance;

    static readonly (string pic, string en, string ko)[] Panels =
    {
        ("bg_city", "Overtime, then more overtime. One day the whole world started to look gray.",
                    "야근, 또 야근. 어느 날부터 세상이 온통 잿빛으로 보였어요."),
        ("op_bus", "I quit, packed one suitcase, and took the last bus out to the countryside.",
                   "회사를 그만두고, 가방 하나 챙겨 시골 가는 막차를 탔어요."),
        ("op_arrive", "An old house no one had lived in for years. Gray, just like me.",
                      "몇 해째 아무도 살지 않은 낡은 집. 나처럼 잿빛이었어요."),
        ("op_arrive_color", "I decided to take care of it, and of myself, a little at a time.",
                            "이 집도, 나도, 조금씩 돌봐 보기로 했어요."),
    };
    const float CharsPerSecond = 28f;
    const float FadeSeconds = 0.4f;

    RectTransform root;
    Image picture, fade;
    TextMeshProUGUI caption, hint;
    int panel = -1;
    float shown;        // 지금 그림에서 글자가 나온 시간
    float fadeT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (PlayOnNextLoad && s.name == FarmMap.SceneName) { PlayOnNextLoad = false; Play(); }
        };
    }

    public static void Play()
    {
        if (instance == null)
        {
            var go = new GameObject("Opening");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Opening>();
        }
        instance.Begin();
    }

    void Build()
    {
        root = ScreenUI.Canvas("OpeningCanvas", 510);
        root.SetParent(transform, false);
        RectTransform back = ScreenUI.Rect("Back", root, Color.black, true);
        ScreenUI.Stretch(back);
        RectTransform frame = ScreenUI.Rect("Frame", root, ScreenUI.Border);
        ScreenUI.At(frame, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1448f, 824f));
        picture = ScreenUI.Picture("Picture", frame, "bg_city");
        ScreenUI.Stretch((RectTransform)picture.transform, 4f);
        RectTransform box = ScreenUI.Box("Caption", root, ScreenUI.Border, ScreenUI.Fill);
        ScreenUI.At(box, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1448f, 116f));
        caption = ScreenUI.Label("Text", box, 36f, TextAlignmentOptions.Center, ScreenUI.Text);
        ScreenUI.Stretch(caption.rectTransform, 18f);
        hint = ScreenUI.Label("Hint", root, 22f, TextAlignmentOptions.Right, ScreenUI.Dim);
        ScreenUI.At(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-260f, 28f), new Vector2(480f, 34f));
        fade = ScreenUI.Rect("Fade", root, Color.black).GetComponent<Image>();
        ScreenUI.Stretch((RectTransform)fade.transform);
    }

    void Begin()
    {
        if (root == null) Build();
        ScreenUI.EnsureEventSystem();
        root.gameObject.SetActive(true);
        Time.timeScale = 0f;
        panel = -1;
        Next();
    }

    void Next()
    {
        panel++;
        if (panel >= Panels.Length) { Finish(); return; }
        Sprite s = PixelArt.Load("World/" + Panels[panel].pic);
        if (s != null) picture.sprite = s;
        shown = 0f;
        fadeT = FadeSeconds;
        hint.text = Loc.T("[Space] next   [Esc] skip", "[Space] 다음   [Esc] 건너뛰기");
        UpdateCaption();
    }

    string Line => Loc.Ko ? Panels[panel].ko : Panels[panel].en;
    bool FullyShown => shown * CharsPerSecond >= Line.Length;

    void UpdateCaption()
    {
        string line = Line;
        int n = Mathf.Clamp(Mathf.FloorToInt(shown * CharsPerSecond), 0, line.Length);
        caption.text = line.Substring(0, n);
    }

    void Update()
    {
        if (!Open) return;
        Time.timeScale = 0f;
        float dt = Time.unscaledDeltaTime;
        if (fadeT > 0f)
        {
            fadeT -= dt;
            // 검은 막이 네 단계로 걷힘 (부드러운 그라데이션 대신)
            float a = Mathf.Round(Mathf.Clamp01(fadeT / FadeSeconds) * 4f) / 4f;
            fade.color = new Color(0f, 0f, 0f, a);
        }
        else
        {
            fade.color = Color.clear;
            shown += dt;
        }
        UpdateCaption();

        if (Input.GetKeyDown(KeyCode.Escape)) { Finish(); return; }
        if (ScreenUI.Confirm() || Input.GetMouseButtonDown(0))
        {
            if (!FullyShown) shown = Line.Length / CharsPerSecond + 0.01f;   // 글자가 다 나오기 전이면 먼저 다 보여 줌
            else { Sfx.Play("click", 0.6f); Next(); }
        }
    }

    void Finish()
    {
        root.gameObject.SetActive(false);
        Time.timeScale = 1f;
        Sfx.Play("open", 0.7f);
    }
}
