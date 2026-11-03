using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Farm 씬의 밭을 관리합니다. (Grid 오브젝트에 붙어 있음)
//  - DataManager.farm 데이터를 타일맵과 작물 오브젝트로 그려줍니다.
//  - 밭 갈기 / 물 주기 / 심기 / 수확 / 정수 사용 같은 농사 행동을 처리합니다.
// 데이터는 항상 DataManager에 있으므로 씬을 이동해도 밭이 그대로 유지됩니다.
public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    public GameObject cropPrefab;

    [Header("에너지 소모")]
    public float tillEnergy = 4f;
    public float waterEnergy = 2f;
    public float plantEnergy = 1f;
    public float scytheEnergy = 2f;

    private Tilemap tilemap;
    private readonly Dictionary<Vector3Int, Crop> visuals = new Dictionary<Vector3Int, Crop>();

    private DataManager Data => DataManager.instance;
    private FarmData Farm => DataManager.instance != null ? DataManager.instance.farm : null;
    private PlayerAction Player => PlayerAction.instance;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Data != null) Data.OnNewDay -= RefreshAll;
    }

    void Start()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        if (tilemap == null && Player != null) tilemap = Player.groundTilemap;

        // 씬에 미리 놓여 있던 작물은 지우고, 저장된 데이터 기준으로 다시 그립니다.
        foreach (Crop crop in FindObjectsByType<Crop>(FindObjectsSortMode.None)) Destroy(crop.gameObject);

        if (Data != null) Data.OnNewDay += RefreshAll;
        RefreshAll();
    }

    // 예전 코드 호환 (이제 데이터가 항상 DataManager에 있으므로 따로 저장할 필요 없음)
    public void SaveCropData() { }

    // ===================================================================
    // 그리기
    // ===================================================================
    public void RefreshAll()
    {
        FarmData farm = Farm;
        if (farm == null || tilemap == null) return;

        foreach (FarmTileData tile in farm.tilledTiles) ApplyTile(tile);

        // 데이터에 없는 작물 오브젝트 제거
        var toRemove = new List<Vector3Int>();
        foreach (var pair in visuals)
        {
            CropData data = farm.GetCrop(pair.Key.x, pair.Key.y);
            if (data == null || pair.Value == null)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                toRemove.Add(pair.Key);
            }
        }
        foreach (var key in toRemove) visuals.Remove(key);

        // 데이터에 있는 작물 오브젝트 생성/갱신
        foreach (CropData data in farm.crops) RefreshCell(new Vector3Int(data.cellX, data.cellY, 0));
    }

    void ApplyTile(FarmTileData tile)
    {
        if (Player == null || tilemap == null) return;
        TileBase target = tile.wet ? Player.tilledWetDirtTile : Player.tilledDirtTile;
        if (target != null) tilemap.SetTile(new Vector3Int(tile.cellX, tile.cellY, 0), target);
    }

    void RefreshCell(Vector3Int cell)
    {
        FarmData farm = Farm;
        if (farm == null) return;

        FarmTileData tile = farm.GetTile(cell.x, cell.y);
        if (tile != null) ApplyTile(tile);

        CropData data = farm.GetCrop(cell.x, cell.y);
        visuals.TryGetValue(cell, out Crop visual);

        if (data == null)
        {
            if (visual != null) Destroy(visual.gameObject);
            visuals.Remove(cell);
            return;
        }

        if (visual == null)
        {
            GameObject prefab = cropPrefab != null ? cropPrefab : (Player != null ? Player.sproutPrefab : null);
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, tilemap.GetCellCenterWorld(cell), Quaternion.identity);
            visual = go.GetComponent<Crop>();
            if (visual == null) visual = go.AddComponent<Crop>();
            visuals[cell] = visual;
        }
        visual.Bind(data);
    }

    // ===================================================================
    // 농사 행동 (PlayerAction에서 호출)
    // ===================================================================
    public TileBase GetTile(Vector3Int cell) => tilemap != null ? tilemap.GetTile(cell) : null;
    public Vector3 GetCellCenter(Vector3Int cell) => tilemap != null ? tilemap.GetCellCenterWorld(cell) : Vector3.zero;
    public Vector3Int WorldToCell(Vector3 world) => tilemap != null ? tilemap.WorldToCell(world) : Vector3Int.zero;
    public bool IsReady => tilemap != null && Farm != null && Player != null;

    // 씬에서 이미 '갈린 흙' 타일로 칠해진 칸은 데이터에도 등록해 둡니다.
    void EnsureTracked(Vector3Int cell)
    {
        if (Farm.IsTilled(cell.x, cell.y)) return;
        TileBase tile = GetTile(cell);
        if (tile != null && (tile == Player.tilledDirtTile || tile == Player.tilledWetDirtTile))
        {
            Farm.Till(cell.x, cell.y).wet = tile == Player.tilledWetDirtTile;
        }
    }

    static List<Vector3Int> GetLine(Vector3Int center, int tier)
    {
        var cells = new List<Vector3Int> { center };
        if (tier >= 2)
        {
            cells.Add(center + Vector3Int.left);
            cells.Add(center + Vector3Int.right);
        }
        return cells;
    }

    public bool TryTill(Vector3Int center, int tier)
    {
        if (!IsReady) return false;
        var targets = new List<Vector3Int>();
        foreach (var cell in GetLine(center, tier))
        {
            if (GetTile(cell) == Player.dirtTile && !Farm.IsTilled(cell.x, cell.y) && Farm.GetCrop(cell.x, cell.y) == null)
                targets.Add(cell);
        }

        if (targets.Count == 0)
        {
            EnsureTracked(center);
            if (Farm.IsTilled(center.x, center.y)) HUD.Toast("This soil is already tilled. Plant a seed here!");
            else HUD.Toast("You can only till dirt.");
            return false;
        }

        if (!Data.UseEnergy(tier >= 2 ? tillEnergy * 1.5f : tillEnergy)) return false;
        foreach (var cell in targets)
        {
            Farm.Till(cell.x, cell.y);
            RefreshCell(cell);
        }
        return true;
    }

    public bool TryWater(Vector3Int center, int tier)
    {
        if (!IsReady) return false;
        if (Data.waterCharges <= 0)
        {
            HUD.Toast("Your watering can is empty. Refill it at the well (E).");
            return false;
        }

        var targets = new List<Vector3Int>();
        foreach (var cell in GetLine(center, tier))
        {
            EnsureTracked(cell);
            FarmTileData tile = Farm.GetTile(cell.x, cell.y);
            if (tile == null) continue;
            CropData crop = Farm.GetCrop(cell.x, cell.y);
            if (!tile.wet || (crop != null && !crop.isWatered)) targets.Add(cell);
        }

        if (targets.Count == 0)
        {
            HUD.Toast(Farm.IsTilled(center.x, center.y) ? "This spot is already watered." : "Water tilled soil or crops.");
            return false;
        }

        if (!Data.UseEnergy(waterEnergy)) return false;
        Data.waterCharges--;
        foreach (var cell in targets)
        {
            Farm.Water(cell.x, cell.y);
            RefreshCell(cell);
        }
        if (Data.waterCharges == 0) HUD.Toast("The watering can is empty now.");
        Data.UpdateAllUI();
        return true;
    }

    public bool TryPlant(Vector3Int cell, string seedName)
    {
        if (!IsReady) return false;
        EnsureTracked(cell);

        if (!Farm.IsTilled(cell.x, cell.y))
        {
            HUD.Toast("Till the soil with a hoe first.");
            return false;
        }
        if (Farm.GetCrop(cell.x, cell.y) != null)
        {
            HUD.Toast("Something is already growing here.");
            return false;
        }

        CropDef def = CropDB.FromSeed(seedName);
        if (def == null || !Data.HasItem(seedName)) return false;
        if (!Data.UseEnergy(plantEnergy)) return false;

        Data.RemoveItem(seedName, 1);
        Farm.Plant(cell.x, cell.y, def.id);
        RefreshCell(cell);
        return true;
    }

    // 손으로 한 칸 수확 (에너지 소모 없음)
    public bool TryHarvest(Vector3Int cell)
    {
        if (!IsReady) return false;
        CropData crop = Farm.GetCrop(cell.x, cell.y);
        if (crop == null || !crop.IsMature) return false;
        HarvestCell(cell, crop);
        return true;
    }

    // 낫으로 주변 3x3 수확
    public bool TryScythe(Vector3Int center)
    {
        if (!IsReady) return false;
        var ripe = new List<Vector3Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var cell = new Vector3Int(center.x + dx, center.y + dy, 0);
                CropData crop = Farm.GetCrop(cell.x, cell.y);
                if (crop != null && crop.IsMature) ripe.Add(cell);
            }
        }

        if (ripe.Count == 0)
        {
            HUD.Toast("Nothing is ripe here yet.");
            return false;
        }
        if (!Data.UseEnergy(scytheEnergy)) return false;
        foreach (var cell in ripe) HarvestCell(cell, Farm.GetCrop(cell.x, cell.y));
        return true;
    }

    void HarvestCell(Vector3Int cell, CropData crop)
    {
        CropDef def = crop.Def;
        bool blessed = crop.blessed;
        int amount = Farm.Harvest(crop);
        if (amount <= 0) return;

        Data.AddItem(def.harvestItem, amount);
        HUD.Toast($"Harvested {def.harvestItem} x{amount}" + (blessed ? "  <color=#ffd84a>(golden!)</color>" : ""));
        Data.OnHarvest(def.harvestItem, amount, blessed);
        RefreshCell(cell);
    }

    // 자연의 정수 사용: Earth's Breath = 하루 성장, Sunshine Shard = 축복(수확량 +1, 마음 +)
    public bool TryUseEssence(Vector3Int cell, string itemName)
    {
        if (!IsReady) return false;
        CropData crop = Farm.GetCrop(cell.x, cell.y);
        if (crop == null)
        {
            HUD.Toast("Use it on a growing crop.");
            return false;
        }

        if (itemName == "Earth's Breath")
        {
            if (crop.IsMature)
            {
                HUD.Toast("This crop is already ripe.");
                return false;
            }
            crop.daysGrown++;
            HUD.Toast("The crop breathes in the earth's energy and grows!");
        }
        else if (itemName == "Sunshine Shard")
        {
            if (crop.blessed)
            {
                HUD.Toast("This crop is already glowing with sunlight.");
                return false;
            }
            crop.blessed = true;
            HUD.Toast("Warm sunlight wraps around the crop. It will ripen golden.");
        }
        else
        {
            return false;
        }

        Data.RemoveItem(itemName, 1);
        RefreshCell(cell);
        return true;
    }
}
