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

    static AlbumDB()
    {
        // 겨울: 번아웃에서 첫 걸음
        Add(Season.Winter, "w_first_light", "First Light", "Harvest your first Carrot.", "harvest", "Carrot", "Items/carrot");
        Add(Season.Winter, "w_warm_bowl", "A Warm Bowl", "Eat something you cooked.", "eat_cooked", null, "Items/roasted_carrot");
        Add(Season.Winter, "w_early_night", "Early Night", "Go to bed before midnight.", "sleep_early", null, "World/bed");
        Add(Season.Winter, "w_gray_friend", "A Gray Friend", "Meet a little spirit.", "spirit_found", null, "Spirit/spirit_gray");
        Add(Season.Winter, "w_cold_water", "Cold Water", "Drink at the well.", "drink", null, "World/well_winter");
        Add(Season.Winter, "w_neighbor", "Hello, Neighbor", "Talk to a villager.", "villager_met", null, "Villagers/rosa_0");
        // 봄: 회복
        Add(Season.Spring, "s_strawberry", "Strawberry Season", "Harvest a Strawberry.", "harvest", "Strawberry", "Items/strawberry");
        Add(Season.Spring, "s_little_color", "A Little Color", "Grant a villager's wish.", "wish", null, "Villagers/mina_1");
        Add(Season.Spring, "s_rainy_day", "Rainy Day", "Wake up to spring rain.", "rain_morning", null, "Tiles/tilled_wet");
        Add(Season.Spring, "s_first_catch", "First Catch", "Catch a fish.", "fish", null, "Items/minnow");
        Add(Season.Spring, "s_sharing", "Sharing", "Share food with the village.", "share", null, "World/donation_box");
        Add(Season.Spring, "s_growing_up", "Growing Up", "Your spirit grows up a little.", "spirit_young", null, "Spirit/spirit_bloom_young");
        // 여름: 활력
        Add(Season.Summer, "u_sunflower", "Sunflower Smile", "Harvest a Sunflower.", "harvest", "Sunflower", "Items/sunflower");
        Add(Season.Summer, "u_corn", "Tall Corn", "Harvest Corn.", "harvest", "Corn", "Items/corn");
        Add(Season.Summer, "u_deal", "It's a Deal!", "Win a haggle with the merchant.", "haggle", null, "World/merchant");
        Add(Season.Summer, "u_big_catch", "The Big One", "Catch a Carp.", "fish", "Carp", "Items/carp");
        Add(Season.Summer, "u_starry", "Starry Night", "Visit Star Hill.", "starhill", null, "World/star_ladder");
        Add(Season.Summer, "u_savings", "Something Saved", "Have 1000G at once.", "gold1000", null, "World/storage_box");
        // 가을: 결실
        Add(Season.Autumn, "a_pumpkin", "Pumpkin Harvest", "Harvest a Pumpkin.", "harvest", "Pumpkin", "Items/pumpkin");
        Add(Season.Autumn, "a_golden", "Golden Harvest", "Harvest a crop blessed by sunlight.", "golden", null, "Items/sunshine_shard");
        Add(Season.Autumn, "a_koi", "Shining Koi", "Catch a Golden Koi.", "fish", "Golden Koi", "Items/golden_koi");
        Add(Season.Autumn, "a_restored", "Colors Restored", "Bring all the color back to a villager.", "restored", null, "Villagers/rosa_3");
        Add(Season.Autumn, "a_shooting_star", "Shooting Star", "Catch a Star Fragment.", "star", null, "Items/star_fragment");
        Add(Season.Autumn, "a_pie", "Pie for Two", "Bake a Pumpkin Pie.", "craft", "Pumpkin Pie", "Items/pumpkin_pie");
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
            HUD.Toast($"<color=#ffd84a>New photo!</color>  \"{p.title}\"  ({SeasonSystem.Name(p.season)} {have}/{AlbumDB.PerSeason})  [P] Album", 3.5f);

            if (have >= AlbumDB.PerSeason && story.Unlock("album_" + SeasonSystem.Key(p.season)))
            {
                data.AddGold(300);
                data.ChangeHeart(5f, null);
                HUD.Announce($"<size=40>{SeasonSystem.Name(p.season)} page complete!</size>\n\nEvery memory of this season is in color now.\n\n<color=#9dff9d>+300G  +5 Heart</color>", 5f);
            }
            if (story.photos.Count >= AlbumDB.All.Count && story.Unlock("album_all"))
            {
                HUD.Announce("<size=42>The whole album is in color</size>\n\nWinter, spring, summer and autumn.\nA year of your new life, one photo at a time.", 7f);
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
