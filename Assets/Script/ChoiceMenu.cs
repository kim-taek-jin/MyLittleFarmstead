using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 고르기 창 (K-4): 셋, 넷 중 하나를 고름. [1]~[4] 또는 클릭. (대화창의 예/아니요로는 모자랄 때)
// 의식에서 "달집에 무엇을 넣을까", "소원지에 무엇을 쓸까", "새알심을 빚으며 떠올린 것"에 씀. 취소는 없음.
public class ChoiceMenu : MonoBehaviour
{
    static ChoiceMenu instance;
    public static bool Open => instance != null && instance.root != null && instance.root.gameObject.activeSelf;

    RectTransform root;
    TextMeshProUGUI title;
    readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
    readonly List<RectTransform> rows = new List<RectTransform>();
    System.Action<int> onPick;
    int count, openedFrame;

    public static void Show(string question, string[] options, System.Action<int> pick)
    {
        if (instance == null)
        {
            var go = new GameObject("ChoiceMenu");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ChoiceMenu>();
            instance.Build();
        }
        instance.Fill(question, options, pick);
    }

    void Build()
    {
        ScreenUI.EnsureEventSystem();
        RectTransform canvas = ScreenUI.Canvas("ChoiceCanvas", 515);
        canvas.SetParent(transform, false);
        root = ScreenUI.Rect("Dim", canvas, new Color(0f, 0f, 0f, 0.45f), true);
        ScreenUI.Stretch(root);
        RectTransform box = ScreenUI.Box("Box", root, ScreenUI.Border, ScreenUI.Fill);
        ScreenUI.At(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
        title = ScreenUI.Label("Title", box, 30f, TextAlignmentOptions.Center, ScreenUI.Gold);
        ScreenUI.At((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 70f));
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            RectTransform row = ScreenUI.Box("Option" + i, box, new Color(0.45f, 0.34f, 0.22f, 1f), new Color(0.13f, 0.09f, 0.07f, 1f), 3f);
            ScreenUI.At(row, new Vector2(0.5f, 1f), new Vector2(0f, -128f - i * 68f), new Vector2(660f, 58f));
            row.GetComponent<Image>().raycastTarget = true;
            Button b = row.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => Choose(index));
            TextMeshProUGUI label = ScreenUI.Label("Label", row, 26f, TextAlignmentOptions.Left, ScreenUI.Text);
            ScreenUI.Stretch((RectTransform)label.transform, 18f);
            rows.Add(row);
            labels.Add(label);
        }
        root.gameObject.SetActive(false);
    }

    void Fill(string question, string[] options, System.Action<int> pick)
    {
        title.text = question;
        count = Mathf.Min(options.Length, 4);
        for (int i = 0; i < 4; i++)
        {
            rows[i].gameObject.SetActive(i < count);
            if (i < count) labels[i].text = $"<color=#ffd84a>[{i + 1}]</color>  {options[i]}";
        }
        onPick = pick;
        openedFrame = Time.frameCount;
        root.gameObject.SetActive(true);
    }

    void Choose(int i)
    {
        if (!Open || i < 0 || i >= count) return;
        root.gameObject.SetActive(false);
        System.Action<int> pick = onPick;
        onPick = null;
        pick?.Invoke(i);
    }

    void Update()
    {
        if (!Open || Time.frameCount <= openedFrame + 1) return;
        for (int i = 0; i < count; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Choose(i); return; }
    }
}
