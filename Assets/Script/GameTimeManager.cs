using UnityEngine;

// 게임 시간. 아침 6시에 시작하고, 잠들지 않고 새벽 2시(26시)가 되면 기절합니다.
public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager instance;

    [Header("시간 설정")]
    [Tooltip("현실 1초당 흐르는 게임 시간(분). 2 = 하루(06:00~02:00)가 약 10분")]
    public float minutesPerSecond = 2f;

    [Tooltip("이 시각(24 이상 = 다음 날 새벽)이 되면 기절합니다. 26 = 새벽 2시")]
    public int passOutHour = 26;

    [Header("현재 게임 시간")]
    public int day = 1;
    public int hour = 6;      // 24 이상이면 자정을 넘긴 새벽 (예: 25 = 01시)
    public float minute = 0;

    private bool warnedMidnight;
    private bool warnedLate;

    // 화면 표시용 시각 (예: "01:30")
    public string FormattedTime => $"{hour % 24:00}:{(int)minute:00}";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false; // 중복 GameManager는 곧 파괴됩니다.
            return;
        }
        instance = this;
    }

    void Update()
    {
        DataManager data = DataManager.instance;
        if (data != null && data.IsGameOver) return;

        minute += Time.deltaTime * minutesPerSecond;
        while (minute >= 60f)
        {
            minute -= 60f;
            hour++;
        }

        if (hour >= 24 && !warnedMidnight)
        {
            warnedMidnight = true;
            HUD.Toast("It's midnight. You should go to bed soon.");
        }
        if (hour >= passOutHour - 1 && !warnedLate)
        {
            warnedLate = true;
            HUD.Toast("You can barely keep your eyes open...");
        }

        if (hour >= passOutHour)
        {
            if (data != null)
            {
                data.PassOut(); // 내부에서 AdvanceDay() 호출
            }
            else
            {
                AdvanceDay();
            }
        }
    }

    public void AdvanceDay()
    {
        day++;
        hour = 6;
        minute = 0;
        warnedMidnight = false;
        warnedLate = false;
        Debug.Log(day + "일차 아침이 밝았습니다.");
    }

    public void SetTime(int newDay, int newHour, float newMinute)
    {
        day = Mathf.Max(1, newDay);
        hour = newHour;
        minute = newMinute;
        warnedMidnight = hour >= 24;
        warnedLate = hour >= passOutHour - 1;
    }
}
