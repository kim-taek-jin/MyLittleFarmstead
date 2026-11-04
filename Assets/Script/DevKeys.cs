using UnityEngine;
using UnityEngine.SceneManagement;

// 개발용 단축키 (Unity 에디터와 Development Build에서만 동작, 출시 빌드에서는 꺼짐)
//  F1  다음 마음의 계절로 (마음을 올리고 하루를 넘김 → 숲/호수/언덕이 열림)
//  F2  마음 +10
//  F3  하루 넘기기
//  F4  테스트 아이템 받기 (곡괭이, 낚싯대, 도토리, 대지의 숨결, 감, 물고기, 나무, 돌, 골드)
//  F6  농장 → 숲 → 호수 → 언덕 순서로 순간이동 (열린 곳만)
//  F7  엔딩 조건 맞추기 (주민 3명 색 회복 + 마음 95) → 집에서 자면 엔딩
//  F8  이 목록 보기
public class DevKeys : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!Debug.isDebugBuild) return;
        var go = new GameObject("DevKeys");
        DontDestroyOnLoad(go);
        go.AddComponent<DevKeys>();
    }

    void Update()
    {
        DataManager data = DataManager.instance;
        if (data == null || HUD.DialogueOpen) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            int next = (int)data.CurrentSeason + 1;
            if (next >= SeasonSystem.Thresholds.Length) { HUD.Toast("[DEV] It's already autumn in your heart."); return; }
            if (data.demoMode) HUD.Toast("[DEV] Demo Mode is on: the season stops at winter. Turn it off on GameManager.", 4f);
            data.heart = Mathf.Max(data.heart, SeasonSystem.Thresholds[next] + 1f);
            data.DevEndDay();
            Reload();
        }
        else if (Input.GetKeyDown(KeyCode.F2)) data.ChangeHeart(10f, "[DEV] +10 Heart");
        else if (Input.GetKeyDown(KeyCode.F3)) { data.DevEndDay(); Reload(); }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            foreach (var (item, n) in new (string, int)[] { ("Mattock", 1), ("Fishing Rod", 1), ("Acorn", 3), ("Earth's Breath", 6), ("Persimmon", 6),
                                                            ("Minnow", 5), ("Carp", 5), ("Wood", 20), ("Stone", 20) })
                if (item == "Mattock" || item == "Fishing Rod" ? !data.HasItem(item) : true) data.AddItem(item, n);
            data.AddGold(500);
            HUD.Toast("[DEV] Test items added (+500G).");
        }
        else if (Input.GetKeyDown(KeyCode.F6)) Teleport();
        else if (Input.GetKeyDown(KeyCode.F7))
        {
            foreach (VillagerDef v in VillagerDB.All) { VillagerProgress p = data.story.Get(v.id); p.met = true; p.wishIndex = 3; }
            data.heart = Mathf.Max(data.heart, 95f);
            data.UpdateAllUI();
            HUD.Toast("[DEV] Ending is ready. Go home and sleep (or wake up at home).", 4f);
        }
        else if (Input.GetKeyDown(KeyCode.F8))
            HUD.Toast("[DEV] F1 next season  F2 +10 Heart  F3 next day  F4 items  F6 teleport  F7 ending ready", 6f);
    }

    static void Reload()
    {
        PlayerAction player = PlayerAction.instance;
        if (player != null)
        {
            SceneData.nextPlayerPosition = player.transform.position;
            SceneData.hasNextPlayerPosition = true;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static void Teleport()
    {
        PlayerAction player = PlayerAction.instance;
        if (player == null || SceneManager.GetActiveScene().name != FarmMap.SceneName) { HUD.Toast("[DEV] Teleport works on the farm."); return; }
        var stops = new System.Collections.Generic.List<Vector2> { new Vector2(0.9f, -2f) };
        if (ForestArea.IsOpen && ForestArea.Map != null && ForestArea.Map.TryArrival("Farm", out Vector2 f)) stops.Add(f);
        if (LakeArea.IsOpen && LakeArea.Map != null && LakeArea.Map.TryArrival("Farm", out Vector2 l)) stops.Add(l);
        if (HillArea.IsOpen && HillArea.Map != null && HillArea.Map.TryArrival("Farm", out Vector2 h)) stops.Add(h);
        Vector2 p = player.transform.position;
        int current = 0;
        for (int i = 1; i < stops.Count; i++) if (Mathf.Abs(p.x - stops[i].x) < 20f) current = i;
        Vector2 to = stops[(current + 1) % stops.Count];
        player.transform.position = new Vector3(to.x, to.y, player.transform.position.z);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = to;
        HUD.Toast(stops.Count == 1 ? "[DEV] No other areas are open yet (F1 opens them)." : "[DEV] Teleported.", 2f);
    }
}
