using UnityEngine;

/// <summary>
/// Camera cố định nhìn thẳng vào bảng thí nghiệm.
/// - Scroll = zoom in/out (không xuyên vật thể)
/// - Chuột giữa giữ = pan nhẹ
/// - R = reset
/// </summary>
public class LockedCameraController : MonoBehaviour
{
    [Header("=== VỊ TRÍ CỐ ĐỊNH ===")]
    public Transform lookTarget;
    public Vector3 startPosition = new Vector3(387f, 0.3f, 498.5f);
    public Vector3 startRotation = new Vector3(0f, 180f, 0f);

    [Header("=== ZOOM ===")]
    public float scrollSpeed = 0.3f;
    public float minZoom = 0.2f;
    public float maxZoom = 3f;

    [Header("=== CHỐNG XUYÊN VẬT THỂ ===")]
    [Tooltip("Khoảng cách tối thiểu giữ camera và vật thể phía trước")]
    public float minDistanceToObject = 0.15f;
    [Tooltip("Layer nào được coi là vật cản (board, dây...)")]
    public LayerMask collisionLayers = ~0; // Tất cả layer mặc định

    [Header("=== PAN ===")]
    public bool allowPan = true;
    public float panSpeed = 0.002f;
    [Tooltip("Giới hạn pan để không rời khỏi bảng")]
    public float maxPanX = 0.3f;
    public float maxPanY = 0.2f;

    private Vector3 initialPosition;
    private float currentZoom = 1f;
    private Vector3 panOffset = Vector3.zero;
    private Vector3 lastMousePos;
    private bool isPanning = false;

    void Start()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(startRotation);
        initialPosition = startPosition;
        if (lookTarget != null) transform.LookAt(lookTarget);
    }

    void Update()
    {
        HandleZoom();
        if (allowPan) HandlePan();
        HandleReset();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        float newZoom = Mathf.Clamp(currentZoom - scroll * scrollSpeed, minZoom, maxZoom);

        // Tính vị trí mới nếu zoom in
        Vector3 direction = transform.forward;
        Vector3 newPos = initialPosition + panOffset - direction * (newZoom - 1f);

        if (scroll > 0f) // Zoom in → kiểm tra khoảng cách an toàn tới mặt phẳng Z = -47.28
        {
            // Trục Z của bảng mạch (các socket) đang ở khoảng -47.28
            // Đảm bảo camera (Z) không đi qua mức -48.0 để không bị xuyên hình
            float boardZ = -47.28f;
            float safeDistanceZ = 0.5f; // Cách mặt bảng ít nhất 0.5 unit

            if (newPos.z > boardZ - safeDistanceZ)
            {
                return; // Ngăn không cho zoom thêm
            }
        }

        currentZoom = newZoom;
        transform.position = newPos;
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
            isPanning = false;

        if (!isPanning) return;

        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;

        Vector3 pan = (-transform.right * delta.x + -transform.up * delta.y) * panSpeed;

        // Giới hạn pan không rời quá xa bảng
        Vector3 newPanOffset = panOffset + pan;
        newPanOffset.x = Mathf.Clamp(newPanOffset.x, -maxPanX, maxPanX);
        newPanOffset.y = Mathf.Clamp(newPanOffset.y, -maxPanY, maxPanY);

        Vector3 panDelta = newPanOffset - panOffset;
        panOffset = newPanOffset;
        transform.position += panDelta;
    }

    void HandleReset()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        currentZoom = 1f;
        panOffset = Vector3.zero;
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(startRotation);
        if (lookTarget != null) transform.LookAt(lookTarget);
        Debug.Log("<color=cyan>Camera reset!</color>");
    }
}
