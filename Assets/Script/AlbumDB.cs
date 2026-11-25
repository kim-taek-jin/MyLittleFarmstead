using System.Collections.Generic;

// 계절 앨범의 사진 한 장
public class PhotoDef
{
    public string id;
    public Season season;
    public string title;
    public string hint;       // 아직 못 찍었을 때 보여줄 힌트
    public string evt;        // 이 이벤트가 일어나면 찍힘 (Album.Report)
    public string arg;        // 이벤트 인자 (null이면 아무거나)
    public string sprite;     // Resources/PixelArt 기준 그림 경로
}

// 계절마다 6장, 총 24장. 그 계절이 됐거나 지나간 뒤에만 찍을 수 있습니다 (놓쳐도 나중에 찍을 수 있음).
public static class AlbumDB
{
    public static readonly List<PhotoDef> All = new List<PhotoDef>();
    public const int PerSeason = 6;

    static void Add(Season s, string id, string title, string hint, string evt, string arg, string sprite)
    {
        All.Add(new PhotoDef { id = id, season = s, title = title, hint = hint, evt = evt, arg = arg, sprite = sprite });
    }

    static string L(string en, string ko) => Loc.T(en, ko);

    static AlbumDB()
    {
        Build();
        Loc.Changed += () => { All.Clear(); Build(); };
    }

    static void Build()
    {
        // 겨울: 번아웃에서 첫 걸음
        Add(Season.Winter, "w_first_light", L("First Light", "첫 햇살"), L("Harvest your first Carrot.", "처음으로 당근을 거둬요."), "harvest", "Carrot", "Items/carrot");
        Add(Season.Winter, "w_warm_bowl", L("A Warm Bowl", "따뜻한 한 그릇"), L("Eat something you cooked.", "직접 만든 음식을 먹어요."), "eat_cooked", null, "Items/roasted_carrot");
        Add(Season.Winter, "w_early_night", L("Early Night", "일찍 잠들기"), L("Go to bed before midnight.", "자정 전에 잠자리에 들어요."), "sleep_early", null, "World/bed");
        Add(Season.Winter, "w_gray_friend", L("A Gray Friend", "잿빛 친구"), L("Meet a little dokkaebi fire.", "작은 도깨비불을 만나요."), "spirit_found", null, "Spirit/spirit_gray");
        Add(Season.Winter, "w_cold_water", L("Cold Water", "시원한 물"), L("Drink at the well.", "우물물을 마셔요."), "drink", null, "World/well_winter");
        Add(Season.Winter, "w_neighbor", L("Hello, Neighbor", "안녕하세요, 이웃님"), L("Talk to a villager.", "마을 사람과 이야기해요."), "villager_met", null, "Villagers/rosa_0");
        // 봄: 회복
        Add(Season.Spring, "s_strawberry", L("Strawberry Season", "딸기 철"), L("Harvest a Strawberry.", "딸기를 거둬요."), "harvest", "Strawberry", "Items/strawberry");
        Add(Season.Spring, "s_little_color", L("A Little Color", "조그만 빛깔"), L("Grant a villager's wish.", "마을 사람의 소원을 들어줘요."), "wish", null, "Villagers/mina_1");
        Add(Season.Spring, "s_rainy_day", L("Rainy Day", "비 오는 날"), L("Wake up to spring rain.", "봄비 소리에 잠을 깨요."), "rain_morning", null, "Tiles/tilled_wet");
        Add(Season.Spring, "s_first_catch", L("First Catch", "첫 손맛"), L("Catch a fish.", "물고기를 낚아요."), "fish", null, "Items/minnow");
        Add(Season.Spring, "s_sharing", L("Sharing", "나눔"), L("Share food with the village.", "마을과 음식을 나눠요."), "share", null, "World/donation_box");
        Add(Season.Spring, "s_growing_up", L("Growing Up", "쑥쑥"), L("The dokkaebi fire burns a little bigger.", "도깨비불이 조금 커져요."), "spirit_young", null, "Spirit/spirit_bloom_young");
        // 여름: 활력
        Add(Season.Summer, "u_sunflower", L("Sunflower Smile", "해바라기 웃음"), L("Harvest a Sunflower.", "해바라기를 거둬요."), "harvest", "Sunflower", "Items/sunflower");
        Add(Season.Summer, "u_corn", L("Tall Corn", "키 큰 옥수수"), L("Harvest Corn.", "옥수수를 거둬요."), "harvest", "Corn", "Items/corn");
        Add(Season.Summer, "u_deal", L("It's a Deal!", "좋아요, 그 값에!"), L("Win a haggle on market day.", "장날에 흥정에 이겨요."), "haggle", null, "World/haggler");
        Add(Season.Summer, "u_big_catch", L("The Big One", "월척"), L("Catch a Carp.", "잉어를 낚아요."), "fish", "Carp", "Items/carp");
        Add(Season.Summer, "u_starry", L("Starry Night", "별 헤는 밤"), L("Visit Star Hill.", "별 언덕에 올라요."), "starhill", null, "World/star_ladder");
        Add(Season.Summer, "u_savings", L("Something Saved", "모아 둔 돈"), L("Have 1000G at once.", "한 번에 1000G를 모아요."), "gold1000", null, "World/storage_box");
        // 가을: 결실
        Add(Season.Autumn, "a_pumpkin", L("Pumpkin Harvest", "호박 거두기"), L("Harvest a Pumpkin.", "호박을 거둬요."), "harvest", "Pumpkin", "Items/pumpkin");
        Add(Season.Autumn, "a_golden", L("Golden Harvest", "황금 수확"), L("Harvest a crop blessed by sunlight.", "햇살 조각을 받은 작물을 거둬요."), "golden", null, "Items/sunshine_shard");
        Add(Season.Autumn, "a_koi", L("Shining Koi", "반짝이는 잉어"), L("Catch a Golden Koi.", "황금 잉어를 낚아요."), "fish", "Golden Koi", "Items/golden_koi");
        Add(Season.Autumn, "a_restored", L("Colors Restored", "되찾은 빛깔"), L("Bring all the color back to a villager.", "마을 사람 한 명의 빛깔을 모두 되찾아요."), "restored", null, "Villagers/rosa_3");
        Add(Season.Autumn, "a_shooting_star", L("Shooting Star", "별똥별"), L("Catch a Star Fragment.", "별 조각을 받아요."), "star", null, "Items/star_fragment");
        Add(Season.Autumn, "a_pie", L("Porridge for Two", "둘이 먹는 호박죽"), L("Cook Pumpkin Porridge.", "호박죽을 쒀요."), "craft", "Pumpkin Porridge", "Items/pumpkin_porridge");
    }

