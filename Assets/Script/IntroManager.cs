using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    public Button startButton;

    // ✨ 인트로를 이미 보여줬는지 기억하는 static 변수 추가
    private static bool introShown = false;

    void Start()
    {
        // ✨ 만약 인트로를 이미 보여줬다면
        if (introShown)
        {
            // 즉시 패널을 끄고 함수를 종료 (시간은 건드리지 않음)
            this.gameObject.SetActive(false);
            return;
        }

        // 처음 시작하는 경우:
        if (startButton != null)
        {
            startButton.onClick.AddListener(CloseIntro);
        }
        Time.timeScale = 0f; // 게임 시간 멈춤
    }

    void CloseIntro()
    {
        Time.timeScale = 1f; // 게임 시간 다시 흐르게 함
        this.gameObject.SetActive(false); // 패널 끄기

        // ✨ 인트로를 보여줬다고 기록
        introShown = true;
    }
}