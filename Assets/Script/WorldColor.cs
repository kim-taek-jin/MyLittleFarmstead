using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// "회색 도시에서 온 주인공" - 마음(Heart)이 회복될수록 세상에 색이 돌아옵니다.
// URP 포스트 프로세싱(Color Adjustments의 채도)을 코드로 만들어 적용하므로 씬 설정이 필요 없습니다.
public class WorldColor : MonoBehaviour
{
    private static WorldColor instance;

    [Tooltip("마음 0일 때 채도")] public float minSaturation = -80f;
    [Tooltip("마음 최대일 때 채도")] public float maxSaturation = 15f;
    [Tooltip("채도가 변하는 속도 (초당)")] public float changeSpeed = 15f;

    private Volume volume;
    private ColorAdjustments colorAdjustments;
    private float targetSaturation;
    private float currentSaturation;

    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[WorldColor]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WorldColor>();
    }

    // heart01: 0 ~ 1
    public static void SetHeart(float heart01, bool instant = false)
    {
        Ensure();
        instance.targetSaturation = Mathf.Lerp(instance.minSaturation, instance.maxSaturation, Mathf.Clamp01(heart01));
        if (instant)
        {
            instance.currentSaturation = instance.targetSaturation;
            instance.Apply();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;
        volume.sharedProfile = profile;

        targetSaturation = currentSaturation = minSaturation;
        Apply();

        SceneManager.sceneLoaded += OnSceneLoaded;
        EnablePostProcessingOnCameras();
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnablePostProcessingOnCameras();
    }

    // 카메라의 Post Processing 체크박스를 켜야 볼륨 효과가 보입니다.
    static void EnablePostProcessingOnCameras()
    {
        foreach (Camera cam in Camera.allCameras)
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    void Update()
    {
        if (!Mathf.Approximately(currentSaturation, targetSaturation))
        {
            currentSaturation = Mathf.MoveTowards(currentSaturation, targetSaturation, changeSpeed * Time.unscaledDeltaTime);
            Apply();
        }
    }

    void Apply()
    {
        if (colorAdjustments != null) colorAdjustments.saturation.value = currentSaturation;
    }
}
