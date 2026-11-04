using UnityEngine;

// 아이템 정보를 담을 간단한 클래스
[System.Serializable]
public class ItemInfo
{
    public string itemName;
    public int count;

    public ItemInfo() { }
    public ItemInfo(string name, int amount) { itemName = name; count = amount; }
}

// [CreateAssetMenu]를 사용하면 Unity 에디터에서 이 파일을 직접 만들 수 있게 됩니다.
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    public ItemInfo[] requiredItems; // 필요한 재료 목록
    public ItemInfo resultItem;      // 결과물 아이템

    [Tooltip("비어있지 않으면, 이 아이템을 가지고 있어야 제작 가능 (소모되지 않음). 예: Campfire")]
    public string requiredStation = "";

    [Tooltip("비어있지 않으면, 이 해금 키를 얻어야 제작 목록에 나타납니다. 예: recipe_carrot_cake")]
    public string requiredUnlock = "";
}
