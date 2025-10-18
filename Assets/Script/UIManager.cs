using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 꼭 필요합니다!

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timeText; // 시간을 표시할 텍스트
    public TextMeshProUGUI dayText;  // ✨ 날짜를 표시할 텍스트 변수 추가

    [Header("Managers")]
    public GameTimeManager gameTimeManager; // 시간 정보를 가져올 매니저

    void Start()
    {
        // GameManager에서 GameTimeManager를 찾아옵니다.
        if (gameTimeManager == null)
        {
            gameTimeManager = FindFirstObjectByType<GameTimeManager>();
        }
    }

    void Update()
    {
        // GameTimeManager가 연결되어 있는지 확인
        if (gameTimeManager != null)
        {
            // 시간 UI 업데이트
            if (timeText != null)
            {
                int hour = gameTimeManager.hour;
                int minute = (int)gameTimeManager.minute;
                timeText.text = $"{hour:00}:{minute:00}";
            }

            // ✨ 날짜 UI 업데이트 로직 추가
            if (dayText != null)
            {
                dayText.text = "Day: " + gameTimeManager.day;
            }
        }
    }
}