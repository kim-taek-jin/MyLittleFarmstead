using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 장 담그기 (K-2): 농장 마당의 세 곳. FarmMap이 농장 장면을 만들 때 놓습니다 (자리는 farm.json의 anchors).
//  - 아궁이 (집 왼쪽 벽, 부엌): 장작 1개로 불을 지피면 오늘 밤 방이 뜨끈함 + 그 앞에서 요리 (C) / 메주 쑤기
//  - 메주 걸이 (마당): 생메주를 걸고, 다 마른 메주를 걷음
//  - 장독대: 장 담그기 / 장 가르기 / 뚜껑 열고 닫기
// 규칙은 JangState (유니티 없이 테스트됨).
public static class JangSpots
{
    public static void Create(Scene scene, Material mat)
    {
        Make<Agungi>(scene, "Agungi", FarmMap.Anchor("agungi", new Vector2(1.0f, 0f)), mat, new Vector2(0.9f, 0.5f));
        Make<MejuRack>(scene, "MejuRack", FarmMap.Anchor("mejurack", new Vector2(-0.4f, 0f)), mat, new Vector2(0.9f, 0.25f));
        Make<JangdokPlatform>(scene, "Jangdok", FarmMap.Anchor("jangdok", new Vector2(7.0f, 0f)), mat, Vector2.zero);   // 장독대 자체는 이미 벽
    }

    static T Make<T>(Scene scene, string name, Vector2 bottom, Material mat, Vector2 feet) where T : JangSpot
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(bottom.x, bottom.y, 0f);
        if (feet != Vector2.zero)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = feet;
            box.offset = new Vector2(0f, feet.y * 0.5f);
        }
        T spot = go.AddComponent<T>();
        spot.bottom = bottom;
        spot.view = AreaKit.SpriteAt(go.transform, "Art", null, bottom, 6, mat);
        if (spot is Agungi)   // N-1: 불 땐 날 밤에 아궁이 불빛
            NightGlow.Add(go, spot.view, "fire", new Vector2(0f, 0.3f), 1.7f, 0.95f, NightLight.FireColor, 0.12f);
        spot.Refresh();
        return spot;
    }

    // 계절 그림 (겨울이면 눈 쌓인 것)을 아래 가운데에 맞춰 놓음
    public static void SetArt(SpriteRenderer sr, string key, Vector2 bottom)
    {
        if (key == null) { sr.enabled = false; return; }
        Sprite sprite = PixelArtWorld.SeasonalSprite(key);
        sr.enabled = sprite != null;
        if (sprite == null) return;
        sr.sprite = sprite;
        float s = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
        sr.transform.localScale = new Vector3(s, s, 1f);
        sr.transform.position = new Vector3(bottom.x, bottom.y + sprite.rect.height / sprite.pixelsPerUnit * s * 0.5f, 0f);
    }
}

public abstract class JangSpot : MonoBehaviour
{
    public Vector2 bottom;
    public SpriteRenderer view;
    protected const float Reach = 1.5f;
    protected static DataManager Data => DataManager.instance;
    protected static JangState Jang { get { JangState j = Data.story.jang; j.Ensure(); return j; } }
    protected static int Today => Data != null ? Data.Today : 0;

    public abstract void Refresh();
    protected abstract void Use();
    protected virtual Vector2 Center => bottom + new Vector2(0f, 0.4f);

    protected bool PlayerWithin(float reach)
    {
        PlayerAction p = PlayerAction.instance;
        return p != null && Vector2.Distance(p.transform.position, Center) < reach;
    }

    // 아궁이, 메주 걸이가 붙어 있어서: [E]는 가장 가까운 한 곳에만
    static readonly List<JangSpot> all = new List<JangSpot>();
    protected virtual void OnEnable() { all.Add(this); }
    protected virtual void OnDisable() { all.Remove(this); }
    protected virtual float ReachHere => Reach;

    float PlayerDistance()
    {
        PlayerAction p = PlayerAction.instance;
        return p == null ? float.MaxValue : Vector2.Distance(p.transform.position, Center);
    }

    bool IsNearest()
    {
        float mine = PlayerDistance();
        foreach (JangSpot s in all) if (s != this && s.PlayerDistance() < mine) return false;
        return true;
    }

    protected virtual void Update()
    {
        if (Data == null || Data.IsGameOver || TitleScreen.Blocking || PixelHUD.ViewOpen || PixelHUD.BagOpen) return;
        if (Time.frameCount % 30 == 0) Refresh();
        if (PlayerWithin(ReachHere) && AreaKit.PressedE() && IsNearest()) { Use(); Refresh(); }
    }

    // 처음 한 번만 하는 방법 안내
    protected static void HowTo(string key, string en, string ko)
    {
        if (Data.story.Unlock("howto_" + key)) HUD.Announce(Loc.T(en, ko), 8f);
    }
}

