// 첫날 안내 (D-1). 키 설명을 한꺼번에 쏟는 대신, 첫날 할 일 세 가지를 하나씩 보여 줍니다.
//   1) 앞 텃밭을 괭이로 갈고 당근 씨앗 심기   2) 물 주기 (물이 떨어지면 우물)   3) 해가 지면 집에 들어가 자기
// 단계는 저장된 상태에서 계산합니다 (따로 저장할 것 없음). 새 게임에서만 켜집니다 (story "guide_on").
// 유니티에 의존하지 않는 규칙만 담았습니다 (화면: PixelHUD의 안내 카드, 월드의 금색 화살표).
public static class FirstDayGuide
{
    public const string OnKey = "guide_on";
    public const string DoneKey = "guide_done";
    public const int Steps = 3;

    // 지금 단계: -1 안내 없음, 0..2 할 일, 3 다 함 (한 번 축하하고 DoneKey를 남김)
    public static int Step(StoryState story, FarmData farm, int day)
    {
        if (story == null || farm == null || !story.IsUnlocked(OnKey) || story.IsUnlocked(DoneKey)) return -1;
        if (day >= 2) return 3;                                       // 첫날 밤을 잤음
        if (farm.crops.Count == 0) return 0;
        if (!farm.crops.Exists(c => c.isWatered)) return 1;
        return 2;
    }

    public static string Task(int step)
    {
        switch (step)
        {
            case 0: return Loc.T("Till the plot right of the house and plant carrot seeds", "집 오른쪽 텃밭을 괭이로 갈고 당근 씨앗을 심어요");
            case 1: return Loc.T("Water the seeds you planted", "심은 씨앗에 물을 줘요");
            case 2: return Loc.T("When it gets dark, go inside and sleep", "날이 저물면 집에 들어가 자요");
            default: return "";
        }
    }

    // 그 단계에 필요한 조작만 (처음 하는 사람 기준)
    public static string Hint(int step)
    {
        switch (step)
        {
            case 0: return Loc.T("[1] Hoe, left click on the plot  ·  then [4] Carrot Seed, left click", "[1] 괭이로 텃밭 왼쪽 클릭  ·  그다음 [4] 당근 씨앗 왼쪽 클릭");
            case 1: return Loc.T("[2] Watering Can, left click the seeds  ·  out of water? [E] at the well", "[2] 물뿌리개로 씨앗 왼쪽 클릭  ·  물이 떨어지면 우물 앞에서 [E]");
            case 2: return Loc.T("Through the house door, [E] at the bed (from 18:00)", "집 문으로 들어가 침대에서 [E] (18시부터)");
            default: return "";
        }
    }

    // 화살표가 가리킬 곳: 앞 텃밭(심기, 물 주기), 집 문
    public static string Target(int step) => step == 0 || step == 1 ? "plot" : step == 2 ? "door" : null;

    public static string Done() => Loc.T("Your first day went well. From here, live as you like.  [M] map  [J] journal",
                                         "첫날을 잘 보냈어요. 이제 마음 가는 대로 지내 봐요.  [M] 지도  [J] 일기");
}
