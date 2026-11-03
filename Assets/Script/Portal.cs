using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Tooltip("이동할 씬의 이름을 정확히 입력하세요.")]
    public string destinationSceneName;

    [Tooltip("도착할 씬에서의 플레이어 위치 (X, Y)")]
    public Vector2 destinationPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (DataManager.instance != null && DataManager.instance.IsGameOver) return;
        if (string.IsNullOrEmpty(destinationSceneName)) return;

        // 농장 데이터는 항상 DataManager에 있으므로 따로 저장할 필요가 없습니다.
        SceneData.nextPlayerPosition = destinationPosition;
        SceneData.hasNextPlayerPosition = true;
        SceneManager.LoadScene(destinationSceneName);
    }
}
