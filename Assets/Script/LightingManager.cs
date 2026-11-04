using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 시간대에 따라 빛을 바꿉니다.
//  - (기존) Directional Light 색 / 카메라 배경색을 Gradient로
//  - (추가) 씬의 Global Light 2D 밝기를 낮/밤, 비에 맞춰 조절
// GameTimeManager와 같은 오브젝트(GameManager)에 있을 때만 동작합니다.
public class LightingManager : MonoBehaviour
{
    [Header("참조 설정")]
    public Light directionalLight;
    public Camera mainCamera;

    private GameTimeManager gameTimeManager;

    [Header("시간대별 색상")]
    public Gradient lightColor;
    [Tooltip("체크하면 카메라 배경색도 Gradient 색으로 바꿉니다.")]
    public bool tintCameraBackground = true;

    [Header("2D 전역 조명 밝기")]
    [Range(0f, 1f)] public float nightIntensity = 0.45f;
    [Range(0f, 1f)] public float rainMultiplier = 0.8f;

    private Light2D globalLight2D;
    private float baseIntensity = 1f;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (directionalLight == null) directionalLight = FindFirstObjectByType<Light>();
        FindGlobalLight();
    }

    void Start()
    {
        gameTimeManager = GetComponent<GameTimeManager>();
        FindGlobalLight();
    }

    void FindGlobalLight()
    {
        globalLight2D = null;
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                globalLight2D = light;
                baseIntensity = light.intensity;
                break;
            }
        }
    }

    void Update()
    {
        if (gameTimeManager == null || !gameTimeManager.enabled) return;

        float hourValue = gameTimeManager.hour + gameTimeManager.minute / 60f;
        float timePercent = (hourValue % 24f) / 24f;

        if (directionalLight != null && lightColor != null)
        {
            Color currentColor = lightColor.Evaluate(timePercent);
            directionalLight.color = currentColor;
            if (tintCameraBackground && mainCamera != null) mainCamera.backgroundColor = currentColor;
        }

        if (globalLight2D != null)
        {
            float daylight = GetDaylight(hourValue);
            float intensity = Mathf.Lerp(nightIntensity, 1f, daylight) * baseIntensity;
            DataManager data = DataManager.instance;
            if (data != null && (data.raining || data.snowing)) intensity *= rainMultiplier;
            globalLight2D.intensity = intensity;
        }
    }

    // 0 = 밤, 1 = 낮. 17~21시에 어두워지고, 4~7시에 밝아집니다.
    static float GetDaylight(float hour)
    {
        if (hour >= 24f) return 0f;
        if (hour < 4f) return 0f;
        if (hour < 7f) return Mathf.InverseLerp(4f, 7f, hour);
        if (hour < 17f) return 1f;
        if (hour < 21f) return 1f - Mathf.InverseLerp(17f, 21f, hour);
        return 0f;
    }
}
