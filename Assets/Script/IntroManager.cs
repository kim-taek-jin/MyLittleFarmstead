using UnityEngine;
using UnityEngine.UI;

// 처음 시작할 때 보여주는 인트로 패널. 저장 파일이 있으면 건너뜁니다.
public class IntroManager : MonoBehaviour
{
    public Button startButton;

    // 인트로를 이미 보여줬는지 기억하는 static 변수
    private static bool introShown = false;

    void Start()
    {
        if (introShown || SaveSystem.HasSave())
        {
            introShown = true;
            this.gameObject.SetActive(false);
            return;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(CloseIntro);
        }
        Time.timeScale = 0f; // 게임 시간 멈춤
    }

    void CloseIntro()
    {
        Time.timeScale = 1f;
        this.gameObject.SetActive(false);
        introShown = true;

        HUD.Announce(
            "<size=40>A new life begins</size>\n\n" +
            "<size=26>The city left your world gray. Live well and the color will come back.\n\n" +
            "WASD: Move    1-5 / Mouse wheel: Select item\n" +
            "Left click: Use tool / seed    Right click or F: Eat\n" +
            "E: Interact / talk    C: Craft    J: Journal    P: Album    G: Feed spirit\n" +
            "F5: Save    F9: Load\n\n" +
            "Eat, drink, rest, and go to bed on time to heal your <color=#ff8fb1>Heart</color>.\n" +
            "The villagers have lost their color too. Maybe you can help them.\n" +
            "It's winter in your heart. Live well, and spring will come.</size>",
            12f);
    }
}
