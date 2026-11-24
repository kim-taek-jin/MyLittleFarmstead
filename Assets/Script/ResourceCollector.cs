using UnityEngine;

// 플레이어에 붙어 있음. 나무/돌/음식에 닿으면 채집합니다.
// 채집한 자원은 그날 하루 동안 사라졌다가 다음 날 다시 자랍니다.
public class ResourceCollector : MonoBehaviour
{
    [Header("드랍 확률 (%)")]
    [Range(0f, 100f)]
    [Tooltip("자연의 정수가 나올 기본 확률. 마음(Heart)이 높을수록 더 자주 발견합니다.")]
    public float specialItemDropChance = 10f;

    [Header("에너지 소모")]
    public float gatherEnergy = 8f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        DataManager data = DataManager.instance;
        if (data == null || data.IsGameOver) return;

        if (other.CompareTag("Wood"))
        {
            Gather(other.gameObject, "Wood", "Sunshine Shard", Loc.T("A warm Sunshine Shard was hidden in the branches!", "나뭇가지 사이에 따뜻한 햇살 조각이 숨어 있었어요!"));
        }
        else if (other.CompareTag("Stone"))
        {
            Gather(other.gameObject, "Stone", "Earth's Breath", Loc.T("You found Earth's Breath beneath the stone!", "돌 밑에서 대지의 숨결을 찾았어요!"));
        }
        else if (other.CompareTag("Food"))
        {
            data.MarkResourceCleared(other.gameObject);
            Destroy(other.gameObject);
            data.AddItem("Apple", 1);
            HUD.Toast(Loc.T("Picked up an Apple. (Right-click / F to eat)", "사과를 주웠어요. (오른쪽 클릭 / F로 먹기)"), 2.5f);
        }
    }

    void Gather(GameObject target, string itemName, string specialItem, string specialMessage)
    {
        DataManager data = DataManager.instance;
        if (!data.UseEnergy(gatherEnergy)) return;

        data.MarkResourceCleared(target);
        Destroy(target);
        data.AddItem(itemName);

        // 마음이 열려 있을수록 자연의 아름다움을 더 잘 발견합니다. (기본 확률의 50% ~ 150%)
        float chance = specialItemDropChance * (0.5f + data.heart / data.maxHeart);
        if (Random.Range(0f, 100f) < chance)
        {
            data.AddItem(specialItem);
            HUD.Toast($"<color=#ffe28a>{specialMessage}</color>");
        }
    }
}
