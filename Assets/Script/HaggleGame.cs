using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HaggleGame : MonoBehaviour
{
    [Header("UI")]
    public Slider haggleSlider;
    public TextMeshProUGUI resultText;

    [Header("게임 설정")]
    public float barSpeed = 2f;
    public float successZoneStart = 0.4f; // 40% 지점
    public float successZoneEnd = 0.6f;   // 60% 지점

    private bool isGameActive = false;
    private bool movingRight = true;

    void Start()
    {
        isGameActive = true;
        haggleSlider.value = 0;
        resultText.text = "Press Space at the right time!";
    }

    void Update()
    {
        if (isGameActive)
        {
            // 막대 움직이기
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

            // 스페이스바를 누르면 판정
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CheckResult();
            }
        }
    }

    void CheckResult()
    {
        isGameActive = false;
        float result = haggleSlider.value;

        if (result >= successZoneStart && result <= successZoneEnd)
        {
            // ✨ "흥정 대성공! +50 골드"를 영어로 수정
            resultText.text = "Great Success! +50 Gold";
            DataManager.instance.AddGold(50);
        }
        else
        {
            // ✨ "흥정 실패..."를 영어로 수정
            resultText.text = "Haggle Failed...";
        }

        // 2초 뒤에 농장으로 돌아감
        Invoke("ReturnToFarm", 2f);
    }

    void ReturnToFarm()
    {
        SceneManager.LoadScene("Farm");
    }
}