using UnityEngine;

public class Well : MonoBehaviour
{
    private bool isPlayerInRange;
    private Hotbar hotbar;

    void Start()
    {
        hotbar = FindObjectOfType<Hotbar>();
    }

    void Update()
    {
        // 플레이어가 범위 안에 있고, E키를 눌렀다면
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // 핫바에서 선택된 아이템이 물뿌리개인지 확인
            string selectedItem = hotbar?.GetSelectedItemName();
            if (selectedItem == "Watering Can")
            {
                // DataManager의 갈증 해소 함수를 호출
                DataManager.instance.RefillThirst();
            }
            else
            {
                Debug.Log("물뿌리개를 들고 있어야 물을 마실 수 있습니다.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}