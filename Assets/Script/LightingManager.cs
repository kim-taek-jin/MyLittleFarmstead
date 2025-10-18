using UnityEngine;
using UnityEngine.SceneManagement; // ✨ 씬(Scene) 관리 기능을 사용하기 위해 추가

public class LightingManager : MonoBehaviour
{
    [Header("참조 설정")]
    public Light directionalLight;
    public Camera mainCamera;

    private GameTimeManager gameTimeManager;

    [Header("시간대별 색상")]
    public Gradient lightColor;

    // ✨ Awake() 함수 추가: 씬 로드 이벤트를 구독합니다.
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ✨ OnDestroy() 함수 추가: 오브젝트 파괴 시 이벤트 구독을 해제합니다.
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✨ OnSceneLoaded() 함수 추가: 씬이 로드될 때마다 실행됩니다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새로 로드된 씬에서 카메라와 조명을 새로 찾습니다.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }
    }

    // Start() 함수는 이제 GameTimeManager를 찾는 역할만 합니다.
    void Start()
    {
        gameTimeManager = GetComponent<GameTimeManager>();
    }

    void Update()
    {
        // 조명이나 시간 매니저가 없으면 아무것도 하지 않음 (에러 방지)
        if (directionalLight == null || gameTimeManager == null) return;

        float timePercent = (gameTimeManager.hour + gameTimeManager.minute / 60f) / 24f;
        Color currentColor = lightColor.Evaluate(timePercent);

        directionalLight.color = currentColor;

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = currentColor;
        }
    }
}