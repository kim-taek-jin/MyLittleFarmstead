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

        HUD.Announce(Loc.Ko ?
            "<size=40>새로운 삶이 시작돼요</size>\n\n" +
            "<size=26>도시에서 지내는 동안 세상이 잿빛이 되었어요. 잘 지내다 보면 빛깔이 돌아와요.\n\n" +
            "WASD: 이동    1-5 / 마우스 휠: 물건 고르기\n" +
            "왼쪽 클릭: 도구 / 씨앗 쓰기    오른쪽 클릭 또는 F: 먹기\n" +
            "E: 살펴보기 / 말 걸기    C: 만들기    J: 일기    P: 앨범    G: 정령 먹이 주기\n" +
            "F5: 저장    F9: 불러오기\n\n" +
            "먹고, 마시고, 쉬고, 제때 잠자리에 들면 <color=#ff8fb1>마음</color>이 나아요.\n" +
            "마을 사람들도 빛깔을 잃었어요. 도와줄 수 있을지도 몰라요.\n" +
            "마음은 아직 겨울이에요. 잘 지내다 보면 봄이 올 거예요.</size>" :
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
