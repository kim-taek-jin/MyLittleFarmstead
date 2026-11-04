using UnityEngine;
using UnityEngine.SceneManagement;

// 씬이 열릴 때마다 배경(농장, 집, 시장, 흥정, 도시)과 월드 오브젝트(나무, 돌, 사과, 우물, 상자, 침대, 출구, 상인)를 픽셀 아트로 바꿔 그립니다.
// 씬 파일은 건드리지 않고 실행 중에만 교체하므로, 언제든 예전 그림으로 되돌릴 수 있습니다.
public static class PixelArtWorld
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    public static void Apply(Scene scene)
    {
        if (!PixelArt.Enabled || !scene.IsValid()) return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name == "PixelArt") continue;
                if (sr.GetComponentInParent<Crop>() != null) continue;
                if (sr.CompareTag("Player")) continue;

                string key = KeyFor(sr);
                if (key == null) continue;
                Sprite sprite = PixelArt.Load("World/" + key);
                if (key.StartsWith("bg_"))
                    PixelArt.Overlay(sr, sprite, 1f, stretch: true);          // 배경: 원래 영역에 정확히
                else if (key == "tree")
                    PixelArt.Overlay(sr, sprite, 1.45f, anchorBottom: true);  // 나무: 플레이어보다 크게, 밑동 고정
                else
                    PixelArt.Overlay(sr, sprite);
            }
        }

        // 플레이어는 DontDestroyOnLoad라 씬 루트에 없으므로 따로 처리
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.GetComponent<PlayerPixelAnimator>() == null)
        {
            player.AddComponent<PlayerPixelAnimator>();
        }
    }

    static string KeyFor(SpriteRenderer sr)
    {
        if (sr.CompareTag("Wood")) return "tree";
        if (sr.CompareTag("Stone")) return "rock";
        if (sr.CompareTag("Food")) return "apple_ground";
        if (sr.sprite == null || sr.sprite.texture == null) return null;

        string tex = sr.sprite.texture.name;
        switch (tex)
        {
            case "BackGroundFarm": return "bg_farm";
            case "BackGroundHome": return "bg_home";
            case "BackGroundMarket": return "bg_market";
            case "BGMG": return "bg_minigame";
            case "City": return "bg_city";
        }
        if (tex == "well") return "well";
        if (tex == "Box") return "storage_box";
        if (tex == "bed_sprite_preview") return "bed";
        if (tex == "Exit") return "exit_sign";
        if (tex.StartsWith("ChatGPT")) return "merchant";
        return null;
    }
}
