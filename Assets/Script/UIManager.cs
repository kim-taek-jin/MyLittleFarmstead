using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timeText; // 시간을 표시할 텍스트
    public TextMeshProUGUI dayText;  // 날짜를 표시할 텍스트

    [Header("Managers")]
    public GameTimeManager gameTimeManager; // 시간 정보를 가져올 매니저

    void Update()
    {
        // 씬 안의 중복 GameManager가 아니라 살아있는 GameTimeManager를 사용
        if (gameTimeManager == null || !gameTimeManager.enabled) gameTimeManager = GameTimeManager.instance;
        if (gameTimeManager == null) return;

        if (timeText != null)
        {
            timeText.text = gameTimeManager.FormattedTime;
        }

        if (dayText != null)
        {
            DataManager data = DataManager.instance;
            string season = data != null ? SeasonSystem.Name(data.CurrentSeason) : "";
            string weather = data != null && data.snowing ? "  (Snow)" : (data != null && data.raining ? "  (Rain)" : "");
            dayText.text = $"{season} {gameTimeManager.day}{weather}";
        }
    }
}
