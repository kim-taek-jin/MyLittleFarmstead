using UnityEngine;

// 아이템 정보를 담을 간단한 클래스
[System.Serializable]
public class ItemInfo
{
    public string itemName;
    public int count;
}

// [CreateAssetMenu]를 사용하면 Unity 에디터에서 이 파일을 직접 만들 수 있게 됩니다.
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    public ItemInfo[] requiredItems; // 필요한 재료 목록
    public ItemInfo resultItem;      // 결과물 아이템
}