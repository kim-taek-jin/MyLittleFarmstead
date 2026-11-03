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

    private float cellWorldSize = 1f;

    public void Bind(CropData cropData)
    {
        data = cropData;
        Refresh();
    }

    // 밭 한 칸의 월드 크기 (픽셀 아트를 칸 크기에 맞추는 데 사용)
    public void SetCellSize(float size)
    {
        cellWorldSize = Mathf.Max(0.01f, size);
    }

    public void Refresh()
    {
        if (data == null || spriteRenderer == null) return;

        // 작물별 픽셀 아트가 있으면 그것을 사용 (32px = 밭 한 칸)
        Sprite[] pixelStages = PixelArt.CropStages(data.cropId);
        bool usingPixelArt = pixelStages != null;
        Sprite[] stages = usingPixelArt ? pixelStages : growthSprites;

        if (stages != null && stages.Length > 0)
        {
            Sprite sprite = stages[data.GetVisualStage(stages.Length)];
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        if (usingPixelArt)
        {
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float s = cellWorldSize / (spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit);
            transform.localScale = new Vector3(s / Mathf.Max(0.0001f, parentScale.x), s / Mathf.Max(0.0001f, parentScale.y), 1f);
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 5);
        }

        // 픽셀 아트는 작물마다 그림이 달라서 색을 입히지 않고, 예전 그림일 때만 작물 색으로 구분
        Color color = (data.IsMature && !usingPixelArt) ? data.Def.tint : Color.white;
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
