using UnityEngine;

public class ResourceCollector : MonoBehaviour
{
    [Header("드랍 확률 (%)")]
    [Range(0f, 100f)]
    public float specialItemDropChance = 10f; // 10% 확률

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 나무의 태그는 "Wood"로 가정하고 진행합니다.
        if (other.CompareTag("Wood"))
        {
            if (DataManager.instance.UseEnergy(10f))
            {
                Destroy(other.gameObject);
                DataManager.instance.AddItem("Wood");

                if (Random.Range(0f, 100f) < specialItemDropChance)
                {
                    // ✨ 아이템 이름을 영어로 수정
                    DataManager.instance.AddItem("Sunshine Shard");
                    Debug.Log("✨ 반짝! Sunshine Shard를 발견했습니다!");
                }
            }
        }
        else if (other.CompareTag("Stone"))
        {
            if (DataManager.instance.UseEnergy(10f))
            {
                Destroy(other.gameObject);
                DataManager.instance.AddItem("Stone");

                if (Random.Range(0f, 100f) < specialItemDropChance)
                {
                    // ✨ 아이템 이름을 영어로 수정
                    DataManager.instance.AddItem("Earth's Breath");
                    Debug.Log("🍃 오오... Earth's Breath를 발견했습니다!");
                }
            }
        }
        else if (other.CompareTag("Food"))
        {
            Destroy(other.gameObject);
            DataManager.instance.RestoreHunger(30f);
        }
        else if (other.CompareTag("Bed"))
        {
            DataManager.instance.Sleep();
        }
    }
}