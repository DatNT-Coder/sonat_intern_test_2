using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    public event Action<Block> OnBlockRemoved;

    [Header("Visual")]
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer arrowRenderer;

    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float exitDistance = 15f;

    private BlockData _data;
    private GridSystem _grid;
    private bool _isSliding;
    private int _hp = 1;

    public Vector2Int GridPosition => _data.gridPosition;
    public Vector2Int Size => _data.size;
    public SlideDirection Direction => _data.direction;
    public bool IsSliding => _isSliding;

    public void Init(BlockData data, GridSystem grid)
    {
        _data = data;
        _grid = grid;
        _hp = data.blockType == BlockType.Heavy ? 2 : 1;
        transform.localScale = new Vector3(0.942f, 0.942f, 1f);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = _data.color;

        if (arrowRenderer != null)
        {
            float angle = _data.direction switch
            {
                SlideDirection.Right => 0f,
                SlideDirection.Left => 180f,
                SlideDirection.Up => 90f,
                SlideDirection.Down => 270f,
                _ => 0f
            };
            arrowRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void OnMouseDown()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        TrySlide();
    }

    public void TrySlide()
    {
        if (_isSliding) return;
        GameManager.Instance?.UseMove(); // Mỗi lần tap = 1 move

        if (_hp > 1)
        {
            _hp--;
            PlayHitEffect();
            AudioManager.Instance?.PlayBlockTap();
            return;
        }

        GearBlock gear = _grid.GetGearInPath(this);
        if (gear != null)
        {
            StartCoroutine(SlideIntoGear(gear));
            return;
        }

        Block blocker = _grid.GetBlocker(this);
        Debug.Log($"[Block] {name} gridPos={_data.gridPosition} dir={_data.direction} blocker={(blocker != null ? blocker.name + "@" + blocker.GridPosition : "none")}");
        if (blocker != null)
            StartCoroutine(SlideToBlocker(blocker));
        else
            StartCoroutine(SlideOffScreen());
    }

    // ─── Trượt đến cạnh block chặn rồi dừng ─────────────────────────────

    private IEnumerator SlideToBlocker(Block blocker)
    {
        _isSliding = true;
        _grid.UnregisterBlock(this);

        // Tính vị trí dừng: cạnh sát blocker
        Vector3 stopPos = GetStopPosition(blocker);
        float dist = Vector3.Distance(transform.position, stopPos);
        float duration = Mathf.Max(0.08f, dist / slideSpeed);

        AudioManager.Instance?.PlayBlockSlide();

        // Trượt đến vị trí dừng
        yield return transform.DOMove(stopPos, duration)
                              .SetEase(Ease.OutQuad)
                              .WaitForCompletion();

        // Cập nhật gridPosition mới
        _data.gridPosition = _grid.WorldToGrid(stopPos);
        _grid.RegisterBlock(this);
        _isSliding = false;

        // Hiệu ứng va chạm: block này rung nhẹ
        Vector2 dir = Block.DirectionToVector(_data.direction);
        transform.DOPunchPosition((Vector3)(dir * 0.08f), 0.2f, 6, 0.3f);

        // Blocker nháy đỏ
        blocker.PlayBlockedFlash();

        AudioManager.Instance?.PlayBlockTap();
    }

    // ─── Trượt vào gear → bị nghiền ─────────────────────────────────────

    private IEnumerator SlideIntoGear(GearBlock gear)
    {
        _isSliding = true;
        _grid.UnregisterBlock(this);
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Dừng ngay tại rìa gear (cách tâm gear 1 nửa cellSize)
        float cellSize = _grid.CellSize;
        Vector3 gearPos = gear.transform.position;
        Vector2 dir = DirectionToVector(_data.direction);
        // Vị trí rìa = tâm gear - hướng * nửa ô
        Vector3 edgePos = gearPos - (Vector3)(dir * cellSize * 0.5f);

        float dist = Vector3.Distance(transform.position, edgePos);
        float duration = Mathf.Max(0.05f, dist / slideSpeed);

        AudioManager.Instance?.PlayBlockSlide();
        yield return transform.DOMove(edgePos, duration).SetEase(Ease.OutQuad).WaitForCompletion();

        gear.CrushBlock(this);
    }

    // ─── Trượt ra ngoài màn hình ─────────────────────────────────────────

    private IEnumerator SlideOffScreen()
    {
        _isSliding = true;
        _grid.UnregisterBlock(this);

        Vector2 slideDir = DirectionToVector(_data.direction);
        Vector3 targetPos = transform.position + (Vector3)(slideDir * exitDistance);
        float duration = exitDistance / slideSpeed;

        AudioManager.Instance?.PlayBlockSlide();

        transform.DOMove(targetPos, duration).SetEase(Ease.InCubic);
        spriteRenderer?.DOFade(0f, 0.25f).SetDelay(duration * 0.5f);
        if (arrowRenderer) arrowRenderer.DOFade(0f, 0.25f).SetDelay(duration * 0.5f);

        yield return new WaitForSeconds(duration * 0.6f);

        AudioManager.Instance?.PlayBlockExit();
        EffectsManager.Instance?.SpawnExitParticle(transform.position, _data.color);
        OnBlockRemoved?.Invoke(this);
        Destroy(gameObject);
    }

    // ─── Tính vị trí dừng sát cạnh blocker ──────────────────────────────

    private Vector3 GetStopPosition(Block blocker)
    {
        float cell = _grid.CellSize;
        Vector3 blockerPos = blocker.transform.position;
        Vector3 myPos = transform.position;

        switch (_data.direction)
        {
            case SlideDirection.Right:
                return new Vector3(blockerPos.x - cell, myPos.y, 0);
            case SlideDirection.Left:
                return new Vector3(blockerPos.x + cell, myPos.y, 0);
            case SlideDirection.Up:
                return new Vector3(myPos.x, blockerPos.y - cell, 0);
            case SlideDirection.Down:
                return new Vector3(myPos.x, blockerPos.y + cell, 0);
            default:
                return myPos;
        }
    }

    // ─── Hiệu ứng nháy đỏ khi bị va chạm vào ────────────────────────────

    public void PlayBlockedFlash()
    {
        if (spriteRenderer == null) return;

        // Kill tween màu cũ và reset màu về gốc trước khi chạy animation mới
        spriteRenderer.DOKill();
        transform.DOKill();
        spriteRenderer.color = _data.color;

        // Nháy đỏ
        Sequence seq = DOTween.Sequence();
        seq.Append(spriteRenderer.DOColor(Color.red, 0.07f));
        seq.Append(spriteRenderer.DOColor(_data.color, 0.07f));
        seq.Append(spriteRenderer.DOColor(Color.red, 0.07f));
        seq.Append(spriteRenderer.DOColor(_data.color, 0.1f));
        seq.SetAutoKill(true);

        // Rung khi bị tông
        Vector3 shakeDir = new Vector3(
            UnityEngine.Random.Range(-0.07f, 0.07f),
            UnityEngine.Random.Range(-0.07f, 0.07f),
            0f
        );
        transform.DOPunchPosition(shakeDir, 0.2f, 8, 0.4f);
    }

    // ─── Hit effect khi tap heavy block ─────────────────────────────────

    private void PlayHitEffect()
    {
        transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.5f);
    }

    // ─── Helper ──────────────────────────────────────────────────────────

    /// <summary>Cho phép GearBlock báo event remove mà không cần invoke trực tiếp</summary>
    public void NotifyRemoved()
    {
        Debug.Log($"[Block] NotifyRemoved called, listeners={OnBlockRemoved?.GetInvocationList()?.Length ?? 0}");
        OnBlockRemoved?.Invoke(this);
    }

    public static Vector2 DirectionToVector(SlideDirection dir)
    {
        return dir switch
        {
            SlideDirection.Up => Vector2.up,
            SlideDirection.Down => Vector2.down,
            SlideDirection.Left => Vector2.left,
            SlideDirection.Right => Vector2.right,
            _ => Vector2.zero
        };
    }
}