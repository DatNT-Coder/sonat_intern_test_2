using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 7;
    [SerializeField] private float cellSize = 1.6f;
    [SerializeField] private Transform gridOrigin;

    private Dictionary<Vector2Int, Block> _grid = new Dictionary<Vector2Int, Block>();

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public void SetCellSize(float newSize) { cellSize = newSize; }

    public void Resize(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        _grid.Clear();
        _gears.Clear();
        _rotatingGears.Clear(); // NEW
        Debug.Log($"[GridSystem] Resized to {width}x{height}");
    }

    /// <summary>Xóa sạch toàn bộ grid — dùng khi clear level</summary>
    public void ForceClean()
    {
        _grid.Clear();
        _gears.Clear();
        _rotatingGears.Clear(); // NEW
    }

    public void Init(LevelData levelData)
    {
        width = levelData.gridWidth;
        height = levelData.gridHeight;
        cellSize = levelData.cellSize;
        _grid.Clear();
    }

    public void RegisterBlock(Block block)
    {
        foreach (var cell in GetOccupiedCells(block.GridPosition, block.Size))
            _grid[cell] = block;
    }

    public void UnregisterBlock(Block block)
    {
        foreach (var cell in GetOccupiedCells(block.GridPosition, block.Size))
            if (_grid.ContainsKey(cell) && _grid[cell] == block)
                _grid.Remove(cell);
    }

    /// <summary>
    /// Kiểm tra block có thể trượt ra ngoài không (không bị chặn bởi bất kỳ block nào)
    /// </summary>
    public bool CanSlide(Block block)
    {
        return GetBlocker(block) == null;
    }

    /// <summary>
    /// Trả về block đầu tiên chặn đường trượt, hoặc null nếu đường thông
    /// </summary>
    public Block GetBlocker(Block block)
    {
        Vector2Int dir = DirectionToGrid(block.Direction);
        var occupied = GetOccupiedCells(block.GridPosition, block.Size);
        var frontEdge = GetFrontEdge(occupied, dir);

        // Tìm block chặn gần nhất
        Block blocker = null;
        int minDist = int.MaxValue;

        // Duyệt toàn bộ các ô theo hướng trượt cho đến khi ra ngoài grid
        foreach (var edgeCell in frontEdge)
        {
            Vector2Int check = edgeCell + dir;
            int dist = 1;

            while (IsInBounds(check))
            {
                if (_grid.ContainsKey(check))
                {
                    Block candidate = _grid[check];
                    // Bỏ qua nếu candidate đã bị destroy
                    if (candidate == null || candidate.gameObject == null)
                    {
                        _grid.Remove(check);
                        check += dir;
                        dist++;
                        continue;
                    }
                    if (candidate != block)
                    {
                        if (dist < minDist)
                        {
                            minDist = dist;
                            blocker = candidate;
                        }
                        break;
                    }
                }
                check += dir;
                dist++;
            }
        }

        return blocker; // null = đường thông → thoát ra ngoài
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 origin = gridOrigin != null ? gridOrigin.position : Vector3.zero;
        return origin + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 origin = gridOrigin != null ? gridOrigin.position : Vector3.zero;
        Vector3 local = worldPos - origin;
        return new Vector2Int(
            Mathf.RoundToInt(local.x / cellSize),
            Mathf.RoundToInt(local.y / cellSize)
        );
    }

    private bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public List<Vector2Int> GetOccupiedCells(Vector2Int origin, Vector2Int size)
    {
        var cells = new List<Vector2Int>();
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                cells.Add(origin + new Vector2Int(x, y));
        return cells;
    }

    private List<Vector2Int> GetFrontEdge(List<Vector2Int> cells, Vector2Int dir)
    {
        var edge = new List<Vector2Int>();
        foreach (var cell in cells)
            if (!cells.Contains(cell + dir))
                edge.Add(cell);
        return edge;
    }

    public static Vector2Int DirectionToGrid(SlideDirection dir)
    {
        return dir switch
        {
            SlideDirection.Up => Vector2Int.up,
            SlideDirection.Down => Vector2Int.down,
            SlideDirection.Left => Vector2Int.left,
            SlideDirection.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }

    // ─── Gear Block tracking ─────────────────────────────────────────────

    private Dictionary<Vector2Int, GearBlock> _gears = new Dictionary<Vector2Int, GearBlock>();

    public void RegisterGear(GearBlock gear)
    {
        _gears[gear.GridPosition] = gear;
    }

    public void UnregisterGear(GearBlock gear)
    {
        _gears.Remove(gear.GridPosition);
    }

    /// <summary>
    /// Trả về GearBlock đầu tiên nằm trên đường trượt của block (nếu có)
    /// </summary>
    public GearBlock GetGearInPath(Block block)
    {
        Vector2Int dir = DirectionToGrid(block.Direction);
        var cells = GetOccupiedCells(block.GridPosition, block.Size);
        var frontEdge = GetFrontEdge(cells, dir);

        GearBlock closest = null;
        int minDist = int.MaxValue;

        foreach (var edge in frontEdge)
        {
            Vector2Int check = edge + dir;
            int dist = 1;
            while (IsInBounds(check))
            {
                if (_gears.TryGetValue(check, out var gear))
                {
                    if (dist < minDist) { minDist = dist; closest = gear; }
                    break;
                }
                // Nếu có block bình thường chặn trước gear thì gear không tính
                if (_grid.ContainsKey(check)) break;
                check += dir;
                dist++;
            }
        }
        return closest;
    }

    // ─── Rotating Gear tracking (NEW) ────────────────────────────────────

    private Dictionary<Vector2Int, RotatingGear> _rotatingGears = new Dictionary<Vector2Int, RotatingGear>();

    public void RegisterRotatingGear(RotatingGear gear)
    {
        if (_rotatingGears.ContainsKey(gear.GridPosition))
        {
            Debug.LogWarning($"[GridSystem] Gear already registered at {gear.GridPosition}");
            return;
        }

        _rotatingGears.Add(gear.GridPosition, gear);
        Debug.Log($"[GridSystem] Registered rotating gear at {gear.GridPosition}");
    }

    public void UnregisterRotatingGear(RotatingGear gear)
    {
        _rotatingGears.Remove(gear.GridPosition);
    }

    public RotatingGear GetRotatingGearAt(Vector2Int pos)
    {
        _rotatingGears.TryGetValue(pos, out var gear);
        return gear;
    }

    /// <summary>
    /// Check if there's a rotating gear in the slide path (blocks cannot pass through)
    /// </summary>
    public bool HasRotatingGearInPath(Block block)
    {
        Vector2Int dir = DirectionToGrid(block.Direction);
        var cells = GetOccupiedCells(block.GridPosition, block.Size);
        var frontEdge = GetFrontEdge(cells, dir);

        foreach (var edge in frontEdge)
        {
            Vector2Int check = edge + dir;
            while (IsInBounds(check))
            {
                if (_rotatingGears.ContainsKey(check))
                    return true; // Bị chặn bởi rotating gear!

                // Nếu có block thì dừng check (block sẽ chặn trước)
                if (_grid.ContainsKey(check))
                    break;

                check += dir;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────

    public Block GetBlockAt(Vector2Int cell)
    {
        _grid.TryGetValue(cell, out var block);
        return block;
    }

    public void RemoveAdjacentBlocks(Vector2Int center)
    {
        Vector2Int[] neighbors = {
            center + Vector2Int.up, center + Vector2Int.down,
            center + Vector2Int.left, center + Vector2Int.right
        };
        foreach (var n in neighbors)
            if (_grid.TryGetValue(n, out var block) && block != null)
                block.TrySlide();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.15f);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = GridToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.95f);
            }
    }
}