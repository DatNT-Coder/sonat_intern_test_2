using UnityEngine;
using DG.Tweening;

/// <summary>
/// Adds a subtle scale pulse on mouse hover to indicate interactivity.
/// Requires the block not be sliding.
/// </summary>
[RequireComponent(typeof(Block))]
public class BlockHoverEffect : MonoBehaviour
{
    [SerializeField] private float hoverScale    = 1.06f;
    [SerializeField] private float hoverDuration = 0.15f;

    private Block  _block;
    private Tween  _tween;
    private bool   _hovered;

    void Awake()
    {
        _block = GetComponent<Block>();
    }

    void OnMouseEnter()
    {
        if (_block.IsSliding || GameManager.Instance.CurrentState != GameState.Playing) return;
        _hovered = true;
        _tween?.Kill();
        _tween = transform.DOScale(hoverScale, hoverDuration).SetEase(Ease.OutQuad);
    }

    void OnMouseExit()
    {
        _hovered = false;
        _tween?.Kill();
        _tween = transform.DOScale(1f, hoverDuration).SetEase(Ease.OutQuad);
    }

    void OnDisable()
    {
        _tween?.Kill();
        transform.localScale = Vector3.one;
    }
}
