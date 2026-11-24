using System;
using UnityEngine;

public enum Lang { En, Ko }

// 한국어 / 영어. 두 언어가 똑같이 기본입니다.
//  - 글은 코드 옆에 두 언어를 나란히 씁니다: Loc.T("Harvested!", "수확했어요!")
//  - 고른 언어는 PlayerPrefs("lang")에 저장되고, 처음에는 컴퓨터 언어(한국어면 한국어)를 따릅니다.
//  - 바꾸면 Changed가 불리고, 픽셀 글꼴도 그 언어에 맞는 것으로 바뀝니다 (영어: Pixelify Sans, 한국어: 갈무리).
public static class Loc
{
    const string PrefKey = "lang";
    private static bool loaded;
    private static Lang current;

    public static event Action Changed;

    public static Lang Current
    {
        get
        {
            if (!loaded) Load();
            return current;
        }
    }

    public static bool Ko => Current == Lang.Ko;

    // 지금 언어의 글. 한국어가 비어 있으면 영어를 씁니다.
    public static string T(string en, string ko) => Ko && !string.IsNullOrEmpty(ko) ? ko : en;

    // 언어 이름 (그 언어로)
    public static string Name(Lang lang) => lang == Lang.Ko ? "한국어" : "English";

    public static void Set(Lang lang)
    {
        if (!loaded) Load();
        if (lang == current) return;
        current = lang;
        PlayerPrefs.SetString(PrefKey, lang == Lang.Ko ? "ko" : "en");
        PlayerPrefs.Save();
        PixelFont.Refresh();
        Changed?.Invoke();
    }

    public static void Toggle() => Set(Current == Lang.Ko ? Lang.En : Lang.Ko);

    static void Load()
    {
        loaded = true;
        string saved = PlayerPrefs.GetString(PrefKey, "");
        if (saved == "ko") current = Lang.Ko;
        else if (saved == "en") current = Lang.En;
        else current = Application.systemLanguage == SystemLanguage.Korean ? Lang.Ko : Lang.En;
    }
}
