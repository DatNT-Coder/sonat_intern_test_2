using UnityEngine;
using DG.Tweening;

/// <summary>
/// Attach to the Camera. Call ScreenShake.Instance.Shake() when needed.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [SerializeField] private float defaultDuration  = 0.2f;
    [SerializeField] private float defaultStrength  = 0.15f;
    [SerializeField] private int   defaultVibrato   = 10;

    private Vector3 _originalPos;
    private Tween   _activeTween;

    void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        _originalPos = transform.localPosition;
    }

    public void Shake(float duration = -1f, float strength = -1f)
    {
        float d = duration < 0 ? defaultDuration : duration;
        float s = strength < 0 ? defaultStrength : strength;

        _activeTween?.Kill();
        transform.localPosition = _originalPos;

        _activeTween = transform
            .DOShakePosition(d, s, defaultVibrato, 90, false, true)
            .OnComplete(() => transform.localPosition = _originalPos);
    }
}
