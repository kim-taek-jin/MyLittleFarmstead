using UnityEngine;

public class Bed : MonoBehaviour
{
    private bool isPlayerInRange;

    void Update()
    {
        // 플레이어가 범위 안에 있고, E키를 눌렀다면
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("잠자기 시도...");
            // DataManager의 Sleep 함수를 호출
            DataManager.instance.Sleep();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("침대 근처입니다. E키를 눌러 잠을 잘 수 있습니다.");
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