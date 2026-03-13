using UnityEngine;
using System.Collections.Generic;

// ─── Direction Enum ───────────────────────────────────────────────────────────

public enum SlideDirection
{
    Up,
    Down,
    Left,
    Right
}

// ─── Block Data ───────────────────────────────────────────────────────────────

/// <summary>
/// Serializable data for a single block's position and properties.
/// </summary>
[System.Serializable]
public class BlockData
{
    public Vector2Int gridPosition;     // position on logical grid
    public Vector2Int size = Vector2Int.one; // 1x1, 1x2, 2x1, etc.
    public SlideDirection direction;
    public BlockType blockType;
    public Color color = Color.white;
}

public enum BlockType
{
    Normal,
    Locked,     // cannot be removed until unlocked
    Heavy,      // takes 2 taps
    Bomb        // removes adjacent blocks when removed
}

// ─── Level Data ───────────────────────────────────────────────────────────────

/// <summary>
/// ScriptableObject holding all block definitions for one level.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "TapAway/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    public int gridWidth = 5;
    public int gridHeight = 7;
    public float cellSize = 1.1f;

    [Header("Blocks")]
    public List<BlockData> blocks = new List<BlockData>();

    [Header("Gear Obstacles")]
    public List<Vector2Int> gearPositions = new List<Vector2Int>();

    [Header("Difficulty")]
    [Range(1, 10)] public int difficulty = 1;
}

// ─── Level Database ───────────────────────────────────────────────────────────

/// <summary>
/// ScriptableObject holding a list of all hand-crafted levels.
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "TapAway/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelData> Levels = new List<LevelData>();
}