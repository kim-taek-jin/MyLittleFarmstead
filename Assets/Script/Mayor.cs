using UnityEngine;

// 박 이장님 (K-1). 장터 길 가운데 전봇대(확성기) 앞에 서 있는 마을 이장. 아침 마을 방송의 목소리.
// 지금은 말을 걸면 인사와 계절 이야기, 오늘 방송 다시 듣기. 이장님의 이야기(마을이 비어 가는 걱정)는 K-3에서.
public class Mayor : MonoBehaviour
{
    public static readonly Vector2 Spot = new Vector2(-3.9f, 0.9f);   // 장터: 방앗간과 철물점 사이, 확성기 전봇대 앞
    const string MetKey = "mayor_met";

    SpriteRenderer body;
    bool inRange;
    int line;

    public static Mayor Create(Material material, float characterScale)
    {
        var go = new GameObject("Mayor");
        go.transform.position = new Vector3(Spot.x, Spot.y, 0f);
        CircleCollider2D talk = go.AddComponent<CircleCollider2D>();
        talk.isTrigger = true;
        talk.radius = 1.0f;
        BoxCollider2D feet = go.AddComponent<BoxCollider2D>();
        feet.size = new Vector2(0.3f * characterScale, 0.14f * characterScale);
        feet.offset = new Vector2(0f, -0.41f * characterScale);
        var bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(go.transform, false);
        bodyGO.transform.localScale = Vector3.one * characterScale;
        SpriteRenderer sr = bodyGO.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 10;
        sr.sprite = PixelArt.Load("Villagers/mayor");
        Mayor m = go.AddComponent<Mayor>();
        m.body = sr;
        return m;
    }

    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) inRange = true; }
    void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) inRange = false; }

    void Update()
    {
        DataManager data = DataManager.instance;
        if (!inRange || data == null || data.IsGameOver || HUD.DialogueOpen || TitleScreen.Blocking) return;
        if (Time.frameCount - HUD.DialogueClosedFrame <= 1 || !Input.GetKeyDown(KeyCode.E)) return;
        data.NoteTalk();
        string name = VillageBroadcast.Speaker;
        if (data.story.Unlock(MetKey))
        {
            HUD.Say(name, new[]
            {
                Loc.T("Well now, you must be the one who moved into the empty house! I'm Park, the village head.",
                      "아이고, 빈집에 새로 이사 온 양반이구먼! 나는 이 마을 이장 박가요."),
                Loc.T("Not many young folks left around here. Everyone's gone to the city. ...It's good to see a new face.",
                      "여기 젊은 사람이 별로 없어. 다들 도시로 나가 버려서. ...새 얼굴 보니 좋네."),
                Loc.T("I make the announcements every morning on that loudspeaker. Keep an ear out, eh?",
                      "아침마다 저 확성기로 방송하니께, 귀 좀 기울여 줘요잉."),
            }, body.sprite);
            return;
        }
        string[] lines =
        {
            Loc.T("Eating well? You look thin. City folks always do.", "밥은 잘 챙겨 먹고? 얼굴이 반쪽이네. 도시 사람들은 다 그래."),
            SeasonLine(data.CurrentSeason),
            Loc.T("This village used to be full of children. Now it's quiet... but you're here now.", "이 마을에도 애들이 바글바글했는데. 지금은 조용하지... 그래도 이제 자네가 있으니께."),
            Loc.T("If you hear the loudspeaker crackle, that's me. Don't laugh at my voice.", "확성기 지지직 하면 나여. 목소리 갖고 웃지 말고."),
        };
        HUD.Say(name, new[] { lines[line % lines.Length] }, body.sprite);
        line++;
    }

    static string SeasonLine(Season s)
    {
        switch (s)
        {
            case Season.Spring: return Loc.T("Spring! Time to open the sauce jars and let the sun in.", "봄이여! 장독 뚜껑 열고 볕 좀 쬐어 줘야지.");
            case Season.Summer: return Loc.T("Hot, isn't it? Come sit in the pavilion when it gets too much.", "덥지? 너무 더우면 정자에 와서 앉아 있어.");
            case Season.Autumn: return Loc.T("Autumn's busy. Kimchi-making day is coming, everyone will need a hand.", "가을은 바빠. 곧 김장날인디, 다들 일손이 필요할 거여.");
            default: return Loc.T("Winter's when we make meju for the sauces. Grandma Boksun knows how.", "겨울엔 메주를 쑤는 거여. 복순 할매가 잘 알지.");
        }
    }
}