// ---------------------------------------------------------------- 아궁이
public class Agungi : JangSpot
{
    static Agungi current;
    // 아궁이 앞에 서 있으면 모닥불 없이도 요리, 메주 쑤기 (CraftingManager가 봄)
    public static bool PlayerNear => current != null && current.isActiveAndEnabled && current.PlayerWithin(Reach + 0.5f);

    void Awake() { current = this; }
    void OnDestroy() { if (current == this) current = null; }

    public override void Refresh() => JangSpots.SetArt(view, Jang.fireToday ? "agungi_fire" : "agungi", bottom);

    protected override void Use()
    {
        JangState j = Jang;
        if (!j.fireToday)
        {
            if (!Data.HasItem("Wood"))
            {
                HUD.Toast(Loc.T("You need a piece of firewood (Wood) to light the agungi.", "아궁이에 불을 지피려면 장작(나무)이 하나 필요해요."), 3f);
                return;
            }
            Data.RemoveItem("Wood", 1);
            j.fireToday = true;
            Sfx.Play("open", 0.7f);
            HUD.Toast(Loc.T("You light the agungi. The floor will be warm tonight. ([C] to cook here)", "아궁이에 불을 지폈어요. 오늘 밤은 방바닥이 뜨끈하겠어요. (여기서 [C]로 요리)"), 4f);
            HowTo("agungi",
                  "<size=34>The agungi</size>\n\nThe kitchen fire also heats the floor of the room (ondol).\nLight it once a day with a piece of firewood, and you'll sleep warm.\n\nStanding here, press <color=#ffd84a>[C]</color> to cook without a campfire,\nand to boil soybeans into <color=#ffd84a>meju</color> (4 Soybeans + 1 Wood).",
                  "<size=34>아궁이</size>\n\n부엌 아궁이 불은 방바닥(구들)까지 데워 줘요.\n하루에 한 번 장작 하나로 불을 지피면, 밤에 뜨끈하게 자요.\n\n여기 서서 <color=#ffd84a>[C]</color>를 누르면 모닥불 없이 요리할 수 있고,\n콩을 삶아 <color=#ffd84a>메주</color>도 쒀요 (메주콩 4 + 나무 1).");
            return;
        }
        // 이미 불을 땠으면: 요리 창
        CraftingManager cm = Object.FindFirstObjectByType<CraftingManager>();
        if (cm != null) cm.ToggleCraftingWindow();
    }
}

// ---------------------------------------------------------------- 메주 걸이
public class MejuRack : JangSpot
{
    protected override float ReachHere => 1.1f;   // 바로 옆 저장 상자와 헷갈리지 않게 가까이서만
    public override void Refresh()
    {
        JangState j = Jang;
        string key = j.hung.Count == 0 ? "meju_rack" : j.DriedCount(Today) > 0 ? "meju_rack_dried" : "meju_rack_fresh";
        JangSpots.SetArt(view, key, bottom);
    }

    protected override void Use()
    {
        JangState j = Jang;
        int ready = j.TakeDried(Today);
        if (ready > 0) Data.AddItem("Dried Meju", ready);
        int have = Data.GetCount("Fresh Meju");
        int hung = have > 0 ? j.Hang(have, Today) : 0;
        if (hung > 0) Data.RemoveItem("Fresh Meju", hung);

        if (ready > 0 && hung > 0) HUD.Toast(Loc.T($"You took down {ready} dried meju and hung {hung} more.", $"마른 메주 {ready}덩이를 걷고 생메주 {hung}덩이를 걸었어요."), 3f);
        else if (ready > 0) HUD.Toast(Loc.T($"You took down {ready} dried meju. It smells nutty.", $"마른 메주 {ready}덩이를 걷었어요. 구수한 냄새가 나요."), 3f);
        else if (hung > 0) HUD.Toast(Loc.T($"You hung {hung} meju to dry. They'll be ready in {JangState.DryDays} days.", $"메주 {hung}덩이를 걸었어요. {JangState.DryDays}일이면 말라요."), 3f);
        else if (j.hung.Count > 0) HUD.Toast(Loc.T($"The meju are still drying. ({j.hung.Count} hanging)", $"메주가 아직 마르는 중이에요. (걸린 메주 {j.hung.Count}덩이)"), 2.5f);
        else HUD.Toast(Loc.T("A rack for drying meju. Boil soybeans into meju at the agungi first.", "메주를 말리는 걸대. 먼저 아궁이에서 콩을 삶아 메주를 쒀요."), 3f);
        if (hung > 0)
            HowTo("meju",
                  "<size=34>Drying meju</size>\n\nMeju dries slowly in the cold winter air.\nIn a few days, take the dried meju down and put it in a sauce jar\nat the jangdokdae with <color=#ffd84a>2 Salt</color>.",
                  "<size=34>메주 말리기</size>\n\n메주는 찬 겨울바람에 천천히 말라요.\n며칠 뒤 마른 메주를 걷어, 장독대 빈 독에\n<color=#ffd84a>소금 2</color>와 함께 넣어요.");
    }
}

// ---------------------------------------------------------------- 장독대
public class JangdokPlatform : JangSpot
{
    protected override Vector2 Center => bottom + new Vector2(0f, -0.3f);   // 장독대 앞에 서서

