using UnityEngine;

// 침대: E키로 잠을 자면 하루가 끝나고, 그날의 생활을 평가해 마음(Heart)이 변합니다.
public class Bed : MonoBehaviour
{
    private bool isPlayerInRange;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && DataManager.instance != null)
        {
            DataManager.instance.TrySleep();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            HUD.Toast("Press E to sleep and end the day.", 2.5f);
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
