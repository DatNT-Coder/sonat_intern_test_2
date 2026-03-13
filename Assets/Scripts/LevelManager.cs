using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BlockSpawner blockSpawner;
    [SerializeField] private LevelDatabase levelDatabase;
    [SerializeField] private GridSystem gridSystem;

    [Header("Gear Prefab")]
    [SerializeField] private GearBlock gearPrefab;

    [Header("Procedural Generation")]
    [SerializeField] private LevelGenerator levelGenerator;

    private List<Block> _activeBlocks = new List<Block>();
    private List<GearBlock> _activeGears = new List<GearBlock>();
    private LevelData _currentLevelData;

    public int TotalBlocks => _activeBlocks.Count;
    public int RemainingBlocks { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            if (GameManager.Instance.CurrentState == GameState.Playing)
                LoadLevel(GameManager.Instance.CurrentLevel);
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Playing)
            LoadLevel(GameManager.Instance.CurrentLevel);
    }

    public void LoadLevel(int levelIndex)
    {
        ClearLevel();

        if (levelDatabase != null && levelIndex < levelDatabase.Levels.Count)
            _currentLevelData = levelDatabase.Levels[levelIndex];
        else if (levelGenerator != null)
            _currentLevelData = levelGenerator.Generate(levelIndex);
        else
        {
            Debug.LogWarning("[LevelManager] No LevelDatabase or LevelGenerator!");
            return;
        }

        Debug.Log($"[LevelManager] Loading: {_currentLevelData.levelName}, {_currentLevelData.blocks.Count} blocks");

        // Spawn gear trước (dưới blocks)
        SpawnGears(_currentLevelData);

        StartCoroutine(SpawnBlocksSequential(_currentLevelData));
    }

    // ─── Spawn Gears ─────────────────────────────────────────────────────────

    private void SpawnGears(LevelData data)
    {
        if (gearPrefab == null || data.gearPositions == null) return;

        foreach (var gearPos in data.gearPositions)
        {
            Vector3 worldPos = gridSystem.GridToWorld(gearPos);
            GearBlock gear = Instantiate(gearPrefab, worldPos, Quaternion.identity);
            gear.Init(gearPos, gridSystem);
            gridSystem.RegisterGear(gear);
            _activeGears.Add(gear);
        }

        Debug.Log($"[LevelManager] Spawned {_activeGears.Count} gears");
    }

    // ─── Spawn Blocks ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBlocksSequential(LevelData data)
    {
        _activeBlocks.Clear();
        RemainingBlocks = 0;

        foreach (var blockData in data.blocks)
        {
            Block b = blockSpawner.SpawnBlock(blockData);
            if (b != null)
            {
                _activeBlocks.Add(b);
                b.OnBlockRemoved += HandleBlockRemoved;
                RemainingBlocks++;
            }
            yield return new WaitForSeconds(0.03f);
        }

        Debug.Log($"[LevelManager] Spawned {RemainingBlocks} blocks");
        UIManager.Instance?.UpdateBlockCounter(RemainingBlocks, RemainingBlocks);
    }

    private void HandleBlockRemoved(Block block)
    {
        _activeBlocks.Remove(block);
        RemainingBlocks--;
        UIManager.Instance?.UpdateBlockCounter(RemainingBlocks, TotalBlocks);
        if (RemainingBlocks <= 0)
            StartCoroutine(DelayedWin());
    }

    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.WinLevel();
    }

    public void ClearLevel()
    {
        StopAllCoroutines();

        foreach (var block in _activeBlocks)
            if (block != null) { block.OnBlockRemoved -= HandleBlockRemoved; Destroy(block.gameObject); }
        _activeBlocks.Clear();
        RemainingBlocks = 0;

        // Xóa gear cũ
        foreach (var gear in _activeGears)
            if (gear != null) { gridSystem?.UnregisterGear(gear); Destroy(gear.gameObject); }
        _activeGears.Clear();
    }
}