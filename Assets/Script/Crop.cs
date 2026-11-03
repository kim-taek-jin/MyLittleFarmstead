using UnityEngine;

// 밭에 보이는 작물 오브젝트. 실제 성장 데이터는 DataManager.farm(CropData)에 있고,
// 이 컴포넌트는 데이터에 맞춰 모습만 그려줍니다. (성장은 '날짜' 기준: 물 준 날 수)
public class Crop : MonoBehaviour
{
    [Header("성장 단계별 스프라이트 (마지막 = 다 자람)")]
    public Sprite[] growthSprites;

    [Header("모습")]
    [Tooltip("물을 받지 못한 작물은 이만큼 어둡게 보입니다.")]
    [Range(0.5f, 1f)] public float dryBrightness = 0.75f;
    public Color blessedColor = new Color(1f, 0.85f, 0.25f);

    private SpriteRenderer spriteRenderer;
    private CropData data;

    public CropData Data => data;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Bind(CropData cropData)
    {
        data = cropData;
        Refresh();
    }

    public void Refresh()
    {
        if (data == null || spriteRenderer == null) return;

        if (growthSprites != null && growthSprites.Length > 0)
        {
            Sprite sprite = growthSprites[data.GetVisualStage(growthSprites.Length)];
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        Color color = data.IsMature ? data.Def.tint : Color.white;
        if (data.blessed) color = Color.Lerp(color, blessedColor, 0.6f);
        if (!data.isWatered && !data.IsMature)
        {
            color = new Color(color.r * dryBrightness, color.g * dryBrightness, color.b * dryBrightness, 1f);
        }
        spriteRenderer.color = color;
    }

    void Update()
    {
        // 축복받은(Sunshine Shard) 작물은 은은하게 반짝입니다.
        if (data != null && data.blessed && spriteRenderer != null)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f);
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(Mathf.Clamp01(blessedColor.r * pulse + 0.1f), Mathf.Clamp01(blessedColor.g * pulse), Mathf.Clamp01(blessedColor.b * pulse), c.a);
        }
    }

    // --- 예전 코드 호환용 ---
    public bool IsWatered() => data != null && data.isWatered;
    public bool IsHarvestable() => data != null && data.IsMature;
    public CropData GetData() => data;
    public void LoadData(CropData cropData) => Bind(cropData);
}
