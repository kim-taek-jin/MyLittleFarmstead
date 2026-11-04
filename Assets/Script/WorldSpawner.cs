using UnityEngine;
using UnityEngine.SceneManagement;

// 씬이 열릴 때 코드로 주민과 낚시터를 배치합니다. (씬 파일 수정 없음)
public static class WorldSpawner
{
    public static readonly Vector2 FarmPond = new Vector2(7.05f, -0.6f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Spawn(scene);
        Spawn(SceneManager.GetActiveScene());
    }

    static void Spawn(Scene scene)
    {
        if (!scene.IsValid()) return;
        Material mat = FindSpriteMaterial();

        foreach (VillagerDef def in VillagerDB.All)
        {
            if (def.scene != scene.name) continue;
            Villager v = Villager.Create(def, mat, PixelArt.CharacterScale);
            SceneManager.MoveGameObjectToScene(v.gameObject, scene);
        }

        if (scene.name == "Farm")
        {
            FishingSpot spot = FishingSpot.Create(FarmPond, 1.4f);
            SceneManager.MoveGameObjectToScene(spot.gameObject, scene);
        }
    }

    // 2D 조명(URP Sprite-Lit)을 받도록 기존 스프라이트의 머티리얼을 빌려 씁니다.
    static Material FindSpriteMaterial()
    {
        if (PlayerAction.instance != null)
        {
            SpriteRenderer sr = PlayerAction.instance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sharedMaterial != null) return sr.sharedMaterial;
        }
        SpriteRenderer any = Object.FindFirstObjectByType<SpriteRenderer>();
        return any != null ? any.sharedMaterial : null;
    }
}
