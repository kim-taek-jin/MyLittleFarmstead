using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요
using System.Collections; // 코루틴을 사용하기 위해 필요

public class TypingEffect : MonoBehaviour
{
    public TextMeshProUGUI textComponent; // 효과를 적용할 텍스트
    public float typingSpeed = 0.05f;     // 글자당 나타나는 속도 (초)

    private string fullText;
    private Coroutine typingCoroutine;

    // 외부에서 텍스트 효과를 시작시키는 함수
    public void RunText(string textToType)
    {
        fullText = textToType;
        // 혹시 이전에 실행 중이던 코루틴이 있다면 멈춤
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        // 새 코루틴 시작
        typingCoroutine = StartCoroutine(ShowText());
    }

    // 실제로 글자를 하나씩 보여주는 코루틴
    IEnumerator ShowText()
    {
        textComponent.text = ""; // 텍스트를 비움
        // 텍스트의 각 글자를 순서대로 처리
        foreach (char c in fullText.ToCharArray())
        {
            textComponent.text += c; // 글자를 하나 추가
            yield return new WaitForSeconds(typingSpeed); // 설정된 시간만큼 잠시 멈춤
        }
    }
}