using UnityEngine;
using UnityEngine.SceneManagement;

// W-1: 장터 마을회관 옆 공동 수돗가. 그림은 장터 배경에 그려져 있고 (Tools/pixelart/water.py, kmarket.py),
// 여기서는 발밑 충돌과 [E] 물 마시기만. 겨울엔 수도꼭지에 헌 천을 감아 둔 모습.
public class MarketTap : MonoBehaviour
{
    public static readonly Vector2 At = new Vector2(4.6f, -1.0f);   // 그림의 아래 가운데
    const float Reach = 1.15f;

    public static void Create(Scene scene)
    {
        var go = new GameObject("MarketTap");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(At.x, At.y, 0f);
        BoxCollider2D feet = go.AddComponent<BoxCollider2D>();     // 개수대와 고무 대야 발밑
        feet.size = new Vector2(1.3f, 0.32f);
        feet.offset = new Vector2(0.05f, 0.2f);
        go.AddComponent<MarketTap>();
    }

    void Update()
    {
        DataManager data = DataManager.instance;
        PlayerAction p = PlayerAction.instance;
        if (data == null || p == null || data.IsGameOver || TitleScreen.Blocking || PixelHUD.ViewOpen || PixelHUD.BagOpen) return;
        if (InteriorRooms.Inside) return;
        if (Vector2.Distance(p.transform.position, At + new Vector2(0f, 0.3f)) > Reach || !AreaKit.PressedE()) return;

        data.RefillThirst();
        int capacity = data.WaterCapacity;
        bool winter = data.CurrentSeason == Season.Winter;
        if (capacity > 0)
        {
            data.RefillWater();
            data.UpdateAllUI();
            HUD.Toast(winter
                ? Loc.T($"You unwrap the cloth, drink the icy tap water and fill your can. ({data.waterCharges}/{capacity})", $"수도꼭지에 감아 둔 천을 풀고 찬물을 마셨어요. 물뿌리개도 채웠어요. ({data.waterCharges}/{capacity})")
                : Loc.T($"You drink from the village tap and fill your can. ({data.waterCharges}/{capacity})", $"마을 수돗가에서 물을 마시고 물뿌리개를 채웠어요. ({data.waterCharges}/{capacity})"));
        }
        else
            HUD.Toast(winter ? Loc.T("You unwrap the cloth and drink the icy tap water.", "수도꼭지에 감아 둔 천을 풀고 찬물을 마셨어요.")
                             : Loc.T("You drink cool water from the village tap.", "마을 수돗가에서 시원한 물을 마셨어요."));
    }
}
