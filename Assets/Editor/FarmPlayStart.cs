using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// 에디터 전용 편의 기능 (게임 빌드에는 들어가지 않음)
//  - ▶(Play)을 누르면 지금 열린 장면과 상관없이 항상 농장(Farm)부터 시작합니다.
//    게임 관리자(GameManager, DataManager)가 Farm 장면에 있어서, 다른 장면에서 바로 시작하면 게임이 돌아가지 않기 때문입니다.
//  - 프로젝트를 처음 열어 빈 장면(Untitled)만 있으면 Farm 장면을 자동으로 엽니다.
// 끄고 싶으면 이 파일을 지우면 됩니다.
[InitializeOnLoad]
static class FarmPlayStart
{
    const string FarmPath = "Assets/Scenes/Farm.unity";

    static FarmPlayStart()
    {
        EditorApplication.delayCall += Setup;
    }

    static void Setup()
    {
        SceneAsset farm = AssetDatabase.LoadAssetAtPath<SceneAsset>(FarmPath);
        if (farm == null) return;
        EditorSceneManager.playModeStartScene = farm;

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene active = SceneManager.GetActiveScene();
        if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path) && !active.isDirty)
            EditorSceneManager.OpenScene(FarmPath);
    }
}
