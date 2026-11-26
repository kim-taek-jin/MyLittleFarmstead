// 체험판(데모) 설정을 한곳에 (K-5).
//  - 체험판 켜기: Player Settings > Scripting Define Symbols에 DEMO_BUILD, 또는 GameManager의 Demo Mode 체크
//  - StoreUrl: 데모 끝 화면의 버튼이 여는 주소. 지금은 itch.io 페이지, 스팀 페이지가 생기면 스팀 주소로 바꾸기
public static class DemoConfig
{
    public const string Version = "0.5.0";

    // TODO: itch.io에 게임 페이지를 만들면 그 주소로 (예: https://아이디.itch.io/my-little-farmstead)
    public const string StoreUrl = "https://itch.io/";
    public const string StoreNameEn = "itch.io", StoreNameKo = "itch.io";
    public static string StoreName => Loc.T(StoreNameEn, StoreNameKo);

#if DEMO_BUILD
    public const bool Build = true;
#else
    public const bool Build = false;
#endif

    public static bool IsDemo => DataManager.instance != null ? DataManager.instance.demoMode : Build;
}
