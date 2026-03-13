using UnityEngine;
using DG.Tweening;

/// <summary>
/// Adjusts the orthographic camera to frame the grid perfectly,
/// with smooth transitions between levels.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private float padding = 1.5f;
    [SerializeField] private float transitionDuration = 0.6f;

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
            FitToGrid();
    }

    public void FitToGrid()
    {
        var grid = FindObjectOfType<GridSystem>();
        if (grid == null) return;

        float gridW = grid.Width  * grid.CellSize;
        float gridH = grid.Height * grid.CellSize;

        // Center the camera on the grid
        Vector3 center = grid.transform.position + new Vector3(gridW * 0.5f - grid.CellSize * 0.5f,
                                                                gridH * 0.5f - grid.CellSize * 0.5f, -10f);

        // Compute required ortho size to fit both dimensions
        float aspect  = (float)Screen.width / Screen.height;
        float sizeByH = gridH * 0.5f + padding;
        float sizeByW = (gridW * 0.5f + padding) / aspect;
        float orthoSize = Mathf.Max(sizeByH, sizeByW);

        _cam.DOOrthoSize(orthoSize, transitionDuration).SetEase(Ease.OutSine);
        transform.DOMove(center, transitionDuration).SetEase(Ease.OutSine);
    }
}
