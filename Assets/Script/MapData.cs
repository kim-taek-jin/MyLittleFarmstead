using System;
using System.Collections.Generic;
using UnityEngine;

// 맵 데이터 (Assets/Resources/Maps/<이름>.json, Tools/maps/make_farm_map.py로 만듦)
//  한 글자 = 한 칸(1유닛 = 32px). 열 c → 셀 x = originX + c, 행 r → 셀 y = originY - 1 - r (0행이 맨 위)
//   .  잔디   =  길   x  빈 흙(갈 수 있음)   g  잡초   k  그루터기   o  바위
//   T  나무(자원)   r  돌(자원)   a  사과   *  꽃(그림만)   ~  예전 농장 영역(그림이 그대로)
[Serializable]
public class MapExit
{
    public string to;
    public float x0, y0, x1, y1;
    public float spawnX, spawnY;
    public string locked;   // 이 출구를 여는 마음의 계절 (비어 있으면 열림)
    public string sign;     // 잠겨 있을 때 보여줄 말

    public Rect Area => Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(y0, y1), Mathf.Max(x0, x1), Mathf.Max(y0, y1));
    public bool IsLocked => !string.IsNullOrEmpty(locked);
}

[Serializable]
public class MapSign
{
    public float x, y;
    public string text;
}

[Serializable]
public class MapArrival
{
    public string from;
    public float x, y;
}

[Serializable]
public class MapData
{
    public string name;
    public int originX, originY, width, height;
    public string[] rows;
    public MapExit[] exits;
    public MapSign[] signs;
    public MapArrival[] arrivals;

    private static readonly Dictionary<string, MapData> cache = new Dictionary<string, MapData>();

    public static MapData Load(string mapName)
    {
        if (cache.TryGetValue(mapName, out MapData cached)) return cached;
        TextAsset text = Resources.Load<TextAsset>("Maps/" + mapName);
        MapData map = text != null ? Parse(text.text) : null;
        cache[mapName] = map;
        return map;
    }

    public static MapData Parse(string json)
    {
        MapData map = JsonUtility.FromJson<MapData>(json);
        if (map == null || map.rows == null || map.rows.Length == 0) return null;
        return map;
    }

    // 맵 전체가 차지하는 월드 영역
    public Rect Bounds => new Rect(originX, originY - height, width, height);
    public Vector2 Center => Bounds.center;

    public char At(int cellX, int cellY)
    {
        int c = cellX - originX;
        int r = originY - 1 - cellY;
        if (r < 0 || r >= rows.Length || c < 0 || c >= rows[r].Length) return ' ';
        return rows[r][c];
    }

    public Vector2Int CellOf(int column, int row) => new Vector2Int(originX + column, originY - 1 - row);

    // 모든 칸을 (셀 좌표, 글자)로 돌기
    public IEnumerable<KeyValuePair<Vector2Int, char>> Cells()
    {
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                yield return new KeyValuePair<Vector2Int, char>(CellOf(c, r), rows[r][c]);
    }

    public static bool IsDebris(char ch) => ch == 'g' || ch == 'k' || ch == 'o';
    public static bool IsSoil(char ch) => ch == 'x' || IsDebris(ch);

    public bool TryArrival(string fromScene, out Vector2 position)
    {
        if (arrivals != null)
            foreach (MapArrival a in arrivals)
                if (a.from == fromScene)
                {
                    position = new Vector2(a.x, a.y);
                    return true;
                }
        position = Vector2.zero;
        return false;
    }

    public int CountDebris()
    {
        int n = 0;
        foreach (var cell in Cells()) if (IsDebris(cell.Value)) n++;
        return n;
    }
}
