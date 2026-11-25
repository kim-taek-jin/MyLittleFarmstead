using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면 (D-1). 게임을 켜면 한 번 나옵니다 (농장 장면 위에 덮어서, 장면 파일은 그대로).
//  - 이어하기 (저장이 있을 때, 계절과 날짜 표시) / 새로 시작 (저장이 있으면 한 번 더 물어봄) / 설정 (ESC 메뉴) / 언어 / 끝내기
//  - 마우스로 누르거나, ↑↓ + Enter/Space로 고릅니다.
//  - 새로 시작하면 짧은 오프닝(Opening)이 나오고, 그다음 첫날 안내(FirstDayGuide)가 이어집니다.
// 배경 그림: World/title_bg (Tools/pixelart/opening.py, 왼쪽은 컬러, 오른쪽은 회색인 우리 집)
public class TitleScreen : MonoBehaviour
{
    public static bool Open => instance != null && instance.root != null && instance.root.gameObject.activeSelf;
    // 타이틀이나 오프닝이 떠 있는 동안에는 다른 키(가방, 지도, 앨범, 제작...)를 받지 않음
    public static bool Blocking => Open || Opening.Open;

    static TitleScreen instance;
    static bool shownThisSession;

    RectTransform root, menu;
    TextMeshProUGUI subtitle, hint, message;
    readonly List<(RectTransform box, TextMeshProUGUI label, System.Action act)> items = new List<(RectTransform, TextMeshProUGUI, System.Action)>();
    int selected;
    bool confirming;
    bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (shownThisSession || SceneManager.GetActiveScene().name != FarmMap.SceneName) return;
        shownThisSession = true;
        var go = new GameObject("TitleScreen");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TitleScreen>();
    }

    void Awake()
    {
        root = ScreenUI.Canvas("TitleCanvas", 505);
        root.SetParent(transform, false);
        Time.timeScale = 0f;
        Loc.Changed += Relabel;
    }

    void OnDestroy() { Loc.Changed -= Relabel; }

    // 메뉴는 첫 Update에서 만듦: 그때는 DataManager가 저장을 이미 불러온 뒤라 "이어하기"에 날짜를 쓸 수 있음
    void Build()
    {
        built = true;
        ScreenUI.EnsureEventSystem();
        RectTransform shade = ScreenUI.Rect("Back", root, Color.black, true);
        ScreenUI.Stretch(shade);
        Image bg = ScreenUI.Picture("Picture", root, "title_bg");
        ScreenUI.Stretch((RectTransform)bg.transform);

        // 제목: 그림자 한 겹 + 금색
        TextMeshProUGUI shadow = ScreenUI.Label("TitleShadow", root, 112f, TextAlignmentOptions.Center, new Color(0.1f, 0.06f, 0.04f, 0.9f));
        shadow.text = "My Little Farmstead";
        ScreenUI.At(shadow.rectTransform, new Vector2(0.5f, 1f), new Vector2(6f, -186f), new Vector2(1600f, 140f));
        TextMeshProUGUI title = ScreenUI.Label("Title", root, 112f, TextAlignmentOptions.Center, ScreenUI.Gold);
        title.text = "My Little Farmstead";
        ScreenUI.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1600f, 140f));
        subtitle = ScreenUI.Label("Subtitle", root, 34f, TextAlignmentOptions.Center, ScreenUI.Text);
        ScreenUI.At(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(1600f, 50f));
        RectTransform plate = ScreenUI.Rect("SubtitlePlate", root, new Color(0.08f, 0.05f, 0.04f, 0.55f));
        ScreenUI.At(plate, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(980f, 52f));
        plate.SetSiblingIndex(subtitle.transform.GetSiblingIndex());

        menu = new GameObject("Menu", typeof(RectTransform)).GetComponent<RectTransform>();
        menu.SetParent(root, false);
        ScreenUI.At(menu, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(560f, 520f));
        message = ScreenUI.Label("Message", root, 30f, TextAlignmentOptions.Center, ScreenUI.Gold);
        ScreenUI.At(message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 600f), new Vector2(1400f, 50f));
        hint = ScreenUI.Label("Hint", root, 22f, TextAlignmentOptions.Center, ScreenUI.Dim);
        ScreenUI.At(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 34f));
        ShowMain();
    }

    void Relabel()
    {
        if (!built) return;
        if (confirming) ShowConfirm(); else ShowMain();
    }

    // ---------------------------------------------------------------- 메뉴
    void ShowMain()
    {
        confirming = false;
        subtitle.text = Loc.T("A tired heart, a quiet village, and the colors slowly coming back.", "지친 마음이 시골에서 조금씩 색을 되찾는 이야기");
        hint.text = Loc.T("Up/Down choose   Enter select   (mouse works too)", "위/아래 키로 고르기   Enter 선택   (마우스도 돼요)");
        message.text = "";
        var list = new List<(string, System.Action)>();
        DataManager data = DataManager.instance;
        bool hasSave = SaveSystem.HasSave();
        if (hasSave && data != null)
        {
            int day = data.Today > 0 ? data.Today : 1;
            list.Add((Loc.T($"Continue  <size=24><color=#c8c0b0>{SeasonSystem.Name(data.CurrentSeason)}, day {day}</color></size>",
                            $"이어하기  <size=24><color=#c8c0b0>{SeasonSystem.Name(data.CurrentSeason)} {day}일째</color></size>"), Continue));
        }
        list.Add((hasSave ? Loc.T("New Game", "새로 시작") : Loc.T("Start", "시작하기"), hasSave ? (System.Action)ShowConfirm : StartFresh));
        list.Add((Loc.T("Settings", "설정"), OpenSettings));
        list.Add((Loc.T("Language: English", "언어: 한국어"), () => { Loc.Toggle(); Sfx.Play("click"); }));
        if (Application.platform != RuntimePlatform.WebGLPlayer) list.Add((Loc.T("Quit", "끝내기"), Quit));
        SetItems(list);
    }

    void ShowConfirm()
    {
        confirming = true;
        message.text = Loc.T("Start over? Your current save will be erased.", "새로 시작할까요? 지금 저장은 지워져요.");
        SetItems(new List<(string, System.Action)>
        {
            (Loc.T("Cancel", "아니요"), ShowMain),
            (Loc.T("Start over", "새로 시작"), StartOver),
        });
    }

    void SetItems(List<(string text, System.Action act)> list)
    {
        foreach (var it in items) Destroy(it.box.gameObject);
        items.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            RectTransform box = ScreenUI.Box("Item" + i, menu, ScreenUI.Border, ScreenUI.Fill);
            ScreenUI.At(box, new Vector2(0.5f, 1f), new Vector2(0f, -40f - i * 82f), new Vector2(520f, 66f));
            box.GetComponent<Image>().raycastTarget = true;
            Button b = box.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => { selected = index; Choose(); });
            TextMeshProUGUI label = ScreenUI.Label("Text", box, 34f, TextAlignmentOptions.Center, ScreenUI.Text);
            label.text = list[i].text;
            ScreenUI.Stretch(label.rectTransform);
            items.Add((box, label, list[i].act));
        }
        selected = 0;
        Highlight();
    }

    void Highlight()
    {
        for (int i = 0; i < items.Count; i++)
        {
            bool on = i == selected;
            items[i].box.GetComponent<Image>().color = on ? ScreenUI.Gold : ScreenUI.Border;
            items[i].label.color = on ? ScreenUI.Gold : ScreenUI.Text;
        }
    }

    void Choose()
    {
        if (selected < 0 || selected >= items.Count) return;
        Sfx.Play("click");
        items[selected].act();
    }

    void Update()
    {
        if (!built) { Build(); return; }
        if (!Open) return;
        Time.timeScale = 0f;
        if (PauseMenu.Open) return;                       // 설정 창이 위에 떠 있는 동안
        int before = selected;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) selected = (selected + items.Count - 1) % items.Count;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) selected = (selected + 1) % items.Count;
        if (before != selected) { Highlight(); Sfx.Play("pickup", 0.5f); }
        if (ScreenUI.Confirm()) Choose();
        else if (confirming && Input.GetKeyDown(KeyCode.Escape)) ShowMain();
    }

    // ---------------------------------------------------------------- 고른 것
    void Close()
    {
        root.gameObject.SetActive(false);
    }

    void Continue()
    {
        Close();
        Time.timeScale = 1f;
        DataManager data = DataManager.instance;
        if (data != null)
            HUD.Toast(Loc.T($"Welcome back. Day {data.Today}.", $"다시 왔어요. {data.Today}일째."));
    }

    // 저장이 없을 때: DataManager가 이미 새 게임 상태라, 바로 오프닝
    void StartFresh()
    {
        Close();
        Opening.Play();
    }

    // 저장이 있을 때: 저장을 지우고 농장을 다시 불러온 뒤 오프닝
    void StartOver()
    {
        Close();
        Opening.PlayOnNextLoad = true;
        if (DataManager.instance != null) DataManager.instance.StartNewGame();
        else Opening.Play();
    }

    void OpenSettings()
    {
        if (PauseMenu.Instance != null) PauseMenu.Instance.Show();
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
