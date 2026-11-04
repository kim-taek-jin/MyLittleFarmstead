using System.Collections.Generic;
using UnityEngine;

// 치울 수 있는 것들. 농장 묵은 밭의 잡초 / 그루터기 / 바위는 치우면 갈 수 있는 흙이 되고,
// 숲의 쓰러진 통나무는 치우면 길이 열립니다.
//  잡초:     낫이나 괭이로 한 번 (에너지 2)
//  그루터기: 곡괭이로 2번, 한 번에 에너지 4 → 나무 2개
//  바위:     곡괭이로 3번, 한 번에 에너지 4 → 돌 2개 (가끔 대지의 숨결)
//  통나무:   곡괭이로 3번, 한 번에 에너지 4 → 나무 3개
// 치운 칸은 FarmData.clearedDebris에 저장되어 다시 생기지 않습니다.
public class Debris : MonoBehaviour
{
    public const float WeedEnergy = 2f;
    public const float DigEnergy = 4f;

    public char kind;            // 'g' 잡초, 'k' 그루터기, 'o' 바위, 'l' 숲의 통나무
    public Vector3Int cell;
    public int hitsLeft;

    private static readonly Dictionary<Vector3Int, Debris> all = new Dictionary<Vector3Int, Debris>();
    private Transform view;
    private float punch;
    private float baseScale = 1f;

    public static Debris At(Vector3Int cell)
    {
        return all.TryGetValue(cell, out Debris d) && d != null ? d : null;
    }

    public static string SpriteKey(char kind, int cellX, int cellY)
    {
        switch (kind)
        {
            case 'k': return "stump";
            case 'o': return "boulder";
            case 'l': return "forest_log";
            default: return "weeds_" + (Mathf.Abs(cellX + cellY) % 2);
        }
    }

    public static string NameOf(char kind) => kind == 'k' ? "stump" : kind == 'o' ? "boulder" : kind == 'l' ? "log" : "weeds";

    // 몇 번 쳐야 치워지는지 (게임 규칙, 미리보기와 같음)
    public static int HitsFor(char kind)
    {
        if (kind == 'k') return 2;
        if (kind == 'o' || kind == 'l') return 3;
        return 1;
    }

    // 농장 묵은 밭의 것인지 (치우면 흙이 됨)
    public static bool IsFieldDebris(char kind) => kind == 'g' || kind == 'k' || kind == 'o';

    public static Debris Create(char kind, Vector3Int cell, Vector3 center, Material material)
    {
        var go = new GameObject("Debris_" + NameOf(kind));
        go.transform.position = new Vector3(center.x, center.y, 0f);
        Debris d = go.AddComponent<Debris>();
        d.kind = kind;
        d.cell = cell;
        d.hitsLeft = HitsFor(kind);

        string key = SpriteKey(kind, cell.x, cell.y);
        Sprite sprite = PixelArtWorld.SeasonalSprite(key);
        if (sprite != null)
        {
            var art = new GameObject("PixelArt");
            art.transform.SetParent(go.transform, false);
            // 배경과 같은 픽셀 크기(32px = 1유닛), 밑동을 칸의 아래쪽에 맞춤
            float scale = PixelArt.PixelsPerUnit / sprite.pixelsPerUnit;
            art.transform.localScale = new Vector3(scale, scale, 1f);
            art.transform.localPosition = new Vector3(0f, -0.47f + sprite.rect.height / sprite.pixelsPerUnit * scale * 0.5f, 0f);
            d.baseScale = scale;
            SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 4;   // 땅(0)과 작물 위, 플레이어(10) 아래
            art.AddComponent<SeasonalArt>().baseKey = key;
            d.view = art.transform;
        }

        // 그루터기와 바위는 지나갈 수 없음 (잡초는 밟고 지나감)
        if (kind != 'g')
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(kind == 'l' ? 0.95f : 0.8f, 0.45f);
            box.offset = new Vector2(0f, -0.22f);
        }
        all[cell] = d;
        return d;
    }

    void OnDestroy()
    {
        if (all.TryGetValue(cell, out Debris d) && d == this) all.Remove(cell);
    }

    void Update()
    {
        if (view == null || punch <= 0f) return;
        punch = Mathf.Max(0f, punch - Time.deltaTime * 6f);
        float s = baseScale * (1f + Mathf.Sin(punch * Mathf.PI) * 0.12f);
        view.localScale = new Vector3(s, s, 1f);
    }

    // 플레이어가 도구로 이 칸을 쳤을 때. 처리했으면 true (다른 농사 행동은 하지 않음)
    public bool Hit(ItemDef tool)
    {
        DataManager data = DataManager.instance;
        if (data == null) return true;
        ToolType type = tool != null ? tool.toolType : ToolType.None;

        if (kind == 'g')
        {
            if (type != ToolType.Scythe && type != ToolType.Hoe)
            {
                HUD.Toast("Weeds. Cut them with a scythe or dig them out with a hoe.", 2.5f);
                return true;
            }
            if (!data.UseEnergy(WeedEnergy)) return true;
            Clear();
            return true;
        }

        if (type != ToolType.Mattock)
        {
            string what = kind == 'k' ? "An old stump." : kind == 'o' ? "A heavy boulder." : "A fallen log blocks the way.";
            HUD.Toast(data.HasItem("Mattock") ? $"{what} Use the mattock." : $"{what} You need a mattock. (Market, or craft: 5 Wood + 5 Stone)", 3f);
            return true;
        }
        if (!data.UseEnergy(DigEnergy)) return true;
        hitsLeft--;
        punch = 1f;
        if (hitsLeft > 0)
        {
            HUD.Toast(kind == 'k' ? $"The roots creak... ({hitsLeft} more)" : kind == 'l' ? $"The log rolls a little... ({hitsLeft} more)" : $"The boulder shifts... ({hitsLeft} more)", 1.2f);
            return true;
        }

        if (kind == 'k')
        {
            data.AddItem("Wood", 2);
            HUD.Toast("You pulled out the old stump. +2 Wood", 2f);
        }
        else if (kind == 'l')
        {
            data.AddItem("Wood", 3);
            HUD.Toast("You cleared the fallen log. The path is open. +3 Wood", 2.5f);
        }
        else
        {
            data.AddItem("Stone", 2);
            float chance = 10f * (0.5f + data.heart / data.maxHeart);
            if (Random.Range(0f, 100f) < chance)
            {
                data.AddItem("Earth's Breath");
                HUD.Toast("<color=#ffe28a>Earth's Breath was sleeping under the boulder!</color>");
            }
            else HUD.Toast("You rolled the boulder away. +2 Stone", 2f);
        }
        Clear();
        return true;
    }

    void Clear()
    {
        DataManager data = DataManager.instance;
        data.farm.ClearDebris(cell.x, cell.y);
        if (IsFieldDebris(kind))
        {
            FarmMap.MakeSoil(cell);
            FarmMap.ReportCleared();
        }
        Destroy(gameObject);
    }
}
