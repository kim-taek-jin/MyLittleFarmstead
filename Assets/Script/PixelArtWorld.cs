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
            ApplyTo(root);
        }

        // 플레이어는 DontDestroyOnLoad라 씬 루트에 없으므로 따로 처리
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.GetComponent<PlayerPixelAnimator>() == null)
        {
            player.AddComponent<PlayerPixelAnimator>();
        }
    }

    // 한 오브젝트(와 자식)의 스프라이트를 픽셀 아트로. 이미 바꾼 것은 그대로 둡니다. (FarmMap이 복제한 자원에도 씀)
    public static void ApplyTo(GameObject root)
    {
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name == "PixelArt") continue;
            if (sr.GetComponentInParent<Crop>() != null) continue;
            if (sr.CompareTag("Player")) continue;

            string key = KeyFor(sr);
            if (key == null) continue;
            Sprite sprite = SeasonalSprite(key);
            SpriteRenderer view;
            if (key == "bg_farmmap")
                view = FarmMap.PlaceGround(sr, sprite);                                 // 넓어진 농장: 맵 전체 그림
            else if (key.StartsWith("bg_"))
                view = PixelArt.Overlay(sr, sprite, 1f, stretch: true);                 // 배경: 원래 영역에 정확히
            else if (key == "tree")
                view = PixelArt.Overlay(sr, sprite, anchorBottom: true, native: true);  // 나무: 배경과 같은 픽셀 크기, 밑동 고정
            else
                view = PixelArt.Overlay(sr, sprite, native: true);                      // 나머지도 배경과 같은 픽셀 크기
            if (view != null && view.GetComponent<SeasonalArt>() == null) view.gameObject.AddComponent<SeasonalArt>().baseKey = key;
        }
    }

    // 마음의 계절에 맞는 그림 (예: bg_farm_winter, tree_autumn). 없으면 기본 그림.
    public static Sprite SeasonalSprite(string key)
    {
        DataManager data = DataManager.instance;
        string season = data != null ? SeasonSystem.Key(data.CurrentSeason) : "winter";
        return PixelArt.Load($"World/{key}_{season}") ?? PixelArt.Load("World/" + key);
    }

    // 계절이 바뀌거나 저장을 불러온 뒤: 이미 그려진 월드 그림을 계절에 맞게 바꿉니다.
    public static void RefreshSeasonal()
    {
        foreach (SeasonalArt art in Object.FindObjectsByType<SeasonalArt>(FindObjectsSortMode.None))
        {
            SpriteRenderer sr = art.GetComponent<SpriteRenderer>();
            Sprite s = SeasonalSprite(art.baseKey);
            if (sr != null && s != null) sr.sprite = s;
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
            case "BackGroundFarm": return FarmMap.Active && SeasonalSprite("bg_farmmap") != null ? "bg_farmmap" : "bg_farm";
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
