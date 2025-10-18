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
        if (other.CompareTag("Player"))
        {
            // ✨ 1. 현재 씬이 "Farm"인지 확인합니다.
            if (SceneManager.GetActiveScene().name == "Farm")
            {
                // ✨ 2. Farm 씬에 있는 CropManager를 찾습니다.
                CropManager cropManager = FindObjectOfType<CropManager>();
                if (cropManager != null)
                {
                    // ✨ 3. CropManager에게 저장을 명령합니다.
                    cropManager.SaveCropData();
                }
            }

            // 4. 스폰 위치를 기록하고 씬을 이동합니다.
            SceneData.nextPlayerPosition = destinationPosition;
            SceneData.hasNextPlayerPosition = true;
            SceneManager.LoadScene(destinationSceneName);
        }
    }
}