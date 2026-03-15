using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class RotatingGear : MonoBehaviour
{
    [Header("Visual Layers")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer middleRenderer;

    [Header("Connection Lines")]
    [SerializeField] private Color lineColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private int lineSortingOrder = 1;

    [Header("Rotation Settings")]
    [SerializeField] private float blockMoveDuration = 0.3f;

    private GridSystem _grid;
    private Vector2Int _gridPosition;
    private bool _isRotating = false;

    private LineRenderer[] _connectionLines = new LineRenderer[4];

    public Vector2Int GridPosition => _gridPosition;

    public void Init(Vector2Int gridPos, GridSystem grid)
    {
        _gridPosition = gridPos;
        _grid = grid;

        // tránh register trùng
        if (_grid.GetRotatingGearAt(_gridPosition) == null)
        {
            _grid.RegisterRotatingGear(this);
        }

        CreateConnectionLines();
        UpdateConnectionLines();
    }

    void OnMouseDown()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        if (_isRotating) return;

        TryRotateBlocks();
    }

    private void CreateConnectionLines()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject lineObj = new GameObject($"ConnectionLine_{i}");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.sortingLayerName = "Default";
            lr.sortingOrder = lineSortingOrder;
            lr.useWorldSpace = true;

            _connectionLines[i] = lr;
        }
    }

    private void UpdateConnectionLines()
    {
        if (_grid == null) return;

        Vector3 gearCenter = transform.position;

        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        for (int i = 0; i < 4; i++)
        {
            Vector2Int blockPos = _gridPosition + directions[i];
            Block block = _grid.GetBlockAt(blockPos);

            if (block != null)
            {
                Vector3 blockCenter = block.transform.position;

                _connectionLines[i].SetPosition(0, gearCenter);
                _connectionLines[i].SetPosition(1, blockCenter);
                _connectionLines[i].enabled = true;
            }
            else
            {
                _connectionLines[i].enabled = false;
            }
        }
    }

    void Update()
    {
        if (!_isRotating)
        {
            UpdateConnectionLines();
        }
    }

    private void TryRotateBlocks()
    {
        Vector2Int topPos = _gridPosition + Vector2Int.up;
        Vector2Int rightPos = _gridPosition + Vector2Int.right;
        Vector2Int bottomPos = _gridPosition + Vector2Int.down;
        Vector2Int leftPos = _gridPosition + Vector2Int.left;

        Vector2Int[] positions = { topPos, rightPos, bottomPos, leftPos };

        Block topBlock = _grid.GetBlockAt(topPos);
        Block rightBlock = _grid.GetBlockAt(rightPos);
        Block bottomBlock = _grid.GetBlockAt(bottomPos);
        Block leftBlock = _grid.GetBlockAt(leftPos);

        Block[] blocks = { topBlock, rightBlock, bottomBlock, leftBlock };

        bool hasBlock = false;

        foreach (var block in blocks)
        {
            if (block != null)
            {
                hasBlock = true;
                break;
            }
        }

        if (!hasBlock)
        {
            Debug.Log("[RotatingGear] No blocks around to rotate");
            PlayCollisionFeedback();
            return;
        }

        if (!CanRotate(positions, blocks))
        {
            Debug.Log("[RotatingGear] COLLISION! Cannot rotate blocks");
            PlayCollisionFeedback();
            AudioManager.Instance?.PlayBlockTap();
            return;
        }

        StartCoroutine(RotateBlocksClockwise(positions, blocks));
        GameManager.Instance?.UseMove();
    }

    private bool CanRotate(Vector2Int[] positions, Block[] blocks)
    {
        for (int i = 0; i < 4; i++)
        {
            Block currentBlock = blocks[i];
            if (currentBlock == null) continue;

            int nextIndex = (i + 1) % 4;
            Vector2Int targetPos = positions[nextIndex];

            // ❌ Không cho block xoay vào gear khác
            RotatingGear gearAtTarget = _grid.GetRotatingGearAt(targetPos);

            if (gearAtTarget != null && gearAtTarget != this)
            {
                Debug.Log("[RotatingGear] Cannot rotate: target cell has another gear!");
                return false;
            }

            Block blockAtTarget = _grid.GetBlockAt(targetPos);

            if (blockAtTarget != null)
            {
                bool isPartOfRotation = false;

                for (int j = 0; j < 4; j++)
                {
                    if (blockAtTarget == blocks[j])
                    {
                        isPartOfRotation = true;
                        break;
                    }
                }

                if (!isPartOfRotation)
                    return false;
            }
        }

        return true;
    }

    private IEnumerator RotateBlocksClockwise(Vector2Int[] positions, Block[] blocks)
    {
        _isRotating = true;

        List<BlockMoveData> moveDataList = new List<BlockMoveData>();

        for (int i = 0; i < 4; i++)
        {
            if (blocks[i] != null)
            {
                int nextIndex = (i + 1) % 4;
                Vector2Int targetGridPos = positions[nextIndex];
                Vector3 targetWorldPos = _grid.GridToWorld(targetGridPos);

                moveDataList.Add(new BlockMoveData
                {
                    block = blocks[i],
                    oldGridPos = positions[i],
                    newGridPos = targetGridPos,
                    targetWorldPos = targetWorldPos
                });
            }
        }

        foreach (var data in moveDataList)
        {
            _grid.UnregisterBlock(data.block);
        }

        foreach (var data in moveDataList)
        {
            data.block.transform.DOMove(data.targetWorldPos, blockMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        float elapsed = 0f;

        while (elapsed < blockMoveDuration)
        {
            UpdateConnectionLines();
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var data in moveDataList)
        {
            data.block.SetGridPosition(data.newGridPos);
            _grid.RegisterBlock(data.block);
        }

        UpdateConnectionLines();

        _isRotating = false;

        AudioManager.Instance?.PlayBlockSlide();
    }

    private void PlayCollisionFeedback()
    {
        SpriteRenderer[] allRenderers = { backgroundRenderer, middleRenderer };

        foreach (var renderer in allRenderers)
        {
            if (renderer == null) continue;

            Color originalColor = renderer.color;

            Sequence seq = DOTween.Sequence();
            seq.Append(renderer.DOColor(Color.red, 0.1f));
            seq.Append(renderer.DOColor(originalColor, 0.1f));
        }

        transform.DOShakePosition(0.3f, 0.1f, 10, 90, false, true);
    }

    void OnDestroy()
    {
        transform.DOKill();

        if (_grid != null)
            _grid.UnregisterRotatingGear(this);

        foreach (var line in _connectionLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
    }

    private class BlockMoveData
    {
        public Block block;
        public Vector2Int oldGridPos;
        public Vector2Int newGridPos;
        public Vector3 targetWorldPos;
    }
}