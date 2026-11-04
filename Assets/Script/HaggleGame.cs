using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 상인과의 흥정 미니게임 (하루 1회).
// 성공하면 그날 시장 판매가가 오릅니다. 완벽(Perfect) +50%, 성공 +25%.
public class HaggleGame : MonoBehaviour
{
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
            resultText.text = "The merchant is busy today.\nCome back tomorrow!";
            Invoke(nameof(ReturnToFarm), 2.5f);
            return;
        }

        isGameActive = true;
        haggleSlider.value = 0;
        resultText.text = "Press Space at the right time!";
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
            resultText.text = $"PERFECT! Today's sell prices +{bonus * 100f:0}%";
        }
        else if (result >= successZoneStart && result <= successZoneEnd)
        {
            bonus = goodBonus;
            resultText.text = $"Deal! Today's sell prices +{bonus * 100f:0}%";
        }
        else
        {
            resultText.text = "Haggle failed... Try again tomorrow.";
        }

        if (DataManager.instance != null) DataManager.instance.ApplyHaggleResult(bonus);

        // 2초 뒤에 농장으로 돌아감
        Invoke(nameof(ReturnToFarm), 2f);
    }

    void ReturnToFarm()
    {
        FarmMap.ReturnFrom("MiniGame");   // 농장 북쪽 문 앞으로
        SceneManager.LoadScene("Farm");
    }
}
