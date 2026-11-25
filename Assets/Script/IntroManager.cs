using UnityEngine;
using UnityEngine.UI;

// 예전 시작 인트로 패널 (Farm 장면의 IntroPanel). 이제는 타이틀 화면(TitleScreen)과 짧은 오프닝(Opening),
// 첫날 안내(FirstDayGuide)가 대신하므로, 장면에 남아 있는 이 패널은 바로 숨깁니다.
// (장면 파일을 건드리지 않으려고 컴포넌트는 그대로 둡니다. startButton은 장면에서 연결된 값이라 남겨 둠)
public class IntroManager : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        gameObject.SetActive(false);
    }
}
