using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelGenerator", menuName = "TapAway/Level Generator")]
public class LevelGenerator : ScriptableObject
{
    [Header("Base Grid Size")]
    [SerializeField] private int baseWidth = 4;
    [SerializeField] private int baseHeight = 5;

    [Header("Block Colors (4 màu từ assets)")]
    [SerializeField]
    private Color[] levelColors = new Color[]
    {
        new Color(0.204f, 0.596f, 0.859f), // Blue
        new Color(0.180f, 0.800f, 0.443f), // Green
        new Color(0.914f, 0.118f, 0.549f), // Pink
        new Color(0.953f, 0.612f, 0.071f), // Yellow
    };

    public LevelData Generate(int difficultyIndex)
    {
        var config = BuildConfig(difficultyIndex);
        LevelData data;
        int attempts = 0;
        do
        {
            data = TryGenerate(config, difficultyIndex);
            attempts++;
        }
        while (!IsSolvable(data) && attempts < 50);

        return data;
    }

    private struct GenConfig
    {
        public int width, height, blockCount;
        public float multiCellChance, heavyChance;
    }

    [Header("Max Grid Size (phai khop voi GridSystem)")]
    [SerializeField] private int maxWidth = 5;
    [SerializeField] private int maxHeight = 7;

    private GenConfig BuildConfig(int difficulty)
    {
        float t = Mathf.Clamp01(difficulty / 20f);
        return new GenConfig
        {
            width = Mathf.Min(baseWidth + Mathf.FloorToInt(t * 3), maxWidth),
            height = Mathf.Min(baseHeight + Mathf.FloorToInt(t * 4), maxHeight),
            blockCount = Mathf.RoundToInt(Mathf.Lerp(4, 18, t)),
            multiCellChance = 0f, // Tắt multi-cell để tránh bug chặn chéo
            heavyChance = Mathf.Lerp(0f, 0.2f, t),
        };
    }

    private LevelData TryGenerate(GenConfig cfg, int difficultyIndex)
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.gridWidth = cfg.width;
        data.gridHeight = cfg.height;
        data.cellSize = 1.6f;
        data.blocks = new List<BlockData>();

        // Chọn 1 màu duy nhất cho toàn bộ level, xoay vòng 4 màu
        Color levelColor = levelColors[difficultyIndex % levelColors.Length];

        bool[,] occupied = new bool[cfg.width, cfg.height];

        // ─── RESERVE ROTATING GEAR POSITIONS FIRST ───────────────────────────
        // Đặt rotating gear trước để blocks không spawn trùng vị trí
        List<Vector2Int> reservedRotatingGearPositions = new List<Vector2Int>();
        List<Vector2Int> mandatoryBlockPositions = new List<Vector2Int>(); // NEW: Blocks bắt buộc xung quanh gear

        if (difficultyIndex >= 14) // Level 15+
        {
            int rotatingGearCount = 1;

            for (int rg = 0; rg < rotatingGearCount * 20 && reservedRotatingGearPositions.Count < rotatingGearCount; rg++)
            {
                int rgx = Random.Range(1, cfg.width - 1);
                int rgy = Random.Range(1, cfg.height - 1);

                if (!occupied[rgx, rgy])
                {
                    // Check 4 vị trí xung quanh có trong grid không
                    Vector2Int[] neighbors = {
                        new Vector2Int(rgx, rgy + 1),     // Top
                        new Vector2Int(rgx + 1, rgy),     // Right
                        new Vector2Int(rgx, rgy - 1),     // Bottom
                        new Vector2Int(rgx - 1, rgy)      // Left
                    };

                    bool canPlace = true;
                    List<Vector2Int> validNeighbors = new List<Vector2Int>();

                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor.x < 0 || neighbor.x >= cfg.width ||
                            neighbor.y < 0 || neighbor.y >= cfg.height)
                        {
                            canPlace = false;
                            break;
                        }
                        if (!occupied[neighbor.x, neighbor.y])
                            validNeighbors.Add(neighbor);
                    }

                    // Cần ít nhất 1 vị trí trống xung quanh để đặt block
                    if (canPlace && validNeighbors.Count >= 1)
                    {
                        occupied[rgx, rgy] = true; // MARK VỊ TRÍ GEAR
                        reservedRotatingGearPositions.Add(new Vector2Int(rgx, rgy));

                        // Spawn 1-2 blocks xung quanh gear (MANDATORY)
                        int blocksToPlace = Mathf.Min(2, validNeighbors.Count);
                        for (int i = 0; i < blocksToPlace; i++)
                        {
                            int idx = Random.Range(0, validNeighbors.Count);
                            Vector2Int blockPos = validNeighbors[idx];
                            occupied[blockPos.x, blockPos.y] = true;
                            mandatoryBlockPositions.Add(blockPos);
                            validNeighbors.RemoveAt(idx);
                        }

                        Debug.Log($"[LevelGenerator] Reserved rotating gear at ({rgx}, {rgy}) with {blocksToPlace} mandatory blocks");
                    }
                }
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        int placed = 0;

