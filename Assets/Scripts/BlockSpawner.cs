using UnityEngine;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Block blockPrefab;

    [Header("References")]
    [SerializeField] private GridSystem gridSystem;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 30;

    private Queue<Block> _pool = new Queue<Block>();
    private Transform _poolParent;

    void Awake()
    {
        _poolParent = new GameObject("BlockPool").transform;
        _poolParent.SetParent(transform);
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var b = Instantiate(blockPrefab, _poolParent);
            b.gameObject.SetActive(false);
            _pool.Enqueue(b);
        }
    }

    public Block SpawnBlock(BlockData data)
    {
        Block block = GetFromPool();
        block.gameObject.SetActive(true);

        // Đặt đúng vị trí world từ grid
        block.transform.position = gridSystem.GridToWorld(data.gridPosition);
        block.transform.localScale = Vector3.one;

        block.Init(data, gridSystem);
        gridSystem.RegisterBlock(block);
        return block;
    }

    private Block GetFromPool()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        return Instantiate(blockPrefab, _poolParent);
    }
}