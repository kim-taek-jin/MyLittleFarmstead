using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 상인과의 흥정 미니게임 (하루 1회).
// 성공하면 그날 시장 판매가가 오릅니다. 완벽(Perfect) +50%, 성공 +25%.
public class HaggleGame : MonoBehaviour
{
    // 흥정이 끝나면 돌아갈 곳 (장날 장터에서 왔으면 장터로)
    public static string ReturnScene = "Farm";
    public static Vector2 ReturnPosition;
    [Header("UI")]
    public Slider haggleSlider;
    public TextMeshProUGUI resultText;

    [Header("게임 설정")]
    public float barSpeed = 2f;
    public float successZoneStart = 0.4f; // 40% 지점
    public float successZoneEnd = 0.6f;   // 60% 지점
    [Tooltip("성공 구간 중앙에서 이 비율 안쪽이면 Perfect")]
    [Range(0f, 1f)] public float perfectRatio = 0.35f;

    public float goodBonus = 0.25f;
    public float perfectBonus = 0.5f;

    private bool isGameActive = false;
    private bool movingRight = true;

    void Start()
    {
        DataManager data = DataManager.instance;
        if (data != null && data.market.haggledToday)
        {
            resultText.text = Loc.T("You've already haggled today.\nCome back next market day!", "오늘은 이미 흥정했어요.\n다음 장날에 또 와요!");
            Invoke(nameof(ReturnToFarm), 2.5f);
            return;
        }

        isGameActive = true;
        haggleSlider.value = 0;
        resultText.text = Loc.T("Press Space at the right time!", "때를 맞춰 스페이스를 누르세요!");
    }

    void Update()
    {
        if (!isGameActive) return;

        if (movingRight)
        {
            haggleSlider.value += barSpeed * Time.deltaTime;
            if (haggleSlider.value >= 1) movingRight = false;
        }
        else
        {
            haggleSlider.value -= barSpeed * Time.deltaTime;
            if (haggleSlider.value <= 0) movingRight = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckResult();
        }
    }

    void CheckResult()
    {
        isGameActive = false;
        float result = haggleSlider.value;
        float center = (successZoneStart + successZoneEnd) * 0.5f;
        float halfWidth = (successZoneEnd - successZoneStart) * 0.5f;
        float distance = Mathf.Abs(result - center);

        float bonus = 0f;
        if (distance <= halfWidth * perfectRatio)
        {
            bonus = perfectBonus;
            resultText.text = Loc.T($"PERFECT! Today's sell prices +{bonus * 100f:0}%", $"완벽해요! 오늘 파는 값 +{bonus * 100f:0}%");
        }
        else if (result >= successZoneStart && result <= successZoneEnd)
        {
            bonus = goodBonus;
            resultText.text = Loc.T($"Deal! Today's sell prices +{bonus * 100f:0}%", $"그 값에 해요! 오늘 파는 값 +{bonus * 100f:0}%");
        }
        else
        {
            resultText.text = Loc.T("Haggle failed... Try again next market day.", "흥정 실패... 다음 장날에 다시 해 봐요.");
        }

        if (DataManager.instance != null) DataManager.instance.ApplyHaggleResult(bonus);

        // 2초 뒤에 농장으로 돌아감
        Invoke(nameof(ReturnToFarm), 2f);
    }

    void ReturnToFarm()
    {
        if (ReturnScene == "Market")
        {
            ReturnScene = "Farm";
            SceneData.nextPlayerPosition = ReturnPosition;
            SceneData.hasNextPlayerPosition = true;
            SceneManager.LoadScene("Market");
            return;
        }
        FarmMap.ReturnFrom("MiniGame");   // 농장 북쪽 문 앞으로
        SceneManager.LoadScene("Farm");
    }
}