    public override void Refresh() => JangSpots.SetArt(view, Jang.lidsOpen ? "jangdok_open" : null, bottom);

    protected override void Use()
    {
        JangState j = Jang;
        int ready = j.ReadyJar(Today);
        if (ready >= 0)
        {
            string taste = JangState.Taste(j.jars[ready]);
            j.Split(ready, out int doenjang, out int ganjang, Data.story.IsUnlocked("favor_jang") ? 1 : 0);   // 복순 할머니의 품
            Data.AddItem("Doenjang", doenjang);
            Data.AddItem("Ganjang", ganjang);
            Sfx.Play("harvest");
            Data.ChangeHeart(3f, Loc.T($"You split your first sauces: {doenjang} doenjang, {ganjang} ganjang. {taste}",
                                       $"장을 갈랐어요: 된장 {doenjang}, 간장 {ganjang}. {taste}"));
            return;
        }
        if (j.EmptyJar() >= 0 && Data.HasItem("Dried Meju") && Data.GetCount("Salt") >= JangState.SaltPerJar)
        {
            // 재료가 되는 만큼 빈 독을 한 번에 채움 (그래야 다음 [E]는 언제나 뚜껑 열고 닫기)
            int filled = 0;
            while (j.EmptyJar() >= 0 && Data.HasItem("Dried Meju") && Data.GetCount("Salt") >= JangState.SaltPerJar)
            {
                Data.RemoveItem("Dried Meju", 1);
                Data.RemoveItem("Salt", JangState.SaltPerJar);
                j.Start(Today);
                filled++;
            }
            Sfx.Play("water");
            HUD.Toast(filled == 1
                ? Loc.T($"You put the meju in a jar with salt water. In {JangState.FermentDays} days it can be split into sauces.",
                        $"메주를 소금물과 함께 독에 넣었어요. {JangState.FermentDays}일 뒤에 장을 가를 수 있어요.")
                : Loc.T($"You filled {filled} jars with meju and salt water. In {JangState.FermentDays} days they can be split into sauces.",
                        $"독 {filled}개에 메주와 소금물을 넣었어요. {JangState.FermentDays}일 뒤에 장을 가를 수 있어요."), 4f);
            HowTo("jangdok",
                  "<size=34>Looking after the jars</size>\n\nOn sunny days, open the lids and let the sun in: the sauce gets deeper.\nWhen it rains or snows, close them, or rainwater gets in.\n(Press <color=#ffd84a>[E]</color> at the jangdokdae to open or close all the lids.)\nForgetting isn't a disaster. It just tastes a little milder.",
                  "<size=34>장독 돌보기</size>\n\n맑은 날엔 뚜껑을 열어 볕을 쬐어 주면 맛이 깊어져요.\n비나 눈이 오면 덮어 줘요. 안 그러면 빗물이 들어가요.\n(장독대에서 <color=#ffd84a>[E]</color>: 뚜껑을 한꺼번에 열고 닫기)\n깜빡해도 망하지는 않아요. 조금 순한 맛이 될 뿐이에요.");
            return;
        }
        // 뚜껑 열고 닫기
        j.lidsOpen = !j.lidsOpen;
        Sfx.Play("click");
        int n = j.Fermenting();
        string state = n > 0 ? Loc.T($"  ({n} jar(s) fermenting)", $"  (익어 가는 독 {n}개)") : "";
        if (j.lidsOpen)
        {
            bool wet = Data.raining || Data.snowing;
            HUD.Toast((wet ? Loc.T("You opened the lids... in this weather? Rain will get in.", "뚜껑을 열었어요... 이 날씨에요? 빗물이 들어가요.")
                           : Loc.T("You opened the jar lids to let the sun in.", "장독 뚜껑을 열어 볕을 쬐어 줘요.")) + state, 3f);
        }
        else HUD.Toast(Loc.T("You closed the jar lids.", "장독 뚜껑을 덮었어요.") + state, 2.5f);
        if (n == 0)
            HowTo("jang",
                  "<size=34>The jangdokdae</size>\n\nThis is where the sauces of the house are made.\n1. Boil <color=#ffd84a>4 Soybeans</color> into meju at the agungi\n2. Hang it on the meju rack to dry for a few days\n3. Put <color=#ffd84a>1 Dried Meju + 2 Salt</color> in an empty jar here\n4. After a while, split it into doenjang and ganjang.\nSoybeans and salt are sold at the market.",
                  "<size=34>장독대</size>\n\n집의 장을 담그는 곳이에요.\n1. 아궁이에서 <color=#ffd84a>메주콩 4</color>로 메주 쑤기\n2. 메주 걸이에 걸어 며칠 말리기\n3. 여기 빈 독에 <color=#ffd84a>마른 메주 1 + 소금 2</color> 넣기\n4. 시간이 지나면 된장과 간장으로 가르기\n메주콩과 소금은 장터에서 팔아요.");
    }
}
