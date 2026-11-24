using UnityEngine;
using UnityEngine.SceneManagement;

// 장날(오일장): 5일마다 장터에 노점이 서고, 흥정 할아버지가 나옵니다. (MarketState.IsMarketDay)
//  - 노점 그림(market_fair)은 장터 배경과 같은 크기라 배경 위에 그대로 겹칩니다.
//  - 흥정 할아버지 앞에서 E: 흥정 미니게임(하루 한 번). 끝나면 장터로 돌아옵니다.
public class MarketFair : MonoBehaviour
{
    static readonly Vector2 BackgroundCenter = new Vector2(0.09f, -0.08f);
    static readonly Vector2 HagglerAt = new Vector2(2.6f, -2.2f);
    // 노점 자리 (밑면 가운데 x, y, 반폭): 지나가지 못하게
    static readonly Vector3[] Stalls = { new Vector3(-1.6f, -2.7f, 1.0f), new Vector3(2.6f, -3.0f, 1.3f), new Vector3(6.4f, -2.6f, 1.1f) };
    const float Reach = 1.3f;

    public static void Create(Scene scene, Material mat)
    {
        DataManager data = DataManager.instance;
        if (data == null || !MarketState.IsMarketDay(data.Today)) return;

        var root = new GameObject("MarketFair");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<MarketFair>();

        Sprite fair = PixelArt.Load("World/market_fair");
        if (fair != null)
        {
            var art = new GameObject("Stalls");
            art.transform.SetParent(root.transform, false);
            art.transform.position = new Vector3(BackgroundCenter.x, BackgroundCenter.y, 0f);
            float s = PixelArt.PixelsPerUnit / fair.pixelsPerUnit;
            art.transform.localScale = new Vector3(s, s, 1f);
            SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = fair;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = 1;
        }
        foreach (Vector3 st in Stalls)
        {
            BoxCollider2D box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(st.z * 2f, 0.5f);
            box.offset = new Vector2(st.x, st.y + 0.3f);
        }

        var man = new GameObject("Haggler");
        man.transform.SetParent(root.transform, false);
        man.transform.position = new Vector3(HagglerAt.x, HagglerAt.y, 0f);
        SpriteRenderer hs = man.AddComponent<SpriteRenderer>();
        hs.sprite = PixelArt.Load("World/haggler");
        if (mat != null) hs.sharedMaterial = mat;
        hs.sortingOrder = 2;
        if (hs.sprite != null)
        {
            float s = PixelArt.PixelsPerUnit / hs.sprite.pixelsPerUnit;
            man.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    void Start()
    {
        HUD.Toast(Loc.T("It's market day! Stalls line the lane, and the old farmer by the green tarp is up for some haggling.",
                        "오늘은 장날! 장터에 노점이 섰고, 초록 천막의 할아버지와 흥정할 수 있어요."), 4f);
    }

    void Update()
    {
        if (!AreaKit.PressedE() || Time.timeScale == 0f) return;
        PlayerAction player = PlayerAction.instance;
        DataManager data = DataManager.instance;
        if (player == null || data == null || data.IsGameOver || Villager.InRange != null) return;
        if (Vector2.Distance(player.transform.position, HagglerAt) > Reach) return;

        if (data.market.haggledToday)
        {
            HUD.Toast(Loc.T("\"We've struck our deal for today. Come back next market day!\"", "\"오늘 흥정은 끝났네. 다음 장날에 또 오게!\""), 2.5f);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded("MiniGame")) return;
        HaggleGame.ReturnScene = "Market";
        HaggleGame.ReturnPosition = new Vector2(HagglerAt.x, HagglerAt.y - 1.1f);
        SceneManager.LoadScene("MiniGame");
    }
}
