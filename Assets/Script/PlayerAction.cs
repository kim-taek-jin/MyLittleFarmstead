using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

// 플레이어 입력 → 행동
//  - 왼쪽 클릭: 들고 있는 도구/씨앗/정수 사용 (다 자란 작물은 손으로 수확)
//  - 오른쪽 클릭 또는 F: 들고 있는 음식 먹기
public class PlayerAction : MonoBehaviour
{
    public static PlayerAction instance;

    public Tilemap groundTilemap;
    public TileBase dirtTile;
    public TileBase tilledDirtTile;
    public TileBase tilledWetDirtTile;
    public GameObject sproutPrefab;
    public LayerMask cropLayer;

    [Header("상호작용")]
    [Tooltip("플레이어로부터 이 거리(월드 단위) 안의 칸만 작업할 수 있습니다.")]
    public float reach = 3.5f;

    private DataManager dataManager;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject gridObject = GameObject.Find("Grid");
        groundTilemap = gridObject != null ? gridObject.GetComponentInChildren<Tilemap>() : null;

        if (SceneData.hasNextPlayerPosition)
        {
            this.transform.position = SceneData.nextPlayerPosition;
            SceneData.hasNextPlayerPosition = false;
        }
    }

    void Start()
    {
        dataManager = DataManager.instance;
    }

    void Update()
    {
        if (dataManager == null) dataManager = DataManager.instance;
        if (dataManager == null || dataManager.IsGameOver || Time.timeScale == 0f || PixelHUD.BagOpen || PixelHUD.ViewOpen) return;

        bool use = Input.GetMouseButtonDown(0);
        bool eat = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F);
        if (!use && !eat) return;
        if ((use || Input.GetMouseButtonDown(1)) && IsPointerOverUI()) return;

        string selected = Hotbar.SelectedItem;

        if (eat)
        {
            TryEat(selected);
            return;
        }
        UseOnWorld(selected);
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void TryEat(string selected)
    {
        if (string.IsNullOrEmpty(selected)) return;
        if (!ItemDB.IsEdible(selected))
        {
            HUD.Toast($"You can't eat {selected}.");
            return;
        }
        dataManager.Eat(selected);
    }

    void UseOnWorld(string selected)
    {
        CropManager farm = CropManager.Instance;
        if (farm == null || !farm.IsReady || StarHill.Active) return; // 밭이 있는 씬(Farm)에서만, 별의 언덕 위에서는 제외

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3Int cell = farm.WorldToCell(mouseWorld);

        Vector2 cellCenter = farm.GetCellCenter(cell);
        if (Vector2.Distance(transform.position, cellCenter) > reach)
        {
            HUD.Toast("That's too far away. Walk closer.", 1.5f);
            return;
        }

        ItemDef def = ItemDB.Get(selected);
        ToolType tool = def != null ? def.toolType : ToolType.None;

        // 숲 빈터의 묘목 자리 (도토리 심기, 물 주기)
        if (ForestArea.TryUse(cell, def)) return;
        // 노을 언덕의 과일나무 (가지치기, 대지의 숨결, 열매 따기)
        if (HillArea.TryUse(cell, def)) return;

        // 치울 것 (묵은 밭의 잡초/그루터기/바위, 숲의 통나무)이 있으면 먼저 치움
        Debris debris = Debris.At(cell);
        if (debris != null)
        {
            debris.Hit(def);
            return;
        }

        // 낫: 주변 3x3 수확
        if (tool == ToolType.Scythe)
        {
            farm.TryScythe(cell);
            return;
        }

        // 다 자란 작물은 무엇을 들고 있든 손으로 수확
        CropData crop = dataManager.farm.GetCrop(cell.x, cell.y);
        if (crop != null && crop.IsMature)
        {
            farm.TryHarvest(cell);
            return;
        }

        if (def == null) return;

        switch (def.category)
        {
            case ItemCategory.Tool:
                if (tool == ToolType.Hoe) farm.TryTill(cell, def.toolTier);
                else if (tool == ToolType.WateringCan) farm.TryWater(cell, def.toolTier);
                break;
            case ItemCategory.Seed:
                farm.TryPlant(cell, def.name);
                break;
            case ItemCategory.Special:
                farm.TryUseEssence(cell, def.name);
                break;
            case ItemCategory.Food:
            case ItemCategory.Crop:
                HUD.Toast("Right-click or press F to eat.", 2f);
                break;
        }
    }
}
