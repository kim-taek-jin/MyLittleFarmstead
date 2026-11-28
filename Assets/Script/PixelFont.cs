using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 픽셀 글꼴. TTF를 실행 중에 TextMeshPro 글꼴로 만들어 씁니다 (에디터에서 글꼴 에셋을 따로 만들 필요 없음).
//  - 영어: Pixelify Sans (SIL OFL 1.1, Assets/Resources/Fonts/OFL.txt)
//  - 한국어: 갈무리 11 (Galmuri11, SIL OFL 1.1, Assets/Resources/Fonts/Galmuri-OFL.txt). 영문 글자도 들어 있습니다.
//  - 영어 글꼴에는 갈무리를 대체 글꼴로 붙여서, 영어 모드에서도 한글이 깨지지 않습니다.
// 글꼴을 못 불러오면 null → 원래 글꼴을 그대로 씁니다.
public static class PixelFont
{
    private static TMP_FontAsset latin, hangul;
    private static bool triedLatin, triedHangul;

    static TMP_FontAsset Latin
    {
        get
        {
            if (!triedLatin)
            {
                triedLatin = true;
                Font font = Resources.Load<Font>("Fonts/PixelifySans-Regular");
                if (font != null) latin = TMP_FontAsset.CreateFontAsset(font);
                if (latin != null && Hangul != null && latin.fallbackFontAssetTable != null && !latin.fallbackFontAssetTable.Contains(Hangul))
                    latin.fallbackFontAssetTable.Add(Hangul);
            }
            return latin;
        }
    }

    static TMP_FontAsset Hangul
    {
        get
        {
            if (!triedHangul)
            {
                triedHangul = true;
                Font font = Resources.Load<Font>("Fonts/Galmuri11");
                // 한글은 글자 수가 많아서 아틀라스를 크게, 여러 장까지 쓸 수 있게 만듭니다.
                if (font != null)
                    hangul = TMP_FontAsset.CreateFontAsset(font, 36, 4, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            }
            return hangul;
        }
    }

    // 장면에 원래 있던 글자들(제작 창, 상점 창 등)은 TextMeshPro 기본 글꼴(LiberationSans)이라 한글이 □로 나왔음.
    // 시작할 때 갈무리를 TextMeshPro 전체의 대체 글꼴로 등록 → 어느 글꼴이든 없는 한글은 갈무리에서 가져옴.
    // 실행 중에 만든 글꼴이라 끝날 때 다시 빼 둠 (에디터에서 TMP Settings 파일에 빈 칸이 남지 않게).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterGlobalFallback()
    {
        try
        {
            TMP_FontAsset h = Hangul;
            if (h == null || TMP_Settings.instance == null) return;
            var list = TMP_Settings.fallbackFontAssets;
            if (list == null || list.Contains(h)) return;
            list.Add(h);
            Application.quitting += () =>
            {
                try { if (TMP_Settings.instance != null) TMP_Settings.fallbackFontAssets?.Remove(h); } catch { }
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PixelFont] Could not add the Korean fallback font: " + e.Message);
        }
    }

    // 지금 언어에 맞는 글꼴
    public static TMP_FontAsset Asset => Loc.Ko ? (Hangul ?? Latin) : (Latin ?? Hangul);

    public static void Apply(TMP_Text text)
    {
        if (text != null && Asset != null) text.font = Asset;
    }

    // 언어가 바뀌면: 이 글꼴을 쓰던 글자들을 모두 새 언어의 글꼴로
    public static void Refresh()
    {
        TMP_FontAsset target = Asset;
        if (target == null) return;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.font != null && (t.font == latin || t.font == hangul) && t.font != target) t.font = target;
        }
    }
}
