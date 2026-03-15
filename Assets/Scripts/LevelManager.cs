using DG.Tweening;
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

    [Header("Moves Config")]
    [SerializeField] private int baseMoves = 20;
    [SerializeField] private int movesPerBlock = 2;

    private List<Block> _activeBlocks = new List<Block>();
    private List<GearBlock> _activeGears = new List<GearBlock>();
    private LevelData _currentLevelData;
    private int _currentLevelIndex;
    private bool _isResuming = false;
    private int _totalBlocks;

    public int RemainingBlocks => _activeBlocks.Count;

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
        if (state == GameState.Playing && !_isResuming)
            LoadLevel(GameManager.Instance.CurrentLevel);
        _isResuming = false;
    }

    public void ResumeWithoutReload()
    {
        _isResuming = true;
        GameManager.Instance.SetState(GameState.Playing);
    }

    public void LoadLevel(int levelIndex)
    {
        ClearLevel();
        AudioManager.Instance?.StopAllSFX();

        if (levelDatabase != null && levelIndex < levelDatabase.Levels.Count)
            _currentLevelData = levelDatabase.Levels[levelIndex];
        else if (levelGenerator != null)
            _currentLevelData = levelGenerator.Generate(levelIndex);
        else { Debug.LogWarning("[LevelManager] No data source!"); return; }

        _currentLevelIndex = levelIndex;
        Debug.Log($"[LevelManager] Loading: {_currentLevelData.levelName}, {_currentLevelData.blocks.Count} blocks");

        // Cập nhật GridSystem theo kích thước level
        gridSystem.Resize(_currentLevelData.gridWidth, _currentLevelData.gridHeight);

        // Tính số moves bằng simulation
        int moves = MovesCalculator.CalculatePlayerMoves(_currentLevelData, levelIndex);
        GameManager.Instance?.InitMoves(moves);

        SpawnGears(_currentLevelData);
        StartCoroutine(SpawnBlocksSequential(_currentLevelData));
    }

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
    }

    private IEnumerator SpawnBlocksSequential(LevelData data)
    {
        _activeBlocks.Clear();

        foreach (var blockData in data.blocks)
        {
            Block b = blockSpawner.SpawnBlock(blockData);
            if (b != null)
            {
                _activeBlocks.Add(b);
                b.OnBlockRemoved += HandleBlockRemoved;
            }
            yield return new WaitForSeconds(0.03f);
        }

        _totalBlocks = _activeBlocks.Count;
        Debug.Log($"[LevelManager] Spawned {_totalBlocks} blocks");
        UIManager.Instance?.UpdateBlockCounter(_activeBlocks.Count, _totalBlocks);
        UIManager.Instance?.SetLevelLabel(GameManager.Instance.CurrentLevel + 1);

        // Nếu level không có block nào → win luôn
        if (_totalBlocks == 0)
        {
            Debug.LogWarning("[LevelManager] No blocks in level, auto-win!");
            StartCoroutine(DelayedWin());
        }
    }

    private void HandleBlockRemoved(Block block)
    {
        _activeBlocks.Remove(block);
        UIManager.Instance?.UpdateBlockCounter(_activeBlocks.Count, _totalBlocks);
        Debug.Log($"[LevelManager] Block removed, remaining={_activeBlocks.Count}");

        if (_activeBlocks.Count <= 0)
        {
            Debug.Log("[LevelManager] All blocks cleared! Triggering win...");
            StartCoroutine(DelayedWin());
        }
    }

    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(0.5f);
        if (GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.WinLevel();
    }

    public void RemoveRandomBlock()
    {
        if (_activeBlocks.Count == 0) return;
        int idx = Random.Range(0, _activeBlocks.Count);
        Block b = _activeBlocks[idx];
        if (b != null)
        {
            _activeBlocks.RemoveAt(idx);
            b.OnBlockRemoved -= HandleBlockRemoved;
            gridSystem?.UnregisterBlock(b);
            b.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() => Destroy(b.gameObject));
            // Check win
            if (_activeBlocks.Count <= 0)
                StartCoroutine(DelayedWin());
        }
    }

    public void ClearLevel()
    {
        StopAllCoroutines();
        foreach (var block in _activeBlocks)
        {
            if (block != null)
            {
                block.OnBlockRemoved -= HandleBlockRemoved;
                gridSystem?.UnregisterBlock(block); // Unregister trước khi Destroy
                Destroy(block.gameObject);
            }
        }
        _activeBlocks.Clear();

        foreach (var gear in _activeGears)
            if (gear != null) { gridSystem?.UnregisterGear(gear); Destroy(gear.gameObject); }
        _activeGears.Clear();

        // Clear grid hoàn toàn để không còn block ma
        gridSystem?.ForceClean();
    }
}