        // ─── PLACE MANDATORY BLOCKS FIRST ───────────────────────────────────
        foreach (var blockPos in mandatoryBlockPositions)
        {
            data.blocks.Add(new BlockData
            {
                gridPosition = blockPos,
                size = Vector2Int.one,
                direction = (SlideDirection)Random.Range(0, 4),
                color = levelColor,
                blockType = BlockType.Normal,
            });
            placed++;
        }
        // ─────────────────────────────────────────────────────────────────────

        for (int i = 0; i < cfg.blockCount * 20 && placed < cfg.blockCount; i++)
        {
            Vector2Int size = Vector2Int.one;
            if (Random.value < cfg.multiCellChance)
                size = Random.value < 0.5f ? new Vector2Int(2, 1) : new Vector2Int(1, 2);

            int px = Random.Range(0, cfg.width - size.x + 1);
            int py = Random.Range(0, cfg.height - size.y + 1);

            if (!CanPlace(occupied, px, py, size, cfg.width, cfg.height)) continue;

            for (int dx = 0; dx < size.x; dx++)
                for (int dy = 0; dy < size.y; dy++)
                    occupied[px + dx, py + dy] = true;

            data.blocks.Add(new BlockData
            {
                gridPosition = new Vector2Int(px, py),
                size = size,
                direction = (SlideDirection)Random.Range(0, 4),
                color = levelColor,   // tất cả blocks cùng màu
                blockType = Random.value < cfg.heavyChance ? BlockType.Heavy : BlockType.Normal,
            });
            placed++;
        }

        // Gear xuất hiện từ level 3+, số lượng random tăng dần theo độ khó
        data.gearPositions = new List<Vector2Int>();
        if (difficultyIndex >= 2)
        {
            // Càng về sau max gear càng cao, nhưng luôn random trong khoảng
            // Level 3-5:  random 1-1
            // Level 6-9:  random 1-2
            // Level 10-14: random 1-3
            // Level 15+:  random 1-4
            int maxGear = difficultyIndex switch
            {
                >= 15 => 4,
                >= 10 => 3,
                >= 6 => 2,
                _ => 1
            };
            int gearCount = Random.Range(1, maxGear + 1);

            for (int g = 0; g < gearCount * 20 && data.gearPositions.Count < gearCount; g++)
            {
                int gx = Random.Range(1, cfg.width - 1);
                int gy = Random.Range(1, cfg.height - 1);
                if (!occupied[gx, gy])
                {
                    occupied[gx, gy] = true;
                    data.gearPositions.Add(new Vector2Int(gx, gy));
                }
            }
            Debug.Log($"[LevelGenerator] Level {difficultyIndex}: {data.gearPositions.Count}/{maxGear} gears");
        }

        // ─── ASSIGN ROTATING GEAR POSITIONS (đã reserve trước) ──────────────
        data.rotatingGearPositions = reservedRotatingGearPositions;
        // ─────────────────────────────────────────────────────────────────────

        return data;
    }

    private bool CanPlace(bool[,] occ, int x, int y, Vector2Int size, int w, int h)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= w || ny >= h || occ[nx, ny]) return false;
            }
        return true;
    }

    private bool IsSolvable(LevelData data)
    {
        var blocksSim = new List<BlockData>(data.blocks);
        var gearSim = new List<Vector2Int>(data.gearPositions ?? new List<Vector2Int>());

        bool progress = true;
        while (progress && blocksSim.Count > 0)
        {
            progress = false;
            for (int i = blocksSim.Count - 1; i >= 0; i--)
            {
                if (blocksSim[i].blockType == BlockType.Locked) continue;

                // Block có thể thoát ra ngoài HOẶC lao vào gear đều coi là hợp lệ
                if (SimCanExit(blocksSim[i], blocksSim, gearSim, data.gridWidth, data.gridHeight))
                {
                    blocksSim.RemoveAt(i);
                    progress = true;
                }
            }
        }
        foreach (var b in blocksSim)
            if (b.blockType != BlockType.Locked) return false;
        return true;
    }

    private bool SimCanExit(BlockData b, List<BlockData> all, List<Vector2Int> gears, int w, int h)
    {
        Vector2Int dir = GridSystem.DirectionToGrid(b.direction);
        var myCells = GetCells(b);
        var frontEdge = GetFront(myCells, dir);

        foreach (var edge in frontEdge)
        {
            var check = edge + dir;
            while (true)
            {
                // Ra ngoài biên = thoát được
                if (check.x < 0 || check.x >= w || check.y < 0 || check.y >= h)
                    return true;

                // Gặp gear = bị nghiền = cũng coi là "thoát được"
                if (gears.Contains(check))
                    return true;

                // Gặp block khác = bị chặn
                bool blocked = false;
                foreach (var other in all)
                    if (other != b && GetCells(other).Contains(check)) { blocked = true; break; }
                if (blocked) break;

                check += dir;
            }
        }
        return false;
    }

    private List<Vector2Int> GetCells(BlockData b)
    {
        var cells = new List<Vector2Int>();
        for (int x = 0; x < b.size.x; x++)
            for (int y = 0; y < b.size.y; y++)
                cells.Add(b.gridPosition + new Vector2Int(x, y));
        return cells;
    }

    private List<Vector2Int> GetFront(List<Vector2Int> cells, Vector2Int dir)
    {
        var edge = new List<Vector2Int>();
        foreach (var c in cells)
            if (!cells.Contains(c + dir))
                edge.Add(c);
        return edge;
    }
}