    public static List<PhotoDef> ForSeason(Season s) => All.FindAll(p => p.season == s);

    // 이벤트 → 새로 찍을 수 있는 사진들 (순수 로직: 테스트용)
    public static List<PhotoDef> Matches(string evt, string arg, Season current, ICollection<string> taken)
    {
        var result = new List<PhotoDef>();
        foreach (PhotoDef p in All)
        {
            if (p.evt != evt || p.season > current || taken.Contains(p.id)) continue;
            if (p.arg != null && p.arg != arg) continue;
            result.Add(p);
        }
        return result;
    }
}

// 게임 곳곳에서 Album.Report("harvest", "Carrot") 처럼 호출합니다.
public static class Album
{
    public static void Report(string evt, string arg = null)
    {
        DataManager data = DataManager.instance;
        if (data == null) return;
        StoryState story = data.story;
        Season current = (Season)story.season;

        foreach (PhotoDef p in AlbumDB.Matches(evt, arg, current, story.photos))
        {
            story.photos.Add(p.id);
            int have = Count(story, p.season);
            HUD.Toast("<color=#ffd84a>" + Loc.T("New photo!", "새 사진!") + $"</color>  \"{p.title}\"  ({SeasonSystem.Name(p.season)} {have}/{AlbumDB.PerSeason})  [P] " + Loc.T("Album", "앨범"), 3.5f);

            if (have >= AlbumDB.PerSeason && story.Unlock("album_" + SeasonSystem.Key(p.season)))
            {
                data.AddGold(300);
                data.ChangeHeart(5f, null);
                HUD.Announce(Loc.T($"<size=40>{SeasonSystem.Name(p.season)} page complete!</size>\n\nEvery memory of this season is in color now.\n\n<color=#9dff9d>+300G  +5 Heart</color>",
                                   $"<size=40>{SeasonSystem.Name(p.season)} 쪽을 다 채웠어요!</size>\n\n이 계절의 추억이 모두 빛깔을 찾았어요.\n\n<color=#9dff9d>+300G  마음 +5</color>"), 5f);
            }
            if (story.photos.Count >= AlbumDB.All.Count && story.Unlock("album_all"))
            {
                HUD.Announce(Loc.T("<size=42>The whole album is in color</size>\n\nWinter, spring, summer and autumn.\nA year of your new life, one photo at a time.",
                                   "<size=42>앨범 전체가 빛깔을 찾았어요</size>\n\n겨울, 봄, 여름, 그리고 가을.\n새 삶의 한 해가 사진 한 장 한 장에 담겼어요."), 7f);
            }
        }
    }

    public static int Count(StoryState story, Season s)
    {
        int n = 0;
        foreach (PhotoDef p in AlbumDB.All) if (p.season == s && story.photos.Contains(p.id)) n++;
        return n;
    }
}
