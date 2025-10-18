using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        // 마우스를 클릭했을 때
        if (Input.GetMouseButtonDown(0))
        {
            // 현재 마우스 위치에 어떤 UI가 있는지 확인
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // 만약 감지된 UI가 있다면
            if (results.Count > 0)
            {
                // 가장 위에 있는 UI의 이름을 콘솔에 출력
                Debug.Log($"UI 클릭 감지! 범인: {results[0].gameObject.name}");
            }
            else
            {
                Debug.Log("UI 클릭이 아님.");
            }
        }
    }
}