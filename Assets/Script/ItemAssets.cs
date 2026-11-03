using UnityEngine;
using System.Collections.Generic;

// 아이템 이름 → 스프라이트 연결.
// 전용 스프라이트가 없으면 비슷한 아이템 스프라이트를 빌려 쓰고,
// 그것도 없으면 아이템 색으로 만든 임시 아이콘을 자동 생성합니다.
public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }

    [System.Serializable]
    public class NamedSprite
    {
        public string itemName;
        public Sprite sprite;
    }

    [Header("픽셀 아트 (Tools/pixelart/generate.py)")]
    [Tooltip("체크하면 Resources/PixelArt의 통일된 픽셀 아트를 우선 사용합니다. 끄면 아래에 연결된 예전 그림을 사용합니다.")]
    public bool usePixelArt = true;

    private void Awake()
    {
        // 씬을 다시 불러올 때 생기는 중복 GameManager가 Instance를 덮어쓰지 않도록
        if (Instance != null && Instance != this) return;
        Instance = this;
        PixelArt.Enabled = usePixelArt;
    }

    // 아이템 이름으로 스프라이트를 찾을 수 있도록 미리 연결해두는 곳
    public Sprite carrotSeedSprite;
    public Sprite hoeSprite;
    public Sprite wateringCanSprite;
    public Sprite scytheSprite;
    public Sprite woodSprite;
    public Sprite stoneSprite;
    public Sprite carrotSprite;
    public Sprite strawberrySeedSprite;
    public Sprite strawberrySprite;
    public Sprite cornSeedSprite;
    public Sprite cornSprite;
    public Sprite potatoSeedSprite;
    public Sprite potatoSprite;

    [Header("추가 아이템 스프라이트 (이름으로 연결, 코드 수정 없이 추가 가능)")]
    public List<NamedSprite> extraSprites = new List<NamedSprite>();

    private static readonly Dictionary<string, Sprite> placeholderCache = new Dictionary<string, Sprite>();

    // ItemAssets가 없는 씬에서도 안전하게 쓸 수 있는 정적 함수
    public static Sprite SpriteFor(string itemName)
    {
        if (Instance != null) return Instance.GetSpriteByName(itemName);
        return FindPixelArt(ItemDB.Normalize(itemName)) ?? GetPlaceholder(itemName);
    }

    // 픽셀 아트 아이콘 (없으면 비슷한 아이템의 아이콘)
    static Sprite FindPixelArt(string itemName)
    {
        if (!PixelArt.Enabled || string.IsNullOrEmpty(itemName)) return null;
        Sprite sprite = PixelArt.Item(itemName);
        if (sprite != null) return sprite;
        ItemDef def = ItemDB.Get(itemName);
        return def != null && !string.IsNullOrEmpty(def.iconFallback) ? PixelArt.Item(def.iconFallback) : null;
    }

    // 아이템 이름을 주면 해당하는 스프라이트를 돌려주는 함수
    public Sprite GetSpriteByName(string itemName)
    {
        itemName = ItemDB.Normalize(itemName);
        if (string.IsNullOrEmpty(itemName)) return null;

        // 1) 인스펙터의 Extra Sprites (직접 지정한 그림이 최우선)
        foreach (var entry in extraSprites)
        {
            if (entry != null && entry.sprite != null && entry.itemName == itemName) return entry.sprite;
        }

        // 2) 통일된 픽셀 아트
        Sprite sprite = FindPixelArt(itemName);
        if (sprite != null) return sprite;

        // 3) 예전에 연결해 둔 그림
        sprite = FindAssigned(itemName);
        if (sprite != null) return sprite;

        // 비슷한 아이템 스프라이트 빌려 쓰기 (예: Fertile Hoe → Hoe)
        ItemDef def = ItemDB.Get(itemName);
        if (def != null && !string.IsNullOrEmpty(def.iconFallback))
        {
            sprite = FindAssigned(def.iconFallback);
            if (sprite != null) return sprite;
        }
        return GetPlaceholder(itemName);
    }

    Sprite FindAssigned(string itemName)
    {
        foreach (var entry in extraSprites)
        {
            if (entry != null && entry.sprite != null && entry.itemName == itemName) return entry.sprite;
        }

        switch (itemName)
        {
            case "Carrot Seed": return carrotSeedSprite;
            case "Hoe": return hoeSprite;
            case "Watering Can": return wateringCanSprite;
            case "Scythe": return scytheSprite;
            case "Wood": return woodSprite;
            case "Stone": return stoneSprite;
            case "Carrot": return carrotSprite;
            case "Strawberry Seed": return strawberrySeedSprite;
            case "Strawberry": return strawberrySprite;
            case "Corn Seed": return cornSeedSprite;
            case "Corn": return cornSprite;
            case "Potato Seed": return potatoSeedSprite;
            case "Potato": return potatoSprite;
            default: return null;
        }
    }

    // 아이템 이름을 주면 해당하는 '기본 판매 가격'을 돌려주는 함수 (시장 보정 전)
    public int GetSellPriceByName(string itemName)
    {
        ItemDef def = ItemDB.Get(itemName);
        return def != null ? Mathf.Max(1, def.sellPrice) : 1;
    }

    // 아이템 색으로 동그란 임시 아이콘을 만듭니다.
    static Sprite GetPlaceholder(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        if (placeholderCache.TryGetValue(itemName, out Sprite cached) && cached != null) return cached;

        ItemDef def = ItemDB.Get(itemName);
        Color fill = def != null ? def.color : Color.gray;
        Color edge = Color.Lerp(fill, Color.black, 0.45f);

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                Color c = d > 13f ? Color.clear : (d > 11f ? edge : fill);
                // 위쪽에 살짝 하이라이트
                if (d <= 11f && y > 18 && x < 16) c = Color.Lerp(c, Color.white, 0.25f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = "Placeholder_" + itemName;
        placeholderCache[itemName] = sprite;
        return sprite;
    }
}
