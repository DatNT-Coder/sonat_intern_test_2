using UnityEngine;

/// <summary>
/// Điều chỉnh camera orthographic size và position để grid luôn vừa màn hình.
/// KHÔNG thay đổi cellSize — chỉ thay đổi camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private Transform gridOrigin;

    [Header("UI Safe Area (world units)")]
    [SerializeField] private float topUIHeight = 1.2f;  // chiều cao top bar
    [SerializeField] private float bottomUIHeight = 1.2f;  // chiều cao bottom bar
    [SerializeField] private float sidePadding = 0.4f;  // padding 2 bên

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Start()
    {
        FitGridToScreen();
    }

    public void FitGridToScreen()
    {
        if (gridSystem == null || gridOrigin == null || _cam == null) return;

        float gridW = gridSystem.Width * gridSystem.CellSize;
        float gridH = gridSystem.Height * gridSystem.CellSize;

        // Tổng chiều cao cần hiển thị = grid + top bar + bottom bar
        float totalH = gridH + topUIHeight + bottomUIHeight;
        // Tổng chiều rộng cần hiển thị = grid + 2 bên
        float totalW = gridW + sidePadding * 2f;

        // Tính ortho size cần thiết theo cả 2 chiều, lấy cái lớn hơn
        float sizeByH = totalH / 2f;
        float sizeByW = totalW / (2f * _cam.aspect);
        float orthoSize = Mathf.Max(sizeByH, sizeByW);

        _cam.orthographicSize = orthoSize;

        // Đặt GridOrigin: căn giữa theo X, đáy = bottom UI + 1 chút padding
        float camCenterX = _cam.transform.position.x;
        float camBottomY = _cam.transform.position.y - orthoSize;

        float originX = camCenterX - gridW * 0.5f;
        float originY = camBottomY + bottomUIHeight;

        gridOrigin.position = new Vector3(originX, originY, 0f);

        // Camera Y = giữa vùng safe (không cần dịch vì grid đã căn theo camera)
        Vector3 camPos = _cam.transform.position;
        camPos.y = originY + gridH * 0.5f + (topUIHeight - bottomUIHeight) * 0.5f;
        _cam.transform.position = camPos;

        Debug.Log($"[Camera] orthoSize={orthoSize:F2}, origin=({originX:F2},{originY:F2})");
    }
}