using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tính số moves tối thiểu để giải level bằng cách simulate greedy.
/// </summary>
public static class MovesCalculator
{
    [System.Serializable]
    public class SimBlock
    {
        public Vector2Int gridPos;
        public SlideDirection direction;
        public bool removed;
    }

    /// <summary>
    /// Tính minimum moves cần thiết để clear level.
    /// </summary>
    public static int CalculateMinMoves(LevelData data)
    {
        // Tạo bản sao để simulate
        var blocks = data.blocks.Select(b => new SimBlock
        {
            gridPos = b.gridPosition,
            direction = b.direction,
            removed = false
        }).ToList();

        var gearPositions = new HashSet<Vector2Int>(data.gearPositions ?? new List<Vector2Int>());

        int totalMoves = 0;
        int maxPasses = blocks.Count * blocks.Count; // tránh infinite loop
        int passCount = 0;
        bool madeProgress = true;

        while (madeProgress && passCount < maxPasses)
        {
            madeProgress = false;
            passCount++;

            foreach (var block in blocks)
            {
                if (block.removed) continue;

                if (CanExit(block, blocks, gearPositions, data.gridWidth, data.gridHeight))
                {
                    block.removed = true;
                    totalMoves++;
                    madeProgress = true;
                }
            }
        }

        // Các block không giải được (deadlock) — thêm moves dự phòng
        int stuck = blocks.Count(b => !b.removed);
        totalMoves += stuck * 3;

        return totalMoves;
    }

    /// <summary>
    /// Kiểm tra block có thể thoát ra không (đường thông hoặc đến gear).
    /// </summary>
    private static bool CanExit(SimBlock block, List<SimBlock> allBlocks,
        HashSet<Vector2Int> gears, int gridW, int gridH)
    {
        Vector2Int dir = DirectionToVec(block.direction);
        Vector2Int check = block.gridPos + dir;

        while (check.x >= 0 && check.x < gridW && check.y >= 0 && check.y < gridH)
        {
            // Gặp gear → thoát được
            if (gears.Contains(check)) return true;

            // Gặp block chưa bị xóa → bị chặn
            var blocker = allBlocks.FirstOrDefault(b =>
                !b.removed && b != block && b.gridPos == check);
            if (blocker != null) return false;

            check += dir;
        }

        // Ra khỏi grid → thoát được
        return true;
    }

    private static Vector2Int DirectionToVec(SlideDirection dir)
    {
        return dir switch
        {
            SlideDirection.Right => Vector2Int.right,
            SlideDirection.Left => Vector2Int.left,
            SlideDirection.Up => Vector2Int.up,
            SlideDirection.Down => Vector2Int.down,
            _ => Vector2Int.zero
        };
    }

    /// <summary>
    /// Tính moves cho player = minMoves * multiplier + buffer.
    /// Difficulty 0-1: multiplier từ 2.0 xuống 1.4
    /// </summary>
    public static int CalculatePlayerMoves(LevelData data, int levelIndex)
    {
        int minMoves = CalculateMinMoves(data);
        float t = Mathf.Clamp01(levelIndex / 30f);
        float mult = Mathf.Lerp(2.0f, 1.4f, t); // level cao → ít buffer hơn
        int buffer = Mathf.RoundToInt(minMoves * mult);

        // Tối thiểu = số block, tối đa = 60
        int result = Mathf.Clamp(buffer, data.blocks.Count, 60);

        Debug.Log($"[Moves] Level {levelIndex}: min={minMoves}, mult={mult:F1}, final={result}");
        return result;
    }
}