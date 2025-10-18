using UnityEngine;
using System.Collections.Generic;

public class ItemAssets : MonoBehaviour
{
    // ✨ 싱글턴 패턴: 이 게임에 단 하나의 ItemAssets만 존재하도록 함
    public static ItemAssets Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 아이템 이름으로 스프라이트를 찾을 수 있도록 미리 연결해두는 곳
    public Sprite carrotSeedSprite;
    public Sprite hoeSprite;
    public Sprite wateringCanSprite;
    public Sprite scytheSprite;
    public Sprite woodSprite; // Wood 스프라이트도 추가하면 좋습니다.
    public Sprite stoneSprite; // ✨ 1. Stone 스프라이트 변수 추가
    public Sprite carrotSprite;
    // ✨ 새로운 작물 스프라이트 변수 추가
    public Sprite strawberrySeedSprite;
    public Sprite strawberrySprite;
    public Sprite cornSeedSprite;
    public Sprite cornSprite;
    public Sprite potatoSeedSprite;
    public Sprite potatoSprite;

    // 아이템 이름을 주면 해당하는 스프라이트를 돌려주는 함수
    public Sprite GetSpriteByName(string itemName)
    {
        switch (itemName)
        {
            case "Carrot Seed": return carrotSeedSprite;
            case "Hoe": return hoeSprite;
            case "Watering Can": return wateringCanSprite;
            case "Scythe": return scytheSprite;
            case "Wood": return woodSprite;
            case "Stone": return stoneSprite; // ✨ 2. Stone 케이스 추가
            case "Carrot": return carrotSprite;
            // ✨ 새로운 작물 case 추가
            case "Strawberry Seed": return strawberrySeedSprite;
            case "Strawberry": return strawberrySprite;
            case "Corn Seed": return cornSeedSprite;
            case "Corn": return cornSprite;
            case "Potato Seed": return potatoSeedSprite;
            case "Potato": return potatoSprite;
            default:
                Debug.LogWarning("해당하는 아이템 스프라이트를 찾을 수 없음: " + itemName);
                return null;
        }
    }
    // ItemAssets.cs 스크립트 안에 추가

    // 아이템 이름을 주면 해당하는 '판매 가격'을 돌려주는 함수
    public int GetSellPriceByName(string itemName)
    {
        switch (itemName)
        {
            // ✨ 여기서 각 아이템의 판매 가격을 설정합니다.
            case "Carrot": return 25;
            case "Carrot Seed": return 5;
            case "Wood": return 2;
            case "Stone": return 5;
            case "Sunshine Shard": return 100;
            case "Earth's Breath": return 100;
            // ✨ 새로운 작물 판매 가격 추가
            case "Strawberry": return 120;
            case "Corn": return 50;
            case "Potato": return 80;

            // 목록에 없는 아이템은 기본값 1골드로 설정
            default:
                return 1;
        }
    }
}