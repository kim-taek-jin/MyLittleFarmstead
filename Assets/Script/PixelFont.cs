using TMPro;
using UnityEngine;

// 픽셀 글꼴 (Pixelify Sans, SIL OFL 1.1 — Assets/Resources/Fonts/OFL.txt).
// TTF를 실행 중에 TextMeshPro 글꼴로 만들어 씁니다 (에디터에서 글꼴 에셋을 따로 만들 필요 없음).
// 글꼴을 못 불러오면 null → 원래 글꼴을 그대로 씁니다.
public static class PixelFont
{
    private static TMP_FontAsset asset;
    private static bool tried;

    public static TMP_FontAsset Asset
    {
        get
        {
            if (tried) return asset;
            tried = true;
            Font font = Resources.Load<Font>("Fonts/PixelifySans-Regular");
            if (font != null) asset = TMP_FontAsset.CreateFontAsset(font);
            return asset;
        }
    }

    public static void Apply(TMP_Text text)
    {
        if (text != null && Asset != null) text.font = Asset;
    }
}
