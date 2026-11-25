using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 아침 마을 방송을 화면 위쪽 가운데에 한 줄씩 보여 줍니다 (K-1). "띵동" 소리와 함께, 글자가 한 자씩.
// 밖(농장, 장터)에 나와 있고 다른 창이 없을 때 나옵니다. 한 줄씩 저절로 넘어가고, E/Space로 빨리 넘길 수 있음.
public class BroadcastBanner : MonoBehaviour
{
    static BroadcastBanner instance;
    public static bool Showing => instance != null && instance.root != null && instance.root.gameObject.activeSelf;
    static readonly Queue<string> pending = new Queue<string>();

    RectTransform root, box;
    TextMeshProUGUI speaker, text;
    string current;
    float shown, stay;

    const float CharsPerSecond = 22f;

    public static void Queue(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return;
        Ensure();
        pending.Clear();
        foreach (string l in lines) pending.Enqueue(l);
    }

    static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("[BroadcastBanner]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BroadcastBanner>();
        instance.Build();
    }

    void Build()
    {
        root = ScreenUI.Canvas("BroadcastCanvas", 460);
        root.SetParent(transform, false);
        Destroy(root.GetComponent<GraphicRaycaster>());
        box = ScreenUI.Box("Box", root, ScreenUI.Border, ScreenUI.Fill);
        ScreenUI.At(box, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(940f, 118f));
        Image icon = ScreenUI.Picture("Speaker", box, "loudspeaker");
        ScreenUI.At((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(64f, 64f));
        icon.preserveAspect = true;
        speaker = ScreenUI.Label("Name", box, 22f, TextAlignmentOptions.Left, ScreenUI.Gold);
        ScreenUI.At(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(520f, -24f), new Vector2(820f, 30f));
        text = ScreenUI.Label("Text", box, 28f, TextAlignmentOptions.TopLeft, ScreenUI.Text);
        ScreenUI.At(text.rectTransform, new Vector2(0f, 1f), new Vector2(520f, -76f), new Vector2(820f, 70f));
        root.gameObject.SetActive(false);
    }

    static bool CanShow()
    {
        string scene = SceneManager.GetActiveScene().name;
        return (scene == "Farm" || scene == "Market") && Time.timeScale > 0f && !HUD.AnnouncementOpen && !HUD.DialogueOpen
               && !PixelHUD.ViewOpen && !TitleScreen.Blocking && !PauseMenu.Open;
    }

    void Update()
    {
        if (current == null)
        {
            if (pending.Count == 0 || !CanShow()) { root.gameObject.SetActive(false); return; }
            bool first = !root.gameObject.activeSelf;
            current = pending.Dequeue();
            shown = 0f;
            stay = 1.8f + current.Length / 14f;
            speaker.text = Loc.T("Village broadcast", "마을 방송") + "  ·  " + VillageBroadcast.Speaker;
            root.gameObject.SetActive(true);
            if (first) Sfx.Play("open", 0.8f);      // 띵동
        }
        shown += Time.unscaledDeltaTime;
        int n = Mathf.Clamp(Mathf.FloorToInt(shown * CharsPerSecond), 0, current.Length);
        text.text = current.Substring(0, n);
        bool done = n >= current.Length;
        bool skip = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        if (skip && !done) shown = current.Length / CharsPerSecond + 0.01f;
        else if ((done && shown > current.Length / CharsPerSecond + stay) || (skip && done)) current = null;
    }
}
