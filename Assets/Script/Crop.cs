using UnityEngine;

public class Crop : MonoBehaviour
{
    [Header("성장 단계별 스프라이트")]
    public Sprite[] growthSprites;

    [Header("성장 관련 설정")]
    public float timeToGrow = 15f;

    private int currentGrowthStage = 0;
    private float growthTimer = 0f;
    private bool isWatered = false;
    private bool isHarvestable = false;
    private SpriteRenderer spriteRenderer;


    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        UpdateSprite();
    }

    void Update()
    {
        if (isWatered && !isHarvestable)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= timeToGrow)
            {
                Grow();
            }
        }
    }

    void Grow()
    {
        currentGrowthStage++;
        growthTimer = 0f;
        isWatered = false;

        if (currentGrowthStage >= growthSprites.Length - 1)
        {
            isHarvestable = true;
        }
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = growthSprites[currentGrowthStage];
        }
    }

    public void Water()
    {
        if (isWatered || isHarvestable) return;
        isWatered = true;
    }

    public bool IsWatered()
    {
        return isWatered;
    }

    public bool IsHarvestable()
    {
        return isHarvestable;
    }
    // Crop.cs의 GetData() 함수

    public CropData GetData()
    {
        // ✨ cropDetails.cropName을 사용하지 않는 간단한 방식으로 변경
        return new CropData(transform.position, currentGrowthStage, isWatered);
    }

    public void LoadData(CropData data)
    {
        // ✨ cropDetails 관련 로직 삭제
        transform.position = data.position;
        currentGrowthStage = data.growthStage;
        isWatered = data.isWatered;

        if (currentGrowthStage >= growthSprites.Length - 1)
        {
            isHarvestable = true;
        }

        UpdateSprite();
    }
}