using UnityEngine;

// 이 오브젝트(와 자식)의 그림을 숨깁니다. 픽셀 아트 교체가 끝난 뒤(Start)에 숨겨서, 나중에 생긴 그림도 함께 숨깁니다.
public class HideArt : MonoBehaviour
{
    void Start()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
    }
}
