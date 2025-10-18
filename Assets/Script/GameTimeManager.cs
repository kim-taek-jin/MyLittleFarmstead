using UnityEngine;

public class GameTimeManager : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("1초당 게임 내에서 흐르는 시간(분)")]
    public float minutesPerSecond = 60f; // 현실 시간 1초가 게임 내 1시간(60분)이 되도록 설정

    [Header("현재 게임 시간")]
    public int day = 1;
    public int hour = 6;
    public float minute = 0;

    void Update()
    {
        // 1. 시간 계산
        // Time.deltaTime은 이전 프레임과 현재 프레임 사이의 시간 간격입니다.
        // 여기에 minutesPerSecond를 곱해서 게임 내 '분'을 계산합니다.
        minute += Time.deltaTime * minutesPerSecond;

        // 2. 분/시/일 단위 정리
        if (minute >= 60)
        {
            hour++;          // 60분이 넘으면 시간을 1 올리고
            minute = 0;      // 분은 0으로 리셋
        }

        if (hour >= 24)
        {
            day++;           // 24시가 넘으면 날짜를 1 올리고
            hour = 0;        // 시간은 0으로 리셋
        }

        // 3. 시간 출력 (테스트용)
        // F12를 눌러 콘솔 창을 열면 시간이 흐르는 것을 볼 수 있습니다.
     //   Debug.Log($"현재 시간: {day}일차 {hour:00}:{minute:00}");
    }

    public void AdvanceDay()
    {
        day++;       // 날짜를 1 증가
        hour = 6;    // 아침 6시로 시간 설정
        minute = 0;
        Debug.Log(day + "일차 아침이 밝았습니다.");
    }
}