using UnityEngine;

// 우물: E키로 물을 마시고(갈증 회복), 물뿌리개가 있으면 가득 채웁니다.
public class Well : MonoBehaviour
{
    private bool isPlayerInRange;

    void Update()
    {
        if (!isPlayerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        DataManager data = DataManager.instance;
        if (data == null) return;

        data.RefillThirst();
        int capacity = data.WaterCapacity;
        if (capacity > 0)
        {
            data.RefillWater();
            data.UpdateAllUI();
            HUD.Toast($"You drink cool water and fill your watering can. ({data.waterCharges}/{capacity})");
        }
        else
        {
            HUD.Toast("You drink cool, fresh water.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            HUD.Toast("Press E to drink and refill your watering can.", 2.5f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}
