using UnityEngine;

// 밭에 심어진 작물 한 개의 저장 데이터 (씬을 이동하거나 저장/불러오기 해도 유지됨)
[System.Serializable]
public class CropData
{
    public int cellX;
    public int cellY;
    public string cropId = "carrot";
    public int daysGrown;      // 물을 받고 지난 날 수
    public bool isWatered;     // 오늘 물을 받았는지
    public bool blessed;       // Sunshine Shard 축복 (수확량 +1, 마음 +)

    // 예전 버전 호환용 필드 (사용하지 않음)
    public Vector3 position;
    public int growthStage;

    public CropData() { }

    public CropData(int x, int y, string id)
    {
        cellX = x;
        cellY = y;
        cropId = id;
    }

    public CropDef Def => CropDB.Get(cropId);
    public bool IsMature => daysGrown >= Def.daysToMature;

    // 성장 단계(스프라이트 인덱스) 계산: 마지막 스프라이트는 '다 자람' 전용
    public int GetVisualStage(int spriteCount)
    {
        if (spriteCount <= 1) return 0;
        if (IsMature) return spriteCount - 1;
        int growingStages = spriteCount - 1;
        int stage = Mathf.FloorToInt((float)daysGrown * growingStages / Mathf.Max(1, Def.daysToMature));
        return Mathf.Clamp(stage, 0, growingStages - 1);
    }
}
