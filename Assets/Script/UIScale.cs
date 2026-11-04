using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 장면에 원래 있던 UI(체력·에너지 막대, 시간, 핫바, 인벤토리, 상점 등)는 1920×1080 화면을 기준으로 픽셀 위치가 정해져 있고,
// Canvas Scaler가 'Constant Pixel Size'라서 Game 창 크기가 다르면 화면 밖으로 밀리거나 너무 크게 보였습니다.
// 장면 파일은 그대로 두고, 실행할 때 'Scale With Screen Size (1920×1080)'로 바꿔 어떤 화면 크기에서도 같은 배치로 보이게 합니다.
public static class UIScale
{
    public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    static void Apply()
    {
        // 한 화면짜리 장면: 배경 그림 밖이 보이지 않게 카메라 맞춤
        CameraFit.Attach(Camera.main);
        // 시작 인트로의 도시 그림은 늘 화면을 꽉 채우게
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (sr.name == "City_0" && sr.GetComponent<CoverCamera>() == null) sr.gameObject.AddComponent<CoverCamera>();
        // 장면의 글자도 픽셀 글꼴로
        if (PixelFont.Asset != null)
            foreach (TMPro.TMP_Text text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.font != PixelFont.Asset) text.font = PixelFont.Asset;

        foreach (CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) continue;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
