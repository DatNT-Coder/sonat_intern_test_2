using UnityEngine;
using DG.Tweening;

public class GearBlock : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer gearRenderer;
    [SerializeField] private float rotateSpeed = 120f;

    [Header("Crush Effect")]
    [SerializeField] private float crushScalePunch = 0.3f;

    private GridSystem _grid;
    private Vector2Int _gridPosition;

    public Vector2Int GridPosition => _gridPosition;

    public void Init(Vector2Int gridPos, GridSystem grid)
    {
        _gridPosition = gridPos;
        _grid = grid;
        StartRotation();
    }

    private void StartRotation()
    {
        if (gearRenderer == null) return;
        gearRenderer.transform
            .DORotate(new Vector3(0, 0, -360f), 360f / rotateSpeed, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    }

    public void CrushBlock(Block block)
    {
        // Gear chỉ phình to nhẹ — không nháy đỏ, không rung
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * crushScalePunch, 0.15f, 4, 0.2f);

        // Block bị nghiền: scale về 0 nhanh rồi destroy
        Color blockColor = block.spriteRenderer != null ? block.spriteRenderer.color : Color.white;

        block.transform.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(block.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            EffectsManager.Instance?.SpawnExitParticle(block.transform.position, blockColor);
            block.NotifyRemoved();
            Destroy(block.gameObject);
        });

        AudioManager.Instance?.PlayBlockExit();
    }

    void OnDestroy()
    {
        gearRenderer?.transform.DOKill();
    }
}