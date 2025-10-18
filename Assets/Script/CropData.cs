using UnityEngine;

[System.Serializable]
public class CropData
{
    public Vector3 position;
    public int growthStage;
    public bool isWatered;

    public CropData(Vector3 pos, int stage, bool watered)
    {
        position = pos;
        growthStage = stage;
        isWatered = watered;
    }
}