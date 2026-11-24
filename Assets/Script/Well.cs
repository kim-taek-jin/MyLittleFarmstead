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
            HUD.Toast(Loc.T($"You drink cool water and fill your watering can. ({data.waterCharges}/{capacity})", $"시원한 물을 마시고 물뿌리개를 채웠어요. ({data.waterCharges}/{capacity})"));
        }
        else
        {
            HUD.Toast(Loc.T("You drink cool, fresh water.", "시원하고 맑은 물을 마셨어요."));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            HUD.Toast(Loc.T("Press E to drink and refill your watering can.", "E를 눌러 물을 마시고 물뿌리개를 채워요."), 2.5f);
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
