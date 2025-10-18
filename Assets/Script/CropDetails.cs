using UnityEngine;

// 이 줄이 있어야 Create 메뉴에 항목이 생깁니다.
[CreateAssetMenu(fileName = "New Crop Details", menuName = "Game Data/Crop Details")]
public class CropDetails : ScriptableObject
{
    [Header("작물 정보")]
    public string cropName; // 작물 이름 (예: Carrot)

    [Header("성장 정보")]
    public Sprite[] growthSprites; // 성장 단계별 스프라이트 배열
    public int daysToGrow;         // 다 자라는 데 걸리는 총 일수

    [Header("수확 정보")]
    public string harvestItemName; // 수확했을 때 나올 아이템의 이름
}