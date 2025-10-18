using UnityEngine;
using System.Linq;

public class CropManager : MonoBehaviour
{
    public GameObject cropPrefab;

    void Start()
    {
        // 로딩 로직은 그대로 유지
        if (DataManager.instance.farmCropData.Any())
        {
            Crop[] existingCrops = FindObjectsOfType<Crop>();
            foreach (var crop in existingCrops)
            {
                if (crop != null) Destroy(crop.gameObject);
            }

            foreach (var data in DataManager.instance.farmCropData)
            {
                GameObject cropGO = Instantiate(cropPrefab, data.position, Quaternion.identity);
                cropGO.GetComponent<Crop>().LoadData(data);
            }
        }
    }

    // ✨ OnDisable을 삭제하고, '저장'만 담당하는 public 함수를 만듭니다.
    public void SaveCropData()
    {
        if (DataManager.instance == null) return;

        DataManager.instance.farmCropData.Clear();
        Crop[] allCrops = FindObjectsOfType<Crop>();

        foreach (var crop in allCrops)
        {
            DataManager.instance.farmCropData.Add(crop.GetData());
        }

        if (allCrops.Length > 0)
        {
            Debug.Log($"[CropManager] {allCrops.Length}개의 작물 데이터를 저장했습니다.");
        }
    }
}