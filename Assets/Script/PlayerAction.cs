using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class PlayerAction : MonoBehaviour
{
    public static PlayerAction instance;

    public Tilemap groundTilemap;
    public TileBase dirtTile;
    public TileBase tilledDirtTile;
    public TileBase tilledWetDirtTile;
    public GameObject sproutPrefab;
    public LayerMask cropLayer;

    private DataManager dataManager;
    private Hotbar hotbar;

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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject gridObject = GameObject.Find("Grid");
        if (gridObject != null)
        {
            groundTilemap = gridObject.GetComponentInChildren<Tilemap>();
        }
        else
        {
            groundTilemap = null;
        }

        if (SceneData.hasNextPlayerPosition)
        {
            this.transform.position = SceneData.nextPlayerPosition;
            SceneData.hasNextPlayerPosition = false;
        }
    }

    void Start()
    {
        dataManager = DataManager.instance;
        hotbar = FindObjectOfType<Hotbar>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null) return;

            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, cropLayer);

            if (hit.collider != null)
            {
                Crop crop = hit.collider.GetComponent<Crop>();
                if (crop != null)
                {
                    if (crop.IsHarvestable()) TryToHarvest(hit.collider.gameObject);
                    else TryToWater(crop);
                }
            }
            else
            {
                if (groundTilemap != null)
                {
                    Vector3Int cellPos = groundTilemap.WorldToCell(mouseWorldPos);
                    TileBase currentTile = groundTilemap.GetTile(cellPos);

                    if (currentTile == dirtTile) TryToTill(cellPos);
                    else if (currentTile == tilledDirtTile) TryToPlant(cellPos);
                }
            }
        }
    }

    void TryToWater(Crop crop)
    {
        string selectedItem = hotbar?.GetSelectedItemName();
        if (selectedItem != "Watering Can") return;
        if (crop.IsWatered()) return;
        if (dataManager.UseEnergy(3f))
        {
            crop.Water();
            groundTilemap.SetTile(groundTilemap.WorldToCell(crop.transform.position), tilledWetDirtTile);
        }
    }

    void TryToHarvest(GameObject cropObject)
    {
        string selectedItem = hotbar?.GetSelectedItemName();
        if (selectedItem != "Scythe") return;
        if (dataManager.UseEnergy(4f))
        {
            dataManager.AddItem("Carrot", 1);
            Destroy(cropObject);
        }
    }

    void TryToTill(Vector3Int cellPos)
    {
        string selectedItem = hotbar?.GetSelectedItemName();
        if (selectedItem != "Hoe") return;
        if (dataManager.UseEnergy(5f))
        {
            groundTilemap.SetTile(cellPos, tilledDirtTile);
        }
    }

    void TryToPlant(Vector3Int cellPos)
    {
        string selectedItem = hotbar?.GetSelectedItemName();
        if (string.IsNullOrEmpty(selectedItem) || !selectedItem.Contains("Seed")) return;

        Vector3 plantPosition = groundTilemap.GetCellCenterWorld(cellPos);
        if (Physics2D.OverlapCircle(plantPosition, 0.1f, cropLayer)) return;

        if (dataManager.UseEnergy(2f))
        {
            dataManager.RemoveItem(selectedItem, 1);
            Instantiate(sproutPrefab, plantPosition, Quaternion.identity);
        }
    }
}