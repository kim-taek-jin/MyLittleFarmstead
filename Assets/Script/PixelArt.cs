using System.Collections.Generic;
using UnityEngine;

// Tools/pixelart/generate.py로 만든 픽셀 아트(Assets/Resources/PixelArt)를 불러옵니다.
//   Items/<아이템키>.png   예) "Earth's Breath" → Items/earths_breath
//   Crops/<작물ID>_<단계>.png
//   World/<이름>.png, Player/player_<방향>_<프레임>.png
// GameManager > ItemAssets > Use Pixel Art 체크를 끄면 예전 그림을 사용합니다.
public static class PixelArt
{
    public static bool Enabled = true;

    public const float PixelsPerUnit = 32f;

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite[]> cropCache = new Dictionary<string, Sprite[]>();

    // "Copper Watering Can" → "copper_watering_can"
    public static string Key(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return "";
        return itemName.Trim().ToLowerInvariant().Replace("'", "").Replace(' ', '_');
    }

    public static Sprite Load(string path)
    {
        if (!Enabled || string.IsNullOrEmpty(path)) return null;
        if (spriteCache.TryGetValue(path, out Sprite cached)) return cached;
        Sprite sprite = Resources.Load<Sprite>("PixelArt/" + path);
        spriteCache[path] = sprite; // 없는 경우(null)도 캐시해서 반복 로드를 막습니다.
        return sprite;
    }

    public static Sprite Item(string itemName) => Load("Items/" + Key(itemName));

    // 작물 성장 단계 스프라이트 (crop_0, crop_1, ... 순서). 없으면 null
    public static Sprite[] CropStages(string cropId)
    {
        if (!Enabled || string.IsNullOrEmpty(cropId)) return null;
        if (cropCache.TryGetValue(cropId, out Sprite[] cached)) return cached;

        var list = new List<Sprite>();
        for (int i = 0; i < 16; i++)
        {
            Sprite s = Load($"Crops/{cropId}_{i}");
            if (s == null) break;
            list.Add(s);
        }
        Sprite[] result = list.Count > 0 ? list.ToArray() : null;
        cropCache[cropId] = result;
        return result;
    }

    // 원래 스프라이트를 숨기고, 같은 자리/비슷한 크기로 픽셀 아트를 자식 오브젝트에 그립니다.
    // (콜라이더와 스크립트는 원래 오브젝트에 그대로 남습니다)
    public static SpriteRenderer Overlay(SpriteRenderer original, Sprite sprite, float fill = 0.9f)
    {
        if (original == null || sprite == null) return null;

        Transform existing = original.transform.Find("PixelArt");
        if (existing != null) return existing.GetComponent<SpriteRenderer>();

        Transform t = original.transform;
        Vector3 lossy = t.lossyScale;
        Bounds bounds = original.bounds;
        Vector2 targetSize = bounds.size;
        Vector3 center = bounds.center;
        if (original.sprite == null || targetSize.y < 0.01f)
        {
            // 스프라이트가 비어 있는 경우(예: 깨진 참조) 오브젝트 크기 1x1 기준
            targetSize = new Vector2(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y));
            center = t.position;
        }

        float spriteW = sprite.rect.width / sprite.pixelsPerUnit;
        float spriteH = sprite.rect.height / sprite.pixelsPerUnit;
        // 원래 영역 안에 비율을 유지하며 맞춤
        float scale = Mathf.Min(targetSize.x / spriteW, targetSize.y / spriteH) * fill;
        if (scale <= 0f || float.IsNaN(scale)) scale = 1f;

        GameObject child = new GameObject("PixelArt");
        child.transform.SetParent(t, false);
        child.transform.position = new Vector3(center.x, center.y, t.position.z);
        child.transform.localScale = new Vector3(
            SafeDiv(scale, lossy.x),
            SafeDiv(scale, lossy.y),
            1f);

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = original.sharedMaterial;
        sr.sortingLayerID = original.sortingLayerID;
        sr.sortingOrder = original.sortingOrder;
        sr.flipX = original.flipX;

        original.enabled = false;
        return sr;
    }

    static float SafeDiv(float a, float b)
    {
        if (Mathf.Abs(b) < 0.0001f) return a;
        return a / b;
    }
}
