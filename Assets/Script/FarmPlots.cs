using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 텃밭 뙈기의 겉모습: 굳은 묵정밭(회색 껍질), 쉰 땅(짚을 덮은 빈 이랑).
// 이랑과 고랑 자체는 농장 바닥 그림(bg_farmmap)에 그려져 있고, 갈린 흙/젖은 흙은 타일맵이 그립니다.
public class FarmPlots : MonoBehaviour
{
    static FarmPlots instance;
    private Material material;
    private readonly List<GameObject> marks = new List<GameObject>();

    public static void Create(Scene scene, Material mat)
    {
        if (FarmMap.PlotList == null) return;
        var go = new GameObject("FarmPlots");
        SceneManager.MoveGameObjectToScene(go, scene);
        FarmPlots view = go.AddComponent<FarmPlots>();
        view.material = mat;
    }

    void Awake() { instance = this; }

    void Start()
    {
        if (DataManager.instance != null) DataManager.instance.OnNewDay += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (DataManager.instance != null) DataManager.instance.OnNewDay -= Refresh;
    }

    // 밭에 무언가를 한 뒤 (심기, 수확, 되살리기)
    public static void RefreshAll()
    {
        if (instance != null) instance.Refresh();
    }

    void Refresh()
    {
        foreach (GameObject m in marks) if (m != null) Destroy(m);
        marks.Clear();
        DataManager data = DataManager.instance;
        MapPlot[] plots = FarmMap.PlotList;
        if (data == null || plots == null) return;

        foreach (MapPlot plot in plots)
        {
            bool open = Plots.IsOpen(data.farm, plot);
            bool rested = open && Plots.IsRested(data.farm, plot);
            if (open && !rested) continue;
            for (int x = plot.x0; x <= plot.x1; x++)
            {
                for (int y = plot.y0; y <= plot.y1; y++)
                {
                    if (rested && data.farm.GetCrop(x, y) != null) continue;
                    string key = open ? StrawKey(x, y) : HardKey(x, y);
                    marks.Add(Mark(key, x, y));
                }
            }
        }
    }

    // 칸마다 조금씩 다른 그림 (같은 무늬가 반복되지 않게)
    static string HardKey(int x, int y) => "plot_hard_" + (((x * 7 + y * 13) % 3 + 3) % 3);
    static string StrawKey(int x, int y) => "plot_straw_" + (((x * 5 + y * 11) % 3 + 3) % 3);

    GameObject Mark(string key, int x, int y)
    {
        var go = new GameObject(key);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArtWorld.SeasonalSprite(key);
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 2;   // 갈린 흙 타일(0) 위, 작물(5) 아래
        if (sr.sprite != null)
        {
            float s = PixelArt.PixelsPerUnit / sr.sprite.pixelsPerUnit;
            go.transform.localScale = new Vector3(s, s, 1f);
        }
        return go;
    }
}

// 퇴비 더미: 작물, 열매, 나물 같은 걸 넣으면 두 개마다 다음 날 아침 퇴비 한 포대.
//  E: 다 된 퇴비가 있으면 꺼내고, 없으면 들고 있는 것을 하나 넣음 (Shift+E: 들고 있는 것 전부)
public class CompostHeap : MonoBehaviour
{
    const float Reach = 1.4f;

    public static void Create(Scene scene, Vector2 at, Material mat)
    {
        var go = new GameObject("CompostHeap");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(at.x, at.y, 0f);
        go.AddComponent<CompostHeap>();
        AreaKit.SpriteAt(go.transform, "Art", "compost_heap", new Vector2(at.x, at.y - 0.5f), 6, mat);
    }

    void Update()
    {
        if (!AreaKit.PressedE()) return;
        PlayerAction player = PlayerAction.instance;
        DataManager data = DataManager.instance;
        if (player == null || data == null || Time.timeScale == 0f) return;
        if (Vector2.Distance(player.transform.position, transform.position) > Reach) return;
        Use(data);
    }

    static void Use(DataManager data)
    {
        FarmData farm = data.farm;
        if (farm.compostReady > 0)
        {
            int n = Plots.TakeCompost(farm);
            data.AddItem("Compost", n);
            HUD.Toast(Loc.T($"You dig out {n} bag(s) of dark, crumbly compost.", $"까맣게 잘 삭은 퇴비 {n}포대를 꺼냈어요."), 3f);
            return;
        }

        string held = Hotbar.SelectedItem;
        if (Plots.IsOrganic(held))
        {
            bool all = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            int count = all ? data.GetCount(held) : 1;
            int put = Plots.AddToHeap(farm, held, count);
            if (put > 0)
            {
                data.RemoveItem(held, put);
                Sfx.Play("plant", 0.9f);
                HUD.Toast(Loc.T($"Into the heap: {ItemNames.Display(held)} x{put}. Every two become a bag of compost by morning. (In the heap: {farm.compostPoints})",
                                $"더미에 넣었어요: {ItemNames.Display(held)} x{put}. 두 개마다 내일 아침 퇴비 한 포대가 돼요. (더미 속 {farm.compostPoints}개)"), 3.5f);
            }
            return;
        }

        HUD.Toast(farm.compostPoints > 0
            ? Loc.T($"The heap is warm and quietly working. ({farm.compostPoints} inside, ready by morning)",
                    $"더미가 따뜻하게 삭는 중이에요. (안에 {farm.compostPoints}개, 내일 아침이면 퇴비가 돼요)")
            : Loc.T("A compost heap. Hold a crop, fruit, greens or food and press E to add it. (Shift+E: all of it)",
                    "퇴비 더미예요. 작물, 열매, 나물, 음식을 들고 E를 누르면 넣어요. (Shift+E: 전부)"), 3.5f);
    }
